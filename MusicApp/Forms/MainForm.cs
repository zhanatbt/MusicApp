using Microsoft.Extensions.DependencyInjection;
using MusicApp.BLL.DTOs;
using MusicApp.BLL.Interfaces;
using MusicApp.UI;

namespace MusicApp.Forms
{
    public class MainForm : Form
    {
        private readonly ITrackService _trackSvc;
        private readonly IPlaylistService _playlistSvc;
        private UserDto _currentUser;

        // Search controls
        private TextBox _txtSearch;
        private ComboBox _cmbGenre, _cmbArtist;
        private Button _btnSearch, _btnClear;

        // Track list
        private DataGridView _dgvTracks;

        // Playlist controls
        private ComboBox _cmbPlaylists;
        private Button _btnNewPlaylist, _btnDeletePlaylist, _btnAddToPlaylist;
        private DataGridView _dgvPlaylist;
        private Button _btnRemoveFromPlaylist, _btnPlay;

        // Player
        private AxWMPLib.AxWindowsMediaPlayer _player;
        private Label _lblNowPlaying;

        public MainForm(ITrackService trackSvc, IPlaylistService playlistSvc)
        {
            _trackSvc = trackSvc;
            _playlistSvc = playlistSvc;
            BuildUI();
        }

        public void SetCurrentUser(UserDto user)
        {
            _currentUser = user;
            Text = $"MusicApp — {user.Username}";
            RefreshAll();
        }

        private void BuildUI()
        {
            Text = "MusicApp";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 600);

            var main = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 620 };

            // ── LEFT: search + track list ──────────────────────────────
            var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 80, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true, Padding = new Padding(2)
            };

            _txtSearch = new TextBox { PlaceholderText = "Название / исполнитель / альбом", Width = 220, Height = 28 };
            _cmbGenre = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbArtist = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _btnSearch = new Button { Text = "🔍 Поиск", Width = 85, Height = 28 };
            _btnClear = new Button { Text = "✕ Сброс", Width = 75, Height = 28 };

            _cmbGenre.Items.Insert(0, "— Жанр —");
            _cmbArtist.Items.Insert(0, "— Исполнитель —");

            _btnSearch.Click += OnSearch;
            _btnClear.Click += (s, e) =>
            {
                _txtSearch.Clear();
                _cmbGenre.SelectedIndex = 0;
                _cmbArtist.SelectedIndex = 0;
                LoadTracks();
            };

            searchPanel.Controls.AddRange(new Control[] { _txtSearch, _cmbGenre, _cmbArtist, _btnSearch, _btnClear });

            _dgvTracks = CreateGrid();
            _dgvTracks.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Название", DataPropertyName = "Title", Width = 180 },
                new DataGridViewTextBoxColumn
                    { HeaderText = "Исполнитель", DataPropertyName = "ArtistName", Width = 140 },
                new DataGridViewTextBoxColumn { HeaderText = "Альбом", DataPropertyName = "AlbumTitle", Width = 140 },
                new DataGridViewTextBoxColumn { HeaderText = "Жанр", DataPropertyName = "Genre", Width = 100 },
                new DataGridViewTextBoxColumn
                    { HeaderText = "Длит.", DataPropertyName = "DurationFormatted", Width = 60 }
            );

            leftPanel.Controls.Add(_dgvTracks);
            leftPanel.Controls.Add(searchPanel);

            // ── RIGHT: playlists + player ──────────────────────────────
            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            var plHeader = new FlowLayoutPanel
                { Dock = DockStyle.Top, Height = 36, FlowDirection = FlowDirection.LeftToRight };
            _cmbPlaylists = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            _btnNewPlaylist = new Button { Text = "+", Width = 28, Height = 26 };
            _btnDeletePlaylist = new Button { Text = "🗑", Width = 28, Height = 26 };
            _btnAddToPlaylist = new Button { Text = "← Добавить трек", Width = 130, Height = 26 };

            _cmbPlaylists.SelectedIndexChanged += OnPlaylistSelected;
            _btnNewPlaylist.Click += OnNewPlaylist;
            _btnDeletePlaylist.Click += OnDeletePlaylist;
            _btnAddToPlaylist.Click += OnAddToPlaylist;

            plHeader.Controls.AddRange(new Control[]
                { _cmbPlaylists, _btnNewPlaylist, _btnDeletePlaylist, _btnAddToPlaylist });

            _dgvPlaylist = CreateGrid();
            _dgvPlaylist.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Название", DataPropertyName = "Title", Width = 130 },
                new DataGridViewTextBoxColumn
                    { HeaderText = "Исполнитель", DataPropertyName = "ArtistName", Width = 110 },
                new DataGridViewTextBoxColumn
                    { HeaderText = "Длит.", DataPropertyName = "DurationFormatted", Width = 55 }
            );

            var plActions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36 };
            _btnRemoveFromPlaylist = new Button { Text = "Удалить из плейлиста", Width = 160, Height = 26 };
            _btnPlay = new Button
            {
                Text = "▶ Воспроизвести плейлист",
                Width = 175, Height = 26,
                BackColor = Color.FromArgb(50, 150, 80), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnRemoveFromPlaylist.Click += OnRemoveFromPlaylist;
            _btnPlay.Click += OnPlayPlaylist;
            plActions.Controls.AddRange(new Control[] { _btnRemoveFromPlaylist, _btnPlay });

            // WMP Player
            _player = new AxWMPLib.AxWindowsMediaPlayer { Dock = DockStyle.Bottom, Height = 70 };
            _lblNowPlaying = new Label
            {
                Dock = DockStyle.Bottom, Height = 22, TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };

            rightPanel.Controls.Add(_dgvPlaylist);
            rightPanel.Controls.Add(plHeader);
            rightPanel.Controls.Add(_lblNowPlaying);
            rightPanel.Controls.Add(_player);
            rightPanel.Controls.Add(plActions);

            main.Panel1.Controls.Add(leftPanel);
            main.Panel2.Controls.Add(rightPanel);
            Controls.Add(main);
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

        private void RefreshAll()
        {
            LoadTracks();
            LoadFilters();
            LoadPlaylists();
        }

        private void LoadTracks(List<TrackDto> tracks = null)
        {
            _dgvTracks.DataSource = tracks ?? _trackSvc.GetAll();
        }

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
            _dgvPlaylist.DataSource = _playlistSvc.GetTracks(playlistId);
        }

        // ── Event Handlers ────────────────────────────────────────────

        private void OnSearch(object sender, EventArgs e)
        {
            string genre = _cmbGenre.SelectedIndex > 0 ? _cmbGenre.SelectedItem.ToString() : null;
            string artist = _cmbArtist.SelectedIndex > 0 ? _cmbArtist.SelectedItem.ToString() : null;
            LoadTracks(_trackSvc.Search(_txtSearch.Text.Trim(), artist, null, genre));
        }

        private void OnPlaylistSelected(object sender, EventArgs e)
        {
            if (_cmbPlaylists.SelectedValue is int id) LoadPlaylistTracks(id);
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
            if (MessageBox.Show("Удалить плейлист?", "Подтверждение", MessageBoxButtons.YesNo) !=
                DialogResult.Yes) return;
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
            // We need PlaylistTrackId — reload with it included
            if (_dgvPlaylist.CurrentRow?.DataBoundItem is not TrackDto track) return;
            if (_cmbPlaylists.SelectedValue is not int plId) return;
            // Get all PlaylistTrack records for this playlist to find the right one
            var pts = Program.Services.GetRequiredService<MusicApp.DAL.Context.MusicAppDbContext>()
                .PlaylistTracks
                .Where(pt => pt.PlaylistId == plId && pt.TrackId == track.TrackId)
                .OrderBy(pt => pt.Position)
                .ToList();
            if (!pts.Any()) return;
            _playlistSvc.RemoveTrack(pts.First().PlaylistTrackId);
            LoadPlaylistTracks(plId);
        }

        private void OnPlayPlaylist(object sender, EventArgs e)
        {
            if (_cmbPlaylists.SelectedValue is not int plId) return;
            var tracks = _playlistSvc.GetTracks(plId)
                .Where(t => !string.IsNullOrEmpty(t.FilePath))
                .ToList();
            if (!tracks.Any())
            {
                MessageBox.Show("Нет треков с файлами в плейлисте.");
                return;
            }

            // Build WMP playlist
            var wmpPlaylist = _player.playlistCollection.newPlaylist("current");
            foreach (var t in tracks)
            {
                var media = _player.newMedia(t.FilePath);
                wmpPlaylist.appendItem(media);
            }

            _player.currentPlaylist = wmpPlaylist;
            _player.Ctlcontrols.play();

            _lblNowPlaying.Text = $"Воспроизведение: {tracks[0].ArtistName} — {tracks[0].Title}";
        }
    }
}