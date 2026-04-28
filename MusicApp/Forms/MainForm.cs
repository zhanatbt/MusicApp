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
        private ComboBox _cmbGenre;
        private CheckedListBox _clbArtists;
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
            InitializeComponent();
            InitializeCustomComponents();
            _playerForm = playerForm;
        }

        public void SetCurrentUser(UserDto user)
        {
            _currentUser = user;
            Text = $"MusicApp — {user.Username}";
            RefreshAll();
        }

        private void InitializeCustomComponents()
        {
            this.Size = new Size(1250, 760);
            this.MinimumSize = new Size(1050, 700);

            var main = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 680
            };

            // ===================== Левая панель =====================
            var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(3)
            };

            _txtSearch = new TextBox { PlaceholderText = "Название трека...", Width = 250, Height = 30 };
            _cmbGenre = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };

            _clbArtists = new CheckedListBox
            {
                Width = 220,
                Height = 90,
                CheckOnClick = true
            };

            _btnSearch = new Button { Text = "🔍 Поиск", Width = 95, Height = 30 };
            _btnClear = new Button { Text = "✕ Сброс", Width = 80, Height = 30 };

            _btnSearch.Click += OnSearch;
            _btnClear.Click += ClearSearch;

            searchPanel.Controls.AddRange(new Control[]
            {
                new Label { Text = "Поиск:", Width = 50, TextAlign = ContentAlignment.MiddleLeft },
                _txtSearch,
                new Label { Text = "Жанр:", Width = 50, TextAlign = ContentAlignment.MiddleLeft },
                _cmbGenre,
                new Label { Text = "Исполнители:", Width = 80, TextAlign = ContentAlignment.MiddleLeft },
                _clbArtists,
                _btnSearch,
                _btnClear
            });

            _dgvTracks = CreateGrid();
            _dgvTracks.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Название", DataPropertyName = "Title", Width = 220 },
                new DataGridViewTextBoxColumn { HeaderText = "Исполнители", DataPropertyName = "ArtistsDisplay", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "Альбом", DataPropertyName = "AlbumTitle", Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "Жанр", DataPropertyName = "Genre", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "Длит.", DataPropertyName = "DurationFormatted", Width = 70 }
            );

            leftPanel.Controls.Add(_dgvTracks);
            leftPanel.Controls.Add(searchPanel);

            // ===================== Правая панель =====================
            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            // Верхняя панель плейлистов
            var plHeader = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
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

            // Сетка плейлиста
            _dgvPlaylist = CreateGrid();
            _dgvPlaylist.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Название", DataPropertyName = "Title", Width = 190 },
                new DataGridViewTextBoxColumn { HeaderText = "Исполнители", DataPropertyName = "ArtistsDisplay", Width = 190 },
                new DataGridViewTextBoxColumn { HeaderText = "Длит.", DataPropertyName = "DurationFormatted", Width = 70 }
            );

            // Панель действий
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
            btnPlayPlaylist.Click += OnPlayWholePlaylist;

            actionsPanel.Controls.AddRange(new Control[] { _btnRemoveFromPlaylist, btnPlayPlaylist });

            // Панель плеера
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

            _lblNowPlaying = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Italic)
            };

            // Порядок добавления важен!
            rightPanel.Controls.Add(_dgvPlaylist);
            rightPanel.Controls.Add(plHeader);
            rightPanel.Controls.Add(actionsPanel);
            rightPanel.Controls.Add(playerPanel);
            rightPanel.Controls.Add(_lblNowPlaying);

            main.Panel1.Controls.Add(leftPanel);
            main.Panel2.Controls.Add(rightPanel);
            this.Controls.Add(main);

            // Двойной клик по треку
            _dgvTracks.CellDoubleClick += (s, e) => PlaySelectedTrack(_dgvTracks);
            _dgvPlaylist.CellDoubleClick += (s, e) => PlaySelectedTrack(_dgvPlaylist);
        }

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
            var artists = Program.Services.GetRequiredService<IArtistService>().GetAll().OrderBy(a => a.Name).ToList();

            _cmbGenre.Items.Clear();
            _cmbGenre.Items.Add("— Жанр —");
            genres.ForEach(g => _cmbGenre.Items.Add(g));
            _cmbGenre.SelectedIndex = 0;

            _clbArtists.Items.Clear();
            foreach (var artist in artists)
                _clbArtists.Items.Add(artist, false);
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
            var selectedArtistIds = _clbArtists.CheckedItems.Cast<ArtistDto>().Select(a => a.ArtistId).ToList();

            var tracks = _trackSvc.Search(_txtSearch.Text.Trim(), null, null, genre);

            if (selectedArtistIds.Any())
            {
                tracks = tracks.Where(t => t.ArtistIds.Intersect(selectedArtistIds).Any()).ToList();
            }

            LoadTracks(tracks);
        }

        private void ClearSearch(object sender, EventArgs e)
        {
            _txtSearch.Clear();
            _cmbGenre.SelectedIndex = 0;
            for (int i = 0; i < _clbArtists.Items.Count; i++)
                _clbArtists.SetItemChecked(i, false);

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
                PlayInPlayerWindow(track.FilePath, track.ArtistsDisplay, track.Title);
            else if (grid.CurrentRow?.DataBoundItem is PlaylistTrackItemDto ptrack)
                PlayInPlayerWindow(ptrack.FilePath, ptrack.ArtistsDisplay, ptrack.Title);
        }

        private void PlayInPlayerWindow(string filePath, string artistDisplay, string title)
        {
            if (_playerForm == null || _playerForm.IsDisposed)
                _playerForm = new PlayerForm();

            _playerForm.Play(filePath, title, artistDisplay);
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

        private void VolumeChanged(object sender, EventArgs e)
        {
            if (_waveOut != null)
                _waveOut.Volume = _volumeTrackBar.Value / 100f;
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

                _lblNowPlaying.Text = $"▶ Плейлист: {tracksWithFile[0].ArtistsDisplay} — {tracksWithFile[0].Title}";
                _btnPlayPause.Text = "⏸ Пауза";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
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
    }
}