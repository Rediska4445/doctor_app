using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Doctor
{
    public class WDiseaseView
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Procedures { get; set; }
        public string Symptoms { get; set; }
        public string Medicines { get; set; }
    }

    /// <summary>
    /// Логика взаимодействия для DiseasesWindow.xaml
    /// </summary>
    public partial class DiseasesWindow : Window
    {
        private ObservableCollection<DiseaseView> diseasesCollection = new ObservableCollection<DiseaseView>();

        public DiseasesWindow()
        {
            InitializeComponent();
            DiseasesDataGrid.ItemsSource = diseasesCollection;
            DiseasesDataGrid.IsReadOnly = true;

            LoadDiseases();
        }

        private void LoadDiseases()
        {
            diseasesCollection.Clear();

            var diseases = DoctorCore.core.wrapper.diseaseServiceObj.LoadAll();

            foreach (var disease in diseases)
            {
                diseasesCollection.Add(new DiseaseView
                {
                    Id = disease.id,
                    Name = disease.name,
                    Procedures = disease.procedures,
                    Symptoms = string.Join(", ", disease.Symptoms?.ConvertAll(s => s.name) ?? new System.Collections.Generic.List<string>()),
                    Medicines = string.Join(", ", disease.Medicines?.ConvertAll(m => m.name) ?? new System.Collections.Generic.List<string>())
                });
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public Disease SelectedDisease { get; private set; } = null;

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DiseasesDataGrid.SelectedItem is DiseaseView selectedView)
            {
                SelectedDisease = new Disease(
                    selectedView.Id,
                    selectedView.Name,
                    selectedView.Procedures,
                    selectedView.Symptoms,
                    selectedView.Medicines
                );

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Выберите болезнь в таблице.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
