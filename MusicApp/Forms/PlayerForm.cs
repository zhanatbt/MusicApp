using NAudio.Wave;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicApp.Forms
{
    public partial class PlayerForm : Form
    {
        private IWavePlayer? _waveOut;
        private AudioFileReader? _audioFileReader;
        private System.Windows.Forms.Timer _timer;

        public Label lblNowPlaying { get; private set; } = null!;
        public TrackBar trackProgress { get; private set; } = null!;
        public Label lblTime { get; private set; } = null!;

        public PlayerForm()
        {
            InitializeComponent();
            SetupPlayer();
        }

        private void SetupPlayer()
        {
            this.Text = "Music Player";
            this.Size = new Size(560, 320);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10f);

            // Название трека (поддерживает несколько исполнителей)
            lblNowPlaying = new Label
            {
                Text = "Трек не выбран",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            // Прогресс-бар
            trackProgress = new TrackBar
            {
                Dock = DockStyle.Top,
                Height = 45,
                Minimum = 0,
                Maximum = 1000,
                TickFrequency = 50,
                BackColor = Color.FromArgb(28, 28, 28)
            };
            trackProgress.Scroll += Progress_Scroll;

            // Время
            lblTime = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "00:00 / 00:00",
                ForeColor = Color.LightGray
            };

            // Панель кнопок
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 80,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(15, 10, 15, 10)
            };

            var btnPlayPause = new Button
            {
                Text = "▶",
                Width = 80,
                Height = 55,
                Font = new Font("Segoe UI", 20f)
            };

            var btnStop = new Button
            {
                Text = "⏹",
                Width = 60,
                Height = 55,
                Font = new Font("Segoe UI", 16f)
            };

            btnPlayPause.Click += BtnPlayPause_Click;
            btnStop.Click += (s, e) => Stop();

            btnPanel.Controls.Add(btnPlayPause);
            btnPanel.Controls.Add(btnStop);

            // Громкость
            var volumePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(15, 5, 15, 5)
            };

            volumePanel.Controls.Add(new Label
            {
                Text = "Громкость:",
                Width = 80,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft
            });

            var trackVolume = new TrackBar
            {
                Width = 280,
                Minimum = 0,
                Maximum = 100,
                Value = 80
            };
            trackVolume.Scroll += (s, e) =>
            {
                if (_waveOut != null)
                    _waveOut.Volume = trackVolume.Value / 100f;
            };

            volumePanel.Controls.Add(trackVolume);

            // Добавляем элементы в правильном порядке
            this.Controls.Add(volumePanel);
            this.Controls.Add(btnPanel);
            this.Controls.Add(lblTime);
            this.Controls.Add(trackProgress);
            this.Controls.Add(lblNowPlaying);

            // Таймер обновления прогресса
            _timer = new System.Windows.Forms.Timer { Interval = 400 };
            _timer.Tick += Timer_Tick;
        }

        /// <summary>
        /// Запуск воспроизведения
        /// </summary>
        public void Play(string filePath, string title, string artistDisplay)
        {
            Stop();

            try
            {
                _audioFileReader = new AudioFileReader(filePath);
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioFileReader);
                _waveOut.Play();

                lblNowPlaying.Text = $"{artistDisplay} — {title}";
                this.Text = $"Сейчас играет — {title}";

                _timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка воспроизведения:\n" + ex.Message,
                               "Ошибка",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioFileReader?.Dispose();

            _waveOut = null;
            _audioFileReader = null;

            trackProgress.Value = 0;
            lblTime.Text = "00:00 / 00:00";
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_audioFileReader == null) return;

            var current = _audioFileReader.CurrentTime;
            var total = _audioFileReader.TotalTime;

            trackProgress.Maximum = (int)total.TotalMilliseconds;
            trackProgress.Value = Math.Min((int)current.TotalMilliseconds, trackProgress.Maximum);

            lblTime.Text = $"{current:mm\\:ss} / {total:mm\\:ss}";
        }

        private void Progress_Scroll(object? sender, EventArgs e)
        {
            if (_audioFileReader != null)
                _audioFileReader.CurrentTime = TimeSpan.FromMilliseconds(trackProgress.Value);
        }

        private void BtnPlayPause_Click(object? sender, EventArgs e)
        {
            if (_waveOut == null) return;

            var btn = (Button)sender!;
            if (_waveOut.PlaybackState == PlaybackState.Playing)
            {
                _waveOut.Pause();
                btn.Text = "▶";
            }
            else
            {
                _waveOut.Play();
                btn.Text = "⏸";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Stop();
            base.OnFormClosing(e);
        }
    }
}