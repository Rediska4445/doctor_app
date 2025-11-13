using System.Windows;
using System.Windows.Media;  

namespace Doctor
{
    public partial class Connect : Window
    {
        public Connect()
        {
            InitializeComponent();

            DoctorCore.core.PrepareSqlWorkspace();
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

            int? userId = null;
            string sql = "SELECT id FROM users WHERE name = @name AND password = @password";

            DoctorCore.core.sqlConnector.Read(sql, reader =>
            {
                userId = reader.GetInt32(0);
            }, cmd =>
            {
                cmd.Parameters.AddWithValue("@name", inputName);
                cmd.Parameters.AddWithValue("@password", inputPassword);
            });

            if (userId == null)
            {
                LoginMessage.Text = "Пользователь не зарегистрирован или неправильный пароль.";
                LoginMessage.Foreground = Brushes.Red;
                return;
            }

            User user = DoctorCore.core.wrapper.userServiceObj.Get(userId.Value);

            LoginMessage.Text = "Вход выполнен успешно!";
            LoginMessage.Foreground = Brushes.Green;

            MainWindow main = new MainWindow();
            main.Title = "Doctor - " + user.permission.name;
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

            if (DoctorCore.core.wrapper.userServiceObj.IsExists(regName))
            {
                RegisterMessage.Text = "Пользователь с таким именем уже существует.";
                RegisterMessage.Foreground = Brushes.Red;
                return;
            }

            Permission defaultPermission = DoctorCore.core.wrapper.permissionServiceObj.Get(3); // Viewer

            User newUser = new User(null, defaultPermission, regName, regPassword);
            DoctorCore.core.wrapper.userServiceObj.Add(newUser);

            RegisterMessage.Text = "Регистрация прошла успешно!";
            RegisterMessage.Foreground = Brushes.Green;
        }
    }
}