using SqlExplorer;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Doctor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FunctionsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            var selectedItem = listBox.SelectedItem as ListBoxItem;

            if (selectedItem == null)
                return;

            string selection = selectedItem.Content.ToString();

            switch (selection)
            {
                case "Болезни":
                case "Лекарства":
                    var dataGrid = new DataGrid
                    {
                        AutoGenerateColumns = true,
                        IsReadOnly = true,
                        Margin = new Thickness(5)
                    };



                    FunctionContent.Content = dataGrid;
                    break;

                case "Рецепт":
                    var formGrid = new Grid();
                    FunctionContent.Content = formGrid;
                    break;

                default:
                    FunctionContent.Content = null;
                    break;
            }
        }
    }
}
