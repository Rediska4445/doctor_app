
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

            StartDatePicker.Text = DateTime.Now.ToString();
            EndDatePicker.Text = DateTime.Now.AddDays(14).ToString();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(this.ScrollViewList as Visual, "Больничная справка");
            }
        }

        private void DiagnosisTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void RecipeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedText = selectedItem.Content.ToString();

                Window childWindow = null;

                switch (selectedText)
                {
                    case "Болезни":
                        var window1 = new DiseasesWindow();
                        if (window1.ShowDialog() == true)
                        {
                            Disease selectedDisease = window1.SelectedDisease;

                            DiagnosisTextBox.Text = selectedDisease.name;
                            SymptomsTextBox.Text = string.Join(", ", selectedDisease.Symptoms);
                            RecipeTextBox.Text = string.Join(", ", selectedDisease.Medicines);
                        }
                        break;
                    case "Лекарства":
                        var window3 = new MedicinesWindow();
                        if (window3.ShowDialog() == true)
                        {
                            //Disease selectedDisease = window3.SelectedDisease;

                            //DiagnosisTextBox.Text = selectedDisease.name;
                        }
                        break;
                    default:
                        break;
                }

                if (childWindow != null)
                    childWindow.Show();
            }
        }
    }
}
