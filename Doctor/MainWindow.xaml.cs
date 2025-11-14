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
    class DiseaseView : INotifyPropertyChanged
    {
        private string name;
        private string procedures;
        private string symptoms;
        private string medicines;

        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(nameof(Name)); }
        }
        public string Procedures
        {
            get => procedures;
            set { procedures = value; OnPropertyChanged(nameof(Procedures)); }
        }
        public string Symptoms
        {
            get => symptoms;
            set { symptoms = value; OnPropertyChanged(nameof(Symptoms)); }
        }
        public string Medicines
        {
            get => medicines;
            set { medicines = value; OnPropertyChanged(nameof(Medicines)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    class MedicineView : INotifyPropertyChanged
    {
        public MedicineView()
        {
            Id = null; 
        }

        public int? Id { get; set; }

        private string name;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(nameof(Name)); }
        }

        private int quantity;
        public int Quantity
        {
            get => quantity;
            set { quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        private string interchangeable;

        public string Interchangeable
        {
            get => interchangeable;
            set { interchangeable = value; OnPropertyChanged(nameof(Interchangeable)); }
        }

        public List<Medicine> InterchangeableList { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                Application.Current.Dispatcher.Invoke(() => {
                    medicinesCollection.Add(medView);
                    ConsoleTextBox.Text = "String: " + medView.Interchangeable + " LIST: "
                    + (medView.InterchangeableList == null ? "null" : medView.InterchangeableList.Count.ToString()) + "\n";
                });
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
                        IsReadOnly = DoctorCore.core.user.permission.id == DoctorDataBasePermissions.ADMIN 
                        || DoctorCore.core.user.permission.id == DoctorDataBasePermissions.MANAGER,
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
                        Binding = new Binding("Interchangeable")
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
                    string editedText = editingElement?.Text ?? "";

                    // Обновляем строку interchangeable и список взаимозаменяемых лекарств
                    if (e.Column.Header.ToString() == "Interchangeable")
                    {
                        editedItem.Interchangeable = editedText;
                        // Парсим строку в список
                        var interchangeableNames = editedText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(s => s.Trim())
                                                            .ToList();

                        // Обновляем InterchangeableList на основании парсинга
                        editedItem.InterchangeableList = medicinesCollection
                            .Where(m => interchangeableNames.Contains(m.Name) && m.Id != editedItem.Id)
                            .Select(m => new Medicine(m.Name, m.Quantity, m.InterchangeableList) { id = m.Id })
                            .ToList();
                    }
                    else if (e.Column.Header.ToString() == "Name")
                    {
                        editedItem.Name = editedText;
                    }
                    else if (e.Column.Header.ToString() == "Quantity")
                    {
                        if (int.TryParse(editedText, out int qty))
                            editedItem.Quantity = qty;
                    }

                    // Создаём объект Medicine для сохранения
                    var medicine = new Medicine(editedItem.Name, editedItem.Quantity, editedItem.InterchangeableList)
                    {
                        id = editedItem.Id
                    };

                    // Добавляем в список изменений
                    var existing = changedMedicines.FirstOrDefault(m => m.id == medicine.id);
                    if (existing != null)
                    {
                        // Обновляем данные
                        existing.name = medicine.name;
                        existing.quantity = medicine.quantity;
                        existing.interchangleMedicineList = medicine.interchangleMedicineList;
                    }
                    else
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
                ConsoleTextBox.Text += "\nSUKA: " + med.id + " " + med.name + " " + med.quantity + "COUNT: "
                    + (med.interchangleMedicineList == null ? "lol" : med.interchangleMedicineList.Count.ToString()) + "\n";
            }

            DoctorCore.core.wrapper.medicineServiceObj.SaveAll(changedMedicines);

            changedMedicines.Clear();
        }
    }
}
