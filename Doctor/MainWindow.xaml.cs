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
        public int? Id { get; set; }
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
        public MainWindow()
        {
            InitializeComponent();

            FunctionsList.Items.Add("Болезни");

            if (DoctorCore.core.user.permission.id == DoctorDataBasePermissions.ADMIN)
            {
                FunctionsList.Items.Add("Лекарства");
                FunctionsList.Items.Add("Рецепт");
            }
            else if (DoctorCore.core.user.permission.id == DoctorDataBasePermissions.MANAGER)
            {
                FunctionsList.Items.Add("Лекарства");
            }
            else if (DoctorCore.core.user.permission.id == DoctorDataBasePermissions.DOCTOR)
            {
                FunctionsList.Items.Add("Лекарства");
                FunctionsList.Items.Add("Рецепт");
            } 
        }

        private ObservableCollection<MedicineView> medicinesCollection = new ObservableCollection<MedicineView>();
        private ObservableCollection<DiseaseView> diseasesCollection = new ObservableCollection<DiseaseView>();

        private void LoadDiseases()
        {
            diseasesCollection.Clear();

            var diseases = DoctorCore.core.wrapper.diseaseServiceObj.LoadAll();

            foreach (var disease in diseases)
            {
                var diseaseView = new DiseaseView
                {
                    Id = disease.id,
                    Name = disease.name,
                    Procedures = disease.procedures,
                    Symptoms = string.Join(", ", disease.Symptoms?.ConvertAll(s => s.name) ?? new List<string>()),
                    Medicines = string.Join(", ", disease.Medicines?.ConvertAll(m => m.name) ?? new List<string>())
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    diseasesCollection.Add(diseaseView);
                });
            }
        }
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

        private void FunctionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            var selectedItem = listBox.SelectedItem as string;

            if (selectedItem == null)
                return;

            string selection = selectedItem;

            switch (selection)
            {
                case "Болезни":
                    var dataGrid = new DataGrid
                    {
                        AutoGenerateColumns = false,
                        IsReadOnly = false,
                        Margin = new Thickness(5),
                        ItemsSource = diseasesCollection
                    };

                    dataGrid.Columns.Add(new DataGridTextColumn { Header = "Id", Binding = new Binding("Id"), IsReadOnly = true });

                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Name",
                        Binding = new Binding("Name") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
                    });

                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Procedures",
                        Binding = new Binding("Procedures") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
                    });

                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Symptoms",
                        Binding = new Binding("Symptoms") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
                    });

                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Medicines",
                        Binding = new Binding("Medicines") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
                    });

                    var rowStyle = new Style(typeof(DataGridRow));
                    rowStyle.Setters.Add(new Setter(DataGridRow.ContextMenuProperty, CreateDeleteMenu()));
                    dataGrid.RowStyle = rowStyle;

                    FunctionContent.Content = dataGrid;

                    dataGrid.CellEditEnding += DataGridDisease_CellEditEnding;

                    LoadDiseases();

                    break;
                case "Лекарства":
                    var dataGridMed = new DataGrid
                    {
                        AutoGenerateColumns = false, 
                        IsReadOnly = !(DoctorCore.core.user.permission.id == DoctorDataBasePermissions.ADMIN 
                        || DoctorCore.core.user.permission.id == DoctorDataBasePermissions.MANAGER),
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

        private ContextMenu CreateDeleteMenu()
        {
            var contextMenu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "Очистить запись (маркировать на удаление)" };
            deleteItem.Click += DeleteMenu_Click;
            contextMenu.Items.Add(deleteItem);
            return contextMenu;
        }

        private void DeleteMenu_Click(object sender, RoutedEventArgs e)
        {
            if (FunctionContent.Content is DataGrid dataGrid)
            {
                var selectedDiseaseView = dataGrid.SelectedItem as DiseaseView;
                if (selectedDiseaseView != null)
                {
                    selectedDiseaseView.Name = "";
                    selectedDiseaseView.Procedures = "";
                    selectedDiseaseView.Symptoms = "";
                    selectedDiseaseView.Medicines = "";

                    var diseaseToDelete = new Disease(selectedDiseaseView.Id, null, null, new List<Medicine>(), new List<Symptom>());

                    var existing = changedDiseases.FirstOrDefault(d => d.id == diseaseToDelete.id);
                    if (existing != null)
                    {
                        changedDiseases.Remove(existing);
                    }
                    changedDiseases.Add(diseaseToDelete);
                }
            }
        }

        private List<Medicine> changedMedicines = new List<Medicine>();
        private List<Disease> changedDiseases = new List<Disease>();

        private void DataGridDisease_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            var editedView = e.Row.Item as DiseaseView;
            if (editedView == null) return;

            var editingElement = e.EditingElement as TextBox;
            string editedText = editingElement?.Text ?? "";

            switch (e.Column.Header.ToString())
            {
                case "Name":
                    editedView.Name = editedText;
                    break;
                case "Procedures":
                    editedView.Procedures = editedText;
                    break;
                case "Symptoms":
                    editedView.Symptoms = editedText;
                    break;
                case "Medicines":
                    editedView.Medicines = editedText;
                    break;
            }

            var medicineNames = string.IsNullOrWhiteSpace(editedView.Medicines)
                ? new List<string>()
                : editedView.Medicines.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

            var medicines = medicineNames.Select(name => new Medicine(name, 0, new List<Medicine>())).ToList();

            var symptomNames = string.IsNullOrWhiteSpace(editedView.Symptoms)
                ? new List<string>()
                : editedView.Symptoms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

            var symptoms = symptomNames.Select(name => new Symptom(name)).ToList();

            int? diseaseId = null;

            if (editedView.GetType().GetProperty("Id") != null)
            {
                var prop = editedView.GetType().GetProperty("Id");
                if (prop != null)
                    diseaseId = prop.GetValue(editedView) as int?;
            }

            var disease = new Disease(diseaseId, editedView.Name, editedView.Procedures, medicines, symptoms);

            var existing = changedDiseases.FirstOrDefault(d => d.id == disease.id);
            if (existing != null)
            {
                existing.name = disease.name;
                existing.procedures = disease.procedures;
                existing.Medicines = disease.Medicines;
                existing.Symptoms = disease.Symptoms;
            }
            else
            {
                changedDiseases.Add(disease);
            }
        }
        private void DataGridMed_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var editedItem = e.Row.Item as MedicineView;
                if (editedItem != null)
                {
                    var editingElement = e.EditingElement as TextBox;
                    string editedText = editingElement?.Text ?? "";

                    if (e.Column.Header.ToString() == "Interchangeable")
                    {
                        editedItem.Interchangeable = editedText;

                        var interchangeableNames = editedText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(s => s.Trim())
                                                            .ToList();

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

                    var medicine = new Medicine(editedItem.Name, editedItem.Quantity, editedItem.InterchangeableList)
                    {
                        id = editedItem.Id
                    };

                    var existing = changedMedicines.FirstOrDefault(m => m.id == medicine.id);
                    if (existing != null)
                    {
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
            try
            {
                DoctorCore.core.wrapper.medicineServiceObj.SaveAll(changedMedicines);
                changedMedicines.Clear();

                DoctorCore.core.wrapper.diseaseServiceObj.SaveAll(changedDiseases);
                changedDiseases.Clear();
            }
            catch (MedicineNotFoundException ex)
            {
                MessageBox.Show($"Ошибка: {ex.InnerException}. Лекарства {ex.Message} нет на складе.",
                                "Ошибка сохранения",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
}