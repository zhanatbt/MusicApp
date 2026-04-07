using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using MusicApp.BLL.Interfaces;
using MusicApp.Forms;

namespace MusicApp.UI.Forms
{
    public class LoginForm : Form
    {
        private readonly IAuthService _auth;
        private TextBox _txtUser, _txtPass;
        private Button _btnLogin, _btnRegister;
        private Label _lblError;

        public LoginForm(IAuthService auth)
        {
            _auth = auth;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "MusicApp — Вход";
            Size = new Size(360, 280);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 20, 30, 20),
                RowCount = 6,
                ColumnCount = 1
            };

            var title = new Label { Text = "🎵 MusicApp", Font = new Font("Segoe UI", 16, FontStyle.Bold),
                                    TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };

            _txtUser = new TextBox { PlaceholderText = "Логин", Dock = DockStyle.Fill };
            _txtPass = new TextBox { PlaceholderText = "Пароль", UseSystemPasswordChar = true, Dock = DockStyle.Fill };
            _lblError = new Label { ForeColor = Color.Red, Dock = DockStyle.Fill, AutoSize = false };

            _btnLogin = new Button { Text = "Войти", Dock = DockStyle.Fill,
                                     BackColor = Color.FromArgb(70, 130, 180), ForeColor = Color.White,
                                     FlatStyle = FlatStyle.Flat, Height = 36 };
            _btnRegister = new Button { Text = "Регистрация", Dock = DockStyle.Fill,
                                        FlatStyle = FlatStyle.Flat, Height = 36 };

            _btnLogin.Click    += OnLogin;
            _btnRegister.Click += OnRegister;

            panel.Controls.Add(title);
            panel.Controls.Add(_txtUser);
            panel.Controls.Add(_txtPass);
            panel.Controls.Add(_lblError);
            panel.Controls.Add(_btnLogin);
            panel.Controls.Add(_btnRegister);

            Controls.Add(panel);
            AcceptButton = _btnLogin;
        }

        private void OnLogin(object sender, EventArgs e)
        {
            _lblError.Text = "";
            var user = _auth.Login(_txtUser.Text.Trim(), _txtPass.Text);
            if (user == null) { _lblError.Text = "Неверный логин или пароль."; return; }

            Hide();
            if (user.Role == "Admin")
                Program.Services.GetRequiredService<AdminForm>().ShowDialog();
            else
            {
                var main = Program.Services.GetRequiredService<MainForm>();
                main.SetCurrentUser(user);
                main.ShowDialog();
            }
            Show();
            _txtPass.Clear();
        }

        private void OnRegister(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtUser.Text) || string.IsNullOrWhiteSpace(_txtPass.Text))
            { _lblError.Text = "Введите логин и пароль."; return; }

            try
            {
                _auth.Register(_txtUser.Text.Trim(), _txtPass.Text);
                MessageBox.Show("Регистрация успешна! Теперь войдите.", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { _lblError.Text = ex.Message; }
        }
    }
}
