using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using MusicApp.BLL.DTOs;
using MusicApp.BLL.Interfaces;

namespace MusicApp.UI.Forms
{
    public class AdminForm : Form
    {
        private readonly ITrackService  _trackSvc;
        private readonly IArtistService _artistSvc;
        private readonly IAlbumService  _albumSvc;

        private TabControl _tabs;

        // Track tab
        private DataGridView _dgvTracks;
        private Button _btnAddTrack, _btnEditTrack, _btnDeleteTrack;

        // Artist tab
        private DataGridView _dgvArtists;
        private Button _btnAddArtist, _btnEditArtist, _btnDeleteArtist;

        // Album tab
        private DataGridView _dgvAlbums;
        private Button _btnAddAlbum, _btnEditAlbum, _btnDeleteAlbum;

        public AdminForm(ITrackService trackSvc, IArtistService artistSvc, IAlbumService albumSvc)
        {
            _trackSvc  = trackSvc;
            _artistSvc = artistSvc;
            _albumSvc  = albumSvc;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "MusicApp — Администратор";
            Size = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;

            _tabs = new TabControl { Dock = DockStyle.Fill };

            _tabs.TabPages.Add(BuildTrackTab());
            _tabs.TabPages.Add(BuildArtistTab());
            _tabs.TabPages.Add(BuildAlbumTab());

            Controls.Add(_tabs);
            _tabs.SelectedIndexChanged += (s, e) => RefreshCurrentTab();
            LoadAllTabs();
        }

        // ── Track Tab ────────────────────────────────────────────────────

        private TabPage BuildTrackTab()
        {
            var page = new TabPage("🎵 Треки");
            _dgvTracks = CreateGrid();
            _dgvTracks.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "ID",     DataPropertyName = "TrackId",          Width = 40 },
                new DataGridViewTextBoxColumn { HeaderText = "Название", DataPropertyName = "Title",          Width = 160 },
                new DataGridViewTextBoxColumn { HeaderText = "Исполнитель", DataPropertyName = "ArtistName",  Width = 130 },
                new DataGridViewTextBoxColumn { HeaderText = "Альбом",  DataPropertyName = "AlbumTitle",      Width = 120 },
                new DataGridViewTextBoxColumn { HeaderText = "Жанр",    DataPropertyName = "Genre",           Width = 90 },
                new DataGridViewTextBoxColumn { HeaderText = "Длит.",   DataPropertyName = "DurationFormatted",Width = 55 },
                new DataGridViewTextBoxColumn { HeaderText = "Файл",    DataPropertyName = "FilePath",        Width = 160 }
            );

            _btnAddTrack    = new Button { Text = "＋ Добавить", Height = 30, Width = 110 };
            _btnEditTrack   = new Button { Text = "✎ Изменить", Height = 30, Width = 110 };
            _btnDeleteTrack = new Button { Text = "🗑 Удалить",  Height = 30, Width = 110 };

            _btnAddTrack.Click    += OnAddTrack;
            _btnEditTrack.Click   += OnEditTrack;
            _btnDeleteTrack.Click += OnDeleteTrack;

            page.Controls.Add(_dgvTracks);
            page.Controls.Add(MakeToolbar(_btnAddTrack, _btnEditTrack, _btnDeleteTrack));
            return page;
        }

        private void OnAddTrack(object sender, EventArgs e)
        {
            using var dlg = new TrackEditDialog(null, _artistSvc, _albumSvc);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try { _trackSvc.Add(dlg.Result); LoadTracks(); }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void OnEditTrack(object sender, EventArgs e)
        {
            if (_dgvTracks.CurrentRow?.DataBoundItem is not TrackDto dto) return;
            using var dlg = new TrackEditDialog(dto, _artistSvc, _albumSvc);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try { _trackSvc.Update(dlg.Result); LoadTracks(); }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void OnDeleteTrack(object sender, EventArgs e)
        {
            if (_dgvTracks.CurrentRow?.DataBoundItem is not TrackDto dto) return;
            if (Confirm($"Удалить трек «{dto.Title}»?")) { _trackSvc.Delete(dto.TrackId); LoadTracks(); }
        }

        // ── Artist Tab ───────────────────────────────────────────────────

        private TabPage BuildArtistTab()
        {
            var page = new TabPage("🎤 Исполнители");
            _dgvArtists = CreateGrid();
            _dgvArtists.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "ID",   DataPropertyName = "ArtistId", Width = 40 },
                new DataGridViewTextBoxColumn { HeaderText = "Имя",  DataPropertyName = "Name",     Width = 220 },
                new DataGridViewTextBoxColumn { HeaderText = "Биография", DataPropertyName = "Bio", Width = 400 }
            );

            _btnAddArtist    = new Button { Text = "＋ Добавить", Height = 30, Width = 110 };
            _btnEditArtist   = new Button { Text = "✎ Изменить", Height = 30, Width = 110 };
            _btnDeleteArtist = new Button { Text = "🗑 Удалить",  Height = 30, Width = 110 };

            _btnAddArtist.Click    += OnAddArtist;
            _btnEditArtist.Click   += OnEditArtist;
            _btnDeleteArtist.Click += OnDeleteArtist;

            page.Controls.Add(_dgvArtists);
            page.Controls.Add(MakeToolbar(_btnAddArtist, _btnEditArtist, _btnDeleteArtist));
            return page;
        }

        private void OnAddArtist(object sender, EventArgs e)
        {
            using var dlg = new ArtistEditDialog(null);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try { _artistSvc.Add(dlg.Result); LoadArtists(); }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void OnEditArtist(object sender, EventArgs e)
        {
            if (_dgvArtists.CurrentRow?.DataBoundItem is not ArtistDto dto) return;
            using var dlg = new ArtistEditDialog(dto);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try { _artistSvc.Update(dlg.Result); LoadArtists(); }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void OnDeleteArtist(object sender, EventArgs e)
        {
            if (_dgvArtists.CurrentRow?.DataBoundItem is not ArtistDto dto) return;
            if (Confirm($"Удалить исполнителя «{dto.Name}»?")) { _artistSvc.Delete(dto.ArtistId); LoadArtists(); }
        }

        // ── Album Tab ────────────────────────────────────────────────────

        private TabPage BuildAlbumTab()
        {
            var page = new TabPage("💿 Альбомы");
            _dgvAlbums = CreateGrid();
            _dgvAlbums.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "ID",          DataPropertyName = "AlbumId",     Width = 40 },
                new DataGridViewTextBoxColumn { HeaderText = "Название",     DataPropertyName = "Title",       Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "Исполнитель", DataPropertyName = "ArtistName",  Width = 180 },
                new DataGridViewTextBoxColumn { HeaderText = "Год",         DataPropertyName = "Year",        Width = 60 }
            );

            _btnAddAlbum    = new Button { Text = "＋ Добавить", Height = 30, Width = 110 };
            _btnEditAlbum   = new Button { Text = "✎ Изменить", Height = 30, Width = 110 };
            _btnDeleteAlbum = new Button { Text = "🗑 Удалить",  Height = 30, Width = 110 };

            _btnAddAlbum.Click    += OnAddAlbum;
            _btnEditAlbum.Click   += OnEditAlbum;
            _btnDeleteAlbum.Click += OnDeleteAlbum;

            page.Controls.Add(_dgvAlbums);
            page.Controls.Add(MakeToolbar(_btnAddAlbum, _btnEditAlbum, _btnDeleteAlbum));
            return page;
        }

        private void OnAddAlbum(object sender, EventArgs e)
        {
            using var dlg = new AlbumEditDialog(null, _artistSvc);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try { _albumSvc.Add(dlg.Result); LoadAlbums(); }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void OnEditAlbum(object sender, EventArgs e)
        {
            if (_dgvAlbums.CurrentRow?.DataBoundItem is not AlbumDto dto) return;
            using var dlg = new AlbumEditDialog(dto, _artistSvc);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try { _albumSvc.Update(dlg.Result); LoadAlbums(); }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void OnDeleteAlbum(object sender, EventArgs e)
        {
            if (_dgvAlbums.CurrentRow?.DataBoundItem is not AlbumDto dto) return;
            if (Confirm($"Удалить альбом «{dto.Title}»?")) { _albumSvc.Delete(dto.AlbumId); LoadAlbums(); }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private void LoadAllTabs() { LoadTracks(); LoadArtists(); LoadAlbums(); }
        private void LoadTracks()  => _dgvTracks.DataSource  = _trackSvc.GetAll();
        private void LoadArtists() => _dgvArtists.DataSource = _artistSvc.GetAll();
        private void LoadAlbums()  => _dgvAlbums.DataSource  = _albumSvc.GetAll();

        private void RefreshCurrentTab()
        {
            switch (_tabs.SelectedIndex)
            {
                case 0: LoadTracks();  break;
                case 1: LoadArtists(); break;
                case 2: LoadAlbums();  break;
            }
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
            return g;
        }

        private static Panel MakeToolbar(params Button[] buttons)
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(4, 4, 0, 0)
            };
            bar.Controls.AddRange(buttons);
            return bar;
        }

        private static bool Confirm(string msg) =>
            MessageBox.Show(msg, "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        private static void ShowError(string msg) =>
            MessageBox.Show(msg, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
