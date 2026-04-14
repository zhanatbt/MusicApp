using Microsoft.Extensions.DependencyInjection;
using MusicApp.BLL.DTOs;
using MusicApp.BLL.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MusicApp.Forms
{
    public partial class MainForm : Form
    {
        private readonly ITrackService _trackSvc;
        private readonly IPlaylistService _playlistSvc;
        private UserDto _currentUser;

        // Поиск
        private TextBox _txtSearch;
        private ComboBox _cmbGenre, _cmbArtist;
        private Button _btnSearch, _btnClear;

        // Списки
        private DataGridView _dgvTracks;
        private DataGridView _dgvPlaylist;

        // Плейлисты
        private ComboBox _cmbPlaylists;
        private Button _btnNewPlaylist, _btnDeletePlaylist, _btnAddToPlaylist;
        private Button _btnRemoveFromPlaylist;

        private List<PlaylistTrackItemDto> _currentPlaylistItems = new();

        // NAudio плеер
        private IWavePlayer? _waveOut;
        private AudioFileReader? _audioFileReader;
        private Label _lblNowPlaying;
        private Button _btnPlayPause, _btnStop;
        private TrackBar _volumeTrackBar;
        private bool _isPaused = false;

        private PlayerForm? _playerForm;

        public MainForm(ITrackService trackSvc, IPlaylistService playlistSvc, PlayerForm? playerForm)
        {
            _trackSvc = trackSvc;
            _playlistSvc = playlistSvc;
            InitializeComponent();        // Это вызов из Designer.cs
            InitializeCustomComponents(); // Наша ручная инициализация
            _playerForm = playerForm;
        }

        public void SetCurrentUser(UserDto user)
        {
            _currentUser = user;
            Text = $"MusicApp — {user.Username}";
            RefreshAll();
        }

        /// <summary>
        /// Здесь размещаем весь код создания элементов управления
        /// </summary>
        private void InitializeCustomComponents()
        {
            this.Size = new Size(1220, 760);
            this.MinimumSize = new Size(1000, 680);

            var main = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 670
            };

            // ===================== Левая панель =====================
            var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 80,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(3)
            };

            _txtSearch = new TextBox { PlaceholderText = "Название / исполнитель / альбом", Width = 250, Height = 30 };
            _cmbGenre = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbArtist = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            _btnSearch = new Button { Text = "🔍 Поиск", Width = 95, Height = 30 };
            _btnClear = new Button { Text = "✕ Сброс", Width = 80, Height = 30 };

            _btnSearch.Click += OnSearch;
            _btnClear.Click += ClearSearch;

            searchPanel.Controls.AddRange(new Control[] { _txtSearch, _cmbGenre, _cmbArtist, _btnSearch, _btnClear });

            _dgvTracks = CreateGrid();
            _dgvTracks.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Название", DataPropertyName = "Title", Width = 210 },
                new DataGridViewTextBoxColumn { HeaderText = "Исполнитель", DataPropertyName = "ArtistName", Width = 170 },
                new DataGridViewTextBoxColumn { HeaderText = "Альбом", DataPropertyName = "AlbumTitle", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "Жанр", DataPropertyName = "Genre", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "Длит.", DataPropertyName = "DurationFormatted", Width = 70 }
            );

            leftPanel.Controls.Add(_dgvTracks);
            leftPanel.Controls.Add(searchPanel);

            // ===================== ПРАВАЯ ПАНЕЛЬ =====================
            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            // 1. Верхняя панель (комбобокс + кнопки управления плейлистом)
            var plHeader = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(2)
            };

            _cmbPlaylists = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Height = 32 };
            _btnNewPlaylist = new Button { Text = "+", Width = 36, Height = 32 };
            _btnDeletePlaylist = new Button { Text = "🗑", Width = 36, Height = 32 };
            _btnAddToPlaylist = new Button { Text = "← Добавить трек", Width = 160, Height = 32 };

            _cmbPlaylists.SelectedIndexChanged += OnPlaylistSelected;
            _btnNewPlaylist.Click += OnNewPlaylist;
            _btnDeletePlaylist.Click += OnDeletePlaylist;
            _btnAddToPlaylist.Click += OnAddToPlaylist;

            plHeader.Controls.AddRange(new Control[] { _cmbPlaylists, _btnNewPlaylist, _btnDeletePlaylist, _btnAddToPlaylist });

            // 2. Сетка плейлиста
            _dgvPlaylist = CreateGrid();
            _dgvPlaylist.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Название", DataPropertyName = "Title", Width = 190 },
                new DataGridViewTextBoxColumn { HeaderText = "Исполнитель", DataPropertyName = "ArtistName", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "Длит.", DataPropertyName = "DurationFormatted", Width = 70 }
            );

            // 3. Панель действий (Удалить + Воспроизвести плейлист)
            var actionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(2)
            };

            _btnRemoveFromPlaylist = new Button
            {
                Text = "🗑 Удалить из плейлиста",
                Width = 190,
                Height = 36,
                BackColor = Color.FromArgb(220, 80, 80),
                ForeColor = Color.White
            };

            var btnPlayPlaylist = new Button
            {
                Text = "▶ Воспроизвести весь плейлист",
                Width = 220,
                Height = 36,
                BackColor = Color.FromArgb(50, 150, 80),
                ForeColor = Color.White
            };

            _btnRemoveFromPlaylist.Click += OnRemoveFromPlaylist;
            btnPlayPlaylist.Click += OnPlayWholePlaylist;   // новая кнопка

            actionsPanel.Controls.AddRange(new Control[] { _btnRemoveFromPlaylist, btnPlayPlaylist });

            // 4. Панель плеера (Пауза, Стоп, громкость)
            var playerPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };

            _btnPlayPause = new Button { Text = "▶ Воспроизвести", Width = 135, Height = 36 };
            _btnStop = new Button { Text = "⏹ Стоп", Width = 85, Height = 36 };
            _volumeTrackBar = new TrackBar { Width = 160, Minimum = 0, Maximum = 100, Value = 80 };
            var lblVolume = new Label { Text = "Громкость:", Width = 70, TextAlign = ContentAlignment.MiddleLeft };

            _btnPlayPause.Click += BtnPlayPause_Click;
            _btnStop.Click += (s, e) => StopPlayback();
            _volumeTrackBar.Scroll += VolumeChanged;

            playerPanel.Controls.AddRange(new Control[] { _btnPlayPause, _btnStop, lblVolume, _volumeTrackBar });

            // 5. Надпись "Сейчас играет"
            _lblNowPlaying = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Italic)
            };

            // === КРИТИЧНО ВАЖНЫЙ ПОРЯДОК ДОБАВЛЕНИЯ ===
            rightPanel.Controls.Add(_dgvPlaylist);     // Fill первым!
            rightPanel.Controls.Add(plHeader);         // Top
            rightPanel.Controls.Add(actionsPanel);     // Bottom
            rightPanel.Controls.Add(playerPanel);      // Bottom
            rightPanel.Controls.Add(_lblNowPlaying);   // Bottom

            main.Panel1.Controls.Add(leftPanel);
            main.Panel2.Controls.Add(rightPanel);
            this.Controls.Add(main);

            // Двойной клик
            _dgvTracks.CellDoubleClick += (s, e) => PlaySelectedTrack(_dgvTracks);
            _dgvPlaylist.CellDoubleClick += (s, e) => PlaySelectedTrack(_dgvPlaylist);
        }

        private void VolumeChanged(object sender, EventArgs e)
        {
            if (_waveOut != null)
                _waveOut.Volume = _volumeTrackBar.Value / 100f;
        }

        // Остальные методы (RefreshAll, LoadTracks, обработчики и т.д.) остаются теми же
        private void RefreshAll()
        {
            LoadTracks();
            LoadFilters();
            LoadPlaylists();
        }

        private void LoadTracks(List<TrackDto> tracks = null) =>
            _dgvTracks.DataSource = tracks ?? _trackSvc.GetAll();

        private void LoadFilters()
        {
            var genres = _trackSvc.GetAllGenres();
            var artists = Program.Services.GetRequiredService<IArtistService>().GetAll();

            _cmbGenre.Items.Clear();
            _cmbGenre.Items.Add("— Жанр —");
            genres.ForEach(g => _cmbGenre.Items.Add(g));
            _cmbGenre.SelectedIndex = 0;

            _cmbArtist.Items.Clear();
            _cmbArtist.Items.Add("— Исполнитель —");
            artists.ForEach(a => _cmbArtist.Items.Add(a.Name));
            _cmbArtist.SelectedIndex = 0;
        }

        private void LoadPlaylists()
        {
            if (_currentUser == null) return;
            var pls = _playlistSvc.GetByUser(_currentUser.UserId);
            _cmbPlaylists.DataSource = pls;
            _cmbPlaylists.DisplayMember = "Name";
            _cmbPlaylists.ValueMember = "PlaylistId";

            if (pls.Count > 0) LoadPlaylistTracks(pls[0].PlaylistId);
        }

        private void LoadPlaylistTracks(int playlistId)
        {
            _currentPlaylistItems = _playlistSvc.GetTracksWithIds(playlistId);
            _dgvPlaylist.DataSource = _currentPlaylistItems;
        }

        private void OnSearch(object sender, EventArgs e)
        {
            string? genre = _cmbGenre.SelectedIndex > 0 ? _cmbGenre.SelectedItem?.ToString() : null;
            string? artist = _cmbArtist.SelectedIndex > 0 ? _cmbArtist.SelectedItem?.ToString() : null;
            LoadTracks(_trackSvc.Search(_txtSearch.Text.Trim(), artist, null, genre));
        }

        private void ClearSearch(object sender, EventArgs e)
        {
            _txtSearch.Clear();
            _cmbGenre.SelectedIndex = 0;
            _cmbArtist.SelectedIndex = 0;
            LoadTracks();
        }

        private void OnPlaylistSelected(object sender, EventArgs e)
        {
            if (_cmbPlaylists.SelectedValue is int id)
                LoadPlaylistTracks(id);
        }

        private void OnNewPlaylist(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Название нового плейлиста:", "Новый плейлист");
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                _playlistSvc.Create(_currentUser.UserId, name);
                LoadPlaylists();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnDeletePlaylist(object sender, EventArgs e)
        {
            if (_cmbPlaylists.SelectedValue is not int id) return;
            if (MessageBox.Show("Удалить плейлист?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            _playlistSvc.Delete(id, _currentUser.UserId);
            LoadPlaylists();
        }

        private void OnAddToPlaylist(object sender, EventArgs e)
        {
            if (_dgvTracks.CurrentRow?.DataBoundItem is not TrackDto track) return;
            if (_cmbPlaylists.SelectedValue is not int plId)
            {
                MessageBox.Show("Сначала выберите плейлист.");
                return;
            }
            _playlistSvc.AddTrack(plId, track.TrackId);
            LoadPlaylistTracks(plId);
        }

        private void OnRemoveFromPlaylist(object sender, EventArgs e)
        {
            if (_dgvPlaylist.CurrentRow?.DataBoundItem is not PlaylistTrackItemDto item) return;
            _playlistSvc.RemoveTrack(item.PlaylistTrackId);
            if (_cmbPlaylists.SelectedValue is int plId)
                LoadPlaylistTracks(plId);
        }

        private void PlaySelectedTrack(DataGridView grid)
        {
            if (grid.CurrentRow?.DataBoundItem is TrackDto track)
                PlayInPlayerWindow(track.FilePath, track.ArtistName, track.Title);
            else if (grid.CurrentRow?.DataBoundItem is PlaylistTrackItemDto ptrack)
                PlayInPlayerWindow(ptrack.FilePath, ptrack.ArtistName, ptrack.Title);
        }

        private void PlayInPlayerWindow(string filePath, string title, string artist)
        {
            if (_playerForm == null || _playerForm.IsDisposed)
                _playerForm = new PlayerForm();

            _playerForm.Play(filePath, title, artist);
            _playerForm.Show();
            _playerForm.BringToFront();
        }

        private void BtnPlayPause_Click(object sender, EventArgs e)
        {
            if (_waveOut == null)
            {
                PlaySelectedTrack(_dgvPlaylist.CurrentRow != null ? _dgvPlaylist : _dgvTracks);
                return;
            }

            if (_isPaused)
            {
                _waveOut.Play();
                _btnPlayPause.Text = "⏸ Пауза";
                _isPaused = false;
            }
            else
            {
                _waveOut.Pause();
                _btnPlayPause.Text = "▶ Продолжить";
                _isPaused = true;
            }
        }

        private void StopPlayback()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioFileReader?.Dispose();
            _waveOut = null;
            _audioFileReader = null;

            _lblNowPlaying.Text = "";
            _btnPlayPause.Text = "▶ Воспроизвести";
            _isPaused = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopPlayback();
            base.OnFormClosing(e);
        }

        private static DataGridView CreateGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window
            };
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 245);
            return g;
        }

        private void OnPlayWholePlaylist(object sender, EventArgs e)
        {
            if (_currentPlaylistItems.Count == 0)
            {
                MessageBox.Show("Плейлист пустой.");
                return;
            }

            var tracksWithFile = _currentPlaylistItems
                .Where(t => !string.IsNullOrEmpty(t.FilePath) && File.Exists(t.FilePath))
                .ToList();

            if (tracksWithFile.Count == 0)
            {
                MessageBox.Show("В плейлисте нет доступных файлов.");
                return;
            }

            StopPlayback();

            try
            {
                _audioFileReader = new AudioFileReader(tracksWithFile[0].FilePath);
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioFileReader);
                _waveOut.Play();

                _lblNowPlaying.Text = $"▶ Плейлист: {tracksWithFile[0].ArtistName} — {tracksWithFile[0].Title}";
                _btnPlayPause.Text = "⏸ Пауза";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }
    }
}