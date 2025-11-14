using SqlExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        public int? Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public string Interchangeable { get; set; }
        public List<Medicine> InterchangeableList { get; set; }
    }

    public partial class MainWindow : Window
    {
        private void LoadMedicines()
        {
            medicinesCollection.Clear();

            DoctorCore.core.wrapper.medicineServiceObj.GetAllMedicines().ForEach(med =>
            {
                var medView = new MedicineView
                {
                    Id = med.id,
                    Name = med.name,
                    Quantity = med.quantity,
                    Interchangeable = string.Join(", ", med.interchangleMedicineList.ConvertAll(i => i.name)),
                    InterchangeableList = med.interchangleMedicineList
                };
                Application.Current.Dispatcher.Invoke(() => medicinesCollection.Add(medView));
            });
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private ObservableCollection<MedicineView> medicinesCollection = new ObservableCollection<MedicineView>();

        private ObservableCollection<DiseaseView> diseasesCollection = new ObservableCollection<DiseaseView>();

        private void LoadDiseases()
        {
            diseasesCollection.Clear();

            DoctorCore.core.wrapper.diseaseServiceObj.LoadAll(
                onDiseaseLoaded: disease =>
                {
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

                    Application.Current.Dispatcher.Invoke(() => diseasesCollection.Add(diseaseView));
                },
                onCompleted: () =>
                {

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
                        AutoGenerateColumns = false, 
                        IsReadOnly = false,
                        Margin = new Thickness(5),
                        ItemsSource = medicinesCollection,
                    };

                    dataGridMed.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Id",
                        Binding = new Binding("Id") { Mode = BindingMode.OneWay }
                    });

                    dataGridMed.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Name",
                        Binding = new Binding("Name")
                        {
                            Mode = BindingMode.TwoWay,
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                        }
                    });

                    dataGridMed.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Quantity",
                        Binding = new Binding("Quantity")
                        {
                            Mode = BindingMode.TwoWay,
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                        }
                    });

                    dataGridMed.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Interchangeable",
                        Binding = new Binding("InterchangeableNames")
                        {
                            Mode = BindingMode.TwoWay,
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                        }
                    });

                    dataGridMed.CellEditEnding += DataGridMed_CellEditEnding;

                    FunctionContent.Content = dataGridMed;
                    medicinesCollection.Clear();

                    LoadMedicines();
                    break;
                case "Рецепт":
                    var recipeView = new Recipe();

                    ScrollViewer scrollViewer = new ScrollViewer
                    {
                        Content = recipeView,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    };

                    FunctionContent.Content = scrollViewer;
                    break;
                default:
                    FunctionContent.Content = null;
                    break;

            }
        }

        private List<Medicine> changedMedicines = new List<Medicine>();

        private void DataGridMed_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var editedItem = e.Row.Item as MedicineView;
                if (editedItem != null)
                {
                    var editingElement = e.EditingElement as TextBox;
                    string text = editingElement?.Text ?? "";

                    var interchangeableList = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(s => s.Trim())
                                                 .ToList();

                    var medicine = new Medicine(editedItem.Name, editedItem.Quantity, editedItem.InterchangeableList)
                    {
                        id = editedItem.Id
                    };

                    if (!changedMedicines.Any(m => m.name == medicine.name && m.quantity == medicine.quantity))
                    {
                        changedMedicines.Add(medicine);
                    }

                    ConsoleTextBox.Text += $"Изменена запись: {editedItem.Name} с количеством {medicine.quantity}\n";
                }
            }
        }


        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Connect connectWindow = new Connect();
            connectWindow.Show();
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ConsoleTextBox.Text += changedMedicines.Count;

            foreach(Medicine med in changedMedicines)
            {
                ConsoleTextBox.Text += "SUKA: " + med.id + " " + med.name + " " + med.quantity + "\n";
            }

            DoctorCore.core.wrapper.medicineServiceObj.SaveAll(changedMedicines);

            changedMedicines.Clear();
        }
    }
}
