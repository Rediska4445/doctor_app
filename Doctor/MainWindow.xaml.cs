using SqlExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static Doctor.DoctorDataBaseWrapper;

namespace Doctor
{
    class DiseaseView
    {
        public string Name { get; set; }
        public string Procedures { get; set; }
        public string Symptoms { get; set; }
        public string Medicines { get; set; }
    }

    class MedicineView
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public string Interchangeable { get; set; }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private ObservableCollection<MedicineView> medicinesCollection = new ObservableCollection<MedicineView>();

        private ObservableCollection<DiseaseView> diseasesCollection = new ObservableCollection<DiseaseView>();

        private void LoadDiseases()
        {
            diseasesCollection.Clear();

            DoctorCore.core.wrapper.diseaseServiceObj.LoadAllDiseases(
                onDiseaseLoaded: disease =>
                {
                    // Преобразуем каждый disease в представление для таблицы
                    var diseaseView = new DiseaseView
                    {
                        Name = disease.name,
                        Procedures = disease.procedures,
                        Symptoms = string.Join(", ", disease.Symptoms?.ConvertAll(s => s.name) ?? new List<string>()),
                        Medicines = string.Join("; ", disease.Medicines?.ConvertAll(m =>
                        {
                            var interNames = m.interchangleMedicineList != null && m.interchangleMedicineList.Count > 0
                                ? $"(заменяемые: {string.Join(", ", m.interchangleMedicineList.ConvertAll(im => im.name))})"
                                : "";
                            return $"{m.name} x{m.quantity} {interNames}";
                        }) ?? new List<string>())
                    };

                    // Добавляем в коллекцию (в UI потоке)
                    Application.Current.Dispatcher.Invoke(() => diseasesCollection.Add(diseaseView));
                },
                onCompleted: () =>
                {
                    // Здесь можно обработать окончание загрузки, если нужно
                });
        }

        private void LoadMedicines()
        {
            medicinesCollection.Clear();

            DoctorCore.core.wrapper.medicineServiceObj.GetAllMedicines().ForEach(med =>
            {
                var medView = new MedicineView
                {
                    Name = med.name,
                    Quantity = med.quantity,
                    Interchangeable = med.interchangleMedicineList != null && med.interchangleMedicineList.Count > 0
                        ? string.Join(", ", med.interchangleMedicineList.ConvertAll(i => i.name))
                        : string.Empty
                };
                Application.Current.Dispatcher.Invoke(() => medicinesCollection.Add(medView));
            });
        }

        private void FunctionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            var selectedItem = listBox.SelectedItem as ListBoxItem;

            if (selectedItem == null)
                return;

            string selection = selectedItem.Content.ToString();

            switch (selection)
            {
                case "Болезни":
                    var dataGrid = new DataGrid
                    {
                        AutoGenerateColumns = true,
                        IsReadOnly = true,
                        Margin = new Thickness(5),
                        ItemsSource = diseasesCollection
                    };

                    FunctionContent.Content = dataGrid;

                    diseasesCollection.Clear();
                    LoadDiseases();

                    break;

                case "Лекарства":
                    var dataGridMed = new DataGrid
                    {
                        AutoGenerateColumns = true,
                        IsReadOnly = true,
                        Margin = new Thickness(5),
                        ItemsSource = medicinesCollection
                    };

                    FunctionContent.Content = dataGridMed;
                    medicinesCollection.Clear();
                    LoadMedicines();

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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Connect connectWindow = new Connect();
            connectWindow.Show(); 
            this.Close();
        }
    }
}
