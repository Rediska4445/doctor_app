using System.Windows;
using System.Windows.Media;  

namespace Doctor
{
    public partial class Connect : Window
    {
        public Connect()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string inputName = LoginName.Text.Trim();
            string inputPassword = LoginPassword.Password.Trim();

            System.Diagnostics.Debug.WriteLine($"Login attempt. User: '{inputName}'");

            if (string.IsNullOrEmpty(inputName) || string.IsNullOrEmpty(inputPassword))
            {
                LoginMessage.Text = "Пожалуйста, введите имя и пароль.";
                LoginMessage.Foreground = Brushes.Red;
                return;
            }

            bool userFound = false;
            int userId = 0;
            int permissionId = 0;
            string userName = null;

            string sql = "SELECT id, permission_id, name FROM users WHERE name = @name AND password = @password";

            DoctorCore.core.sqlConnector.Read(sql, reader =>
            {
                userFound = true;
                userId = reader.GetInt32(0);
                permissionId = reader.GetInt32(1);
                userName = reader.GetString(2);
            }, command =>
            {
                command.Parameters.AddWithValue("@name", inputName);
                command.Parameters.AddWithValue("@password", inputPassword);
            });

            if (!userFound)
            {
                LoginMessage.Text = "Пользователь не зарегистрирован или неправильный пароль.";
                LoginMessage.Foreground = Brushes.Red;
                return;
            }

            LoginMessage.Text = "Вход выполнен успешно!";
            LoginMessage.Foreground = Brushes.Green;

            MainWindow main = new MainWindow();
            main.Title = "Doctor - " + DoctorCore.core.DeterminePermission(permissionId).name;
            main.Show();
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string regName = RegName.Text.Trim();
            string regPassword = RegPassword.Password.Trim();

            if (string.IsNullOrEmpty(regName) || string.IsNullOrEmpty(regPassword))
            {
                RegisterMessage.Text = "Пожалуйста, заполните все поля.";
                RegisterMessage.Foreground = Brushes.Red;
                return;
            }

            bool exists = false;
            string checkSql = "SELECT COUNT(*) FROM users WHERE name = @name";

            DoctorCore.core.sqlConnector.Read(checkSql, reader =>
            {
                int count = reader.GetInt32(0);
                exists = count > 0;
            }, command =>
            {
                command.Parameters.AddWithValue("@name", regName);
            });

            if (exists)
            {
                RegisterMessage.Text = "Пользователь с таким именем уже существует.";
                RegisterMessage.Foreground = Brushes.Red;
                return;
            }

            string insertSql = "INSERT INTO users (permission_id, name, password) VALUES (3, @name, @password)";
            DoctorCore.core.sqlConnector.Push(insertSql, command =>
            {
                command.Parameters.AddWithValue("@name", regName);
                command.Parameters.AddWithValue("@password", regPassword);
            });

            RegisterMessage.Text = "Регистрация прошла успешно!";
            RegisterMessage.Foreground = Brushes.Green;
        }
    }
}