using MusicApp.BLL.DTOs;
using MusicApp.BLL.Interfaces;
using NAudio.Wave;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MusicApp.UI.Forms
{
    // ─────────────────────────────────────────────────────────────────────
    //  TrackEditDialog
    // ─────────────────────────────────────────────────────────────────────
    public class TrackEditDialog : Form
    {
        public TrackDto Result { get; private set; }

        private TextBox _txtTitle, _txtGenre, _txtFilePath;
        private Label _lblDurationValue;
        private int _durationSeconds;
        private CheckedListBox _clbArtists;     // ← Изменено: теперь multi-select
        private ComboBox _cmbAlbum;
        private Button _btnBrowse, _btnOk, _btnCancel;

        private readonly IArtistService _artistSvc;
        private readonly IAlbumService _albumSvc;

        public TrackEditDialog(TrackDto existing, IArtistService artistSvc, IAlbumService albumSvc)
        {
            _artistSvc = artistSvc;
            _albumSvc = albumSvc;

            Text = existing == null ? "Добавить трек" : "Редактировать трек";
            Size = new Size(500, 480);                    // Увеличили высоту
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 7
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _txtTitle = new TextBox { Dock = DockStyle.Fill };
            _txtGenre = new TextBox { Dock = DockStyle.Fill };

            _lblDurationValue = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "— (выберите файл)",
                ForeColor = Color.Gray
            };

            // === CheckedListBox для нескольких исполнителей ===
            _clbArtists = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                Height = 140,
                CheckOnClick = true
            };

            _cmbAlbum = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var filePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight
            };
            _txtFilePath = new TextBox { Width = 280, ReadOnly = true };
            _btnBrowse = new Button { Text = "…", Width = 35, Height = 25 };
            _btnBrowse.Click += OnBrowse;
            filePanel.Controls.AddRange(new Control[] { _txtFilePath, _btnBrowse });

            AddRow(tbl, 0, "Название:", _txtTitle);
            AddRow(tbl, 1, "Жанр:", _txtGenre);
            AddRow(tbl, 2, "Длительность:", _lblDurationValue);
            AddRow(tbl, 3, "Исполнители:", _clbArtists);     // ← Изменено
            AddRow(tbl, 4, "Альбом:", _cmbAlbum);
            AddRow(tbl, 5, "MP3-файл:", filePanel);

            _btnOk = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Width = 110 };
            _btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 110 };
            _btnOk.Click += OnOk;

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 45,
                Padding = new Padding(0, 5, 0, 0)
            };
            btnPanel.Controls.AddRange(new Control[] { _btnCancel, _btnOk });

            Controls.Add(tbl);
            Controls.Add(btnPanel);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            // Загрузка данных
            LoadArtists();
            LoadAlbums();

            if (existing != null)
            {
                _txtTitle.Text = existing.Title;
                _txtGenre.Text = existing.Genre;
                _txtFilePath.Text = existing.FilePath;
                _durationSeconds = existing.DurationSeconds;
                UpdateDurationLabel(_durationSeconds);

                // Отмечаем выбранных исполнителей
                SelectExistingArtists(existing.ArtistIds);

                if (existing.AlbumId.HasValue)
                    _cmbAlbum.SelectedValue = existing.AlbumId.Value;

                Result = existing;
            }
        }

        private void LoadArtists()
        {
            var artists = _artistSvc.GetAll().OrderBy(a => a.Name).ToList();
            _clbArtists.Items.Clear();
            foreach (var artist in artists)
            {
                _clbArtists.Items.Add(artist, false);
            }
        }

        private void LoadAlbums()
        {
            // Загружаем все альбомы (можно улучшить позже)
            var albums = _albumSvc.GetAll();
            albums.Insert(0, new AlbumDto { AlbumId = 0, Title = "— Без альбома —" });

            _cmbAlbum.DataSource = albums;
            _cmbAlbum.DisplayMember = "Title";
            _cmbAlbum.ValueMember = "AlbumId";
        }

        private void SelectExistingArtists(List<int> artistIds)
        {
            if (artistIds == null) return;

            for (int i = 0; i < _clbArtists.Items.Count; i++)
            {
                var artist = (ArtistDto)_clbArtists.Items[i];
                if (artistIds.Contains(artist.ArtistId))
                {
                    _clbArtists.SetItemChecked(i, true);
                }
            }
        }

        // ── Выбор файла ─────────────────────────────────────────────────────
        private void OnBrowse(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MP3 files|*.mp3|All files|*.*",
                Title = "Выберите аудиофайл"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            _txtFilePath.Text = ofd.FileName;
            _durationSeconds = ReadDurationFromFile(ofd.FileName);
            UpdateDurationLabel(_durationSeconds);
        }

        private static int ReadDurationFromFile(string filePath)
        {
            try
            {
                using var reader = new AudioFileReader(filePath);
                return (int)reader.TotalTime.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }

        private void UpdateDurationLabel(int seconds)
        {
            if (seconds > 0)
            {
                _lblDurationValue.Text = $"{seconds / 60}:{seconds % 60:D2} ({seconds} сек.)";
                _lblDurationValue.ForeColor = Color.Black;
            }
            else
            {
                _lblDurationValue.Text = "Не удалось определить длительность";
                _lblDurationValue.ForeColor = Color.Red;
            }
        }

        // ── Сохранение ─────────────────────────────────────────────────────
        private void OnOk(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtTitle.Text))
            {
                MessageBox.Show("Введите название трека.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtFilePath.Text))
            {
                MessageBox.Show("Выберите MP3-файл.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            var selectedArtistIds = _clbArtists.CheckedItems
                                               .Cast<ArtistDto>()
                                               .Select(a => a.ArtistId)
                                               .ToList();

            if (selectedArtistIds.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одного исполнителя.", "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            int? albumId = _cmbAlbum.SelectedValue is int aid && aid != 0 ? aid : null;

            Result = new TrackDto
            {
                TrackId = Result?.TrackId ?? 0,
                Title = _txtTitle.Text.Trim(),
                Genre = _txtGenre.Text.Trim(),
                DurationSeconds = _durationSeconds,
                FilePath = _txtFilePath.Text.Trim(),
                ArtistIds = selectedArtistIds,
                AlbumId = albumId
            };
        }

        private static void AddRow(TableLayoutPanel tbl, int row, string label, Control ctrl)
        {
            tbl.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            }, 0, row);
            tbl.Controls.Add(ctrl, 1, row);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ArtistEditDialog
    // ─────────────────────────────────────────────────────────────────────
    public class ArtistEditDialog : Form
    {
        public ArtistDto Result { get; private set; }
 
        private TextBox _txtName, _txtBio;
 
        public ArtistEditDialog(ArtistDto existing)
        {
            Text = existing == null ? "Добавить исполнителя" : "Редактировать исполнителя";
            Size = new Size(380, 240);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
 
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, Padding = new Padding(12),
                ColumnCount = 2, RowCount = 3
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
 
            _txtName = new TextBox { Dock = DockStyle.Fill };
            _txtBio  = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
 
            tbl.Controls.Add(new Label { Text = "Имя:",       Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            tbl.Controls.Add(_txtName, 1, 0);
            tbl.Controls.Add(new Label { Text = "Биография:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            tbl.Controls.Add(_txtBio, 1, 1);
 
            var btnOk     = new Button { Text = "Сохранить", DialogResult = DialogResult.OK,    Width = 100 };
            var btnCancel = new Button { Text = "Отмена",    DialogResult = DialogResult.Cancel, Width = 100 };
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtName.Text))
                {
                    MessageBox.Show("Введите имя исполнителя.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                Result = new ArtistDto
                {
                    ArtistId = existing?.ArtistId ?? 0,
                    Name     = _txtName.Text.Trim(),
                    Bio      = _txtBio.Text.Trim()   // пустая строка — допустимо
                };
            };
 
            var btnPanel = new FlowLayoutPanel
                { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 36 };
            btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });
 
            Controls.Add(tbl);
            Controls.Add(btnPanel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
 
            if (existing != null) { _txtName.Text = existing.Name; _txtBio.Text = existing.Bio ?? ""; }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  AlbumEditDialog
    // ─────────────────────────────────────────────────────────────────────
    public class AlbumEditDialog : Form
    {
        public AlbumDto Result { get; private set; }

        private TextBox  _txtTitle;
        private NumericUpDown _nudYear;
        private ComboBox _cmbArtist;

        public AlbumEditDialog(AlbumDto existing, IArtistService artistSvc)
        {
            Text = existing == null ? "Добавить альбом" : "Редактировать альбом";
            Size = new Size(360, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;

            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 4 };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _txtTitle  = new TextBox { Dock = DockStyle.Fill };
            _nudYear   = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1900, Maximum = DateTime.Now.Year, Value = DateTime.Now.Year };
            _cmbArtist = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

            var artists = artistSvc.GetAll();
            _cmbArtist.DataSource    = artists;
            _cmbArtist.DisplayMember = "Name";
            _cmbArtist.ValueMember   = "ArtistId";

            tbl.Controls.Add(new Label { Text = "Название:",    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            tbl.Controls.Add(_txtTitle, 1, 0);
            tbl.Controls.Add(new Label { Text = "Год:",         Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            tbl.Controls.Add(_nudYear, 1, 1);
            tbl.Controls.Add(new Label { Text = "Исполнитель:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            tbl.Controls.Add(_cmbArtist, 1, 2);

            var btnOk     = new Button { Text = "Сохранить", DialogResult = DialogResult.OK,    Width = 100 };
            var btnCancel = new Button { Text = "Отмена",    DialogResult = DialogResult.Cancel, Width = 100 };
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtTitle.Text)) { MessageBox.Show("Введите название."); DialogResult = DialogResult.None; return; }
                Result = new AlbumDto { AlbumId = existing?.AlbumId ?? 0, Title = _txtTitle.Text.Trim(), Year = (int)_nudYear.Value, ArtistId = (int)_cmbArtist.SelectedValue };
            };

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 36 };
            btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });

            Controls.Add(tbl);
            Controls.Add(btnPanel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            if (existing != null) { _txtTitle.Text = existing.Title; _nudYear.Value = existing.Year; _cmbArtist.SelectedValue = existing.ArtistId; }
        }
    }
}
