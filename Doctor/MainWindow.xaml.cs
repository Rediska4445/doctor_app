using SqlExplorer;
using System.Collections.Generic;
using System.Windows;

namespace Doctor
{
    public partial class MainWindow : Window
    {
        private static readonly List<string> SqlQueries = new List<string>
        {
            @"
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'doctor_test')
            BEGIN
                CREATE DATABASE doctor_test;
            END;
            ",
            "USE doctor_test;",
            @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'permissions')
            BEGIN
                CREATE TABLE permissions (
                    id INT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL
                );
            END;
            ",
            @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
            BEGIN
                CREATE TABLE users (
                    id INT PRIMARY KEY,
                    permission_id INT,
                    name VARCHAR(32),
                    password VARCHAR(64),
                    FOREIGN KEY (permission_id) REFERENCES permissions(id)
                );
            END;
            ",
            @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'disease')
            BEGIN
                CREATE TABLE disease (
                    id INT PRIMARY KEY,
                    name VARCHAR(96),
                    procedures VARCHAR(1024)
                );
            END;
            ",
            @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'symptoms')
            BEGIN
                CREATE TABLE symptoms (
                    id INT PRIMARY KEY,
                    name VARCHAR(256)
                );
            END;
            ",
            @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'medicines')
            BEGIN
                CREATE TABLE medicines (
                    id INT PRIMARY KEY,
                    name VARCHAR(96),
                    quantity INT
                );
            END;
            ",
            @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'diseases_symptoms')
            BEGIN
                CREATE TABLE diseases_symptoms (
                    disease_id INT,
                    symptom_id INT,
                    PRIMARY KEY (disease_id, symptom_id),
                    FOREIGN KEY (disease_id) REFERENCES disease(id),
                    FOREIGN KEY (symptom_id) REFERENCES symptoms(id)
                );
            END;
            ",
            @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'diseases_medicines')
            BEGIN
                CREATE TABLE diseases_medicines (
                    medicines_id INT,
                    diseases_id INT,
                    quantity INT,
                    PRIMARY KEY (medicines_id, diseases_id),
                    FOREIGN KEY (medicines_id) REFERENCES medicines(id),
                    FOREIGN KEY (diseases_id) REFERENCES disease(id)
                );
            END;
            ",
            @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'interchangeable_medicines')
            BEGIN
                CREATE TABLE interchangeable_medicines (
                    medicine_id INT,
                    interchangeable_id INT,
                    PRIMARY KEY (medicine_id, interchangeable_id),
                    FOREIGN KEY (medicine_id) REFERENCES medicines(id),
                    FOREIGN KEY (interchangeable_id) REFERENCES medicines(id)
                );
            END;
            "
        };

        public static SqlConnector sqlConnector = new SqlConnector();

        public MainWindow()
        {
            InitializeComponent();

            foreach (var query in SqlQueries)
            {
                sqlConnector.Push(query);
            }
        }
    }
}
