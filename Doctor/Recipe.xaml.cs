
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Doctor
{
    /// <summary>
    /// Логика взаимодействия для Recipe.xaml
    /// </summary>
    public partial class Recipe : UserControl
    {
        public Recipe()
        {
            InitializeComponent();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(this.ScrollViewList as Visual, "Больничная справка");
            }
        }
    }
}
