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
        private ComboBox _cmbArtist, _cmbAlbum;
        private Button _btnBrowse, _btnOk, _btnCancel;

        private readonly IArtistService _artistSvc;
        private readonly IAlbumService _albumSvc;

        public TrackEditDialog(TrackDto existing, IArtistService artistSvc, IAlbumService albumSvc)
        {
            _artistSvc = artistSvc;
            _albumSvc = albumSvc;
            Text = existing == null ? "Добавить трек" : "Редактировать трек";
            Size = new Size(460, 380);
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
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _txtTitle = new TextBox { Dock = DockStyle.Fill };
            _txtGenre = new TextBox { Dock = DockStyle.Fill };

            _lblDurationValue = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "—  (выберите файл)",
                ForeColor = Color.Gray
            };

            _cmbArtist = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbAlbum = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

            var filePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight
            };
            _txtFilePath = new TextBox { Width = 240, ReadOnly = true };
            _btnBrowse = new Button { Text = "…", Width = 35, Height = 25 };
            _btnBrowse.Click += OnBrowse;
            filePanel.Controls.AddRange(new Control[] { _txtFilePath, _btnBrowse });

            AddRow(tbl, 0, "Название:", _txtTitle);
            AddRow(tbl, 1, "Жанр:", _txtGenre);
            AddRow(tbl, 2, "Длительность:", _lblDurationValue);
            AddRow(tbl, 3, "Исполнитель:", _cmbArtist);
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

            // Заполнение исполнителей
            var artists = _artistSvc.GetAll();
            _cmbArtist.DataSource = artists;
            _cmbArtist.DisplayMember = "Name";
            _cmbArtist.ValueMember = "ArtistId";
            _cmbArtist.SelectedIndexChanged += (s, e) => ReloadAlbums();
            ReloadAlbums();

            if (existing != null)
            {
                _txtTitle.Text = existing.Title;
                _txtGenre.Text = existing.Genre;
                _txtFilePath.Text = existing.FilePath;
                _durationSeconds = existing.DurationSeconds;
                UpdateDurationLabel(_durationSeconds);
                _cmbArtist.SelectedValue = existing.ArtistId;
                if (existing.AlbumId.HasValue)
                    _cmbAlbum.SelectedValue = existing.AlbumId.Value;

                Result = existing;
            }
        }

        // ── Выбор файла и чтение длительности через NAudio ─────────────────
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

        /// <summary>
        /// Читает длительность аудиофайла через NAudio
        /// </summary>
        private static int ReadDurationFromFile(string filePath)
        {
            try
            {
                using var reader = new AudioFileReader(filePath);
                int seconds = (int)reader.TotalTime.TotalSeconds;
                return seconds > 0 ? seconds : 0;
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
                _lblDurationValue.Text = $"{seconds / 60}:{seconds % 60:D2}  ({seconds} сек.)";
                _lblDurationValue.ForeColor = Color.Black;
            }
            else
            {
                _lblDurationValue.Text = "Не удалось определить длительность";
                _lblDurationValue.ForeColor = Color.Red;
            }
        }

        // ── Загрузка альбомов по исполнителю ────────────────────────────
        private void ReloadAlbums()
        {
            if (_cmbArtist.SelectedValue is not int artistId) return;

            var albums = _albumSvc.GetByArtist(artistId);
            albums.Insert(0, new AlbumDto { AlbumId = 0, Title = "— Без альбома —" });

            _cmbAlbum.DataSource = albums;
            _cmbAlbum.DisplayMember = "Title";
            _cmbAlbum.ValueMember = "AlbumId";
        }

        // ── Сохранение ───────────────────────────────────────────────────
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

            int? albumId = _cmbAlbum.SelectedValue is int aid && aid != 0 ? aid : null;

            Result = new TrackDto
            {
                TrackId = Result?.TrackId ?? 0,
                Title = _txtTitle.Text.Trim(),
                Genre = _txtGenre.Text.Trim(),
                DurationSeconds = _durationSeconds,
                FilePath = _txtFilePath.Text.Trim(),
                ArtistId = (int)_cmbArtist.SelectedValue,
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
