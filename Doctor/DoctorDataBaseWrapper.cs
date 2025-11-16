using Microsoft.Data.SqlClient;
using SqlExplorer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Documents;

namespace Doctor
{
    public static class DoctorDataBasePermissions
    {
        public static readonly int ADMIN = 1;
        public static readonly int MANAGER = 2;
        public static readonly int DOCTOR = 3;
        public static readonly int VIEWER = 4;
    }

    public class DoctorDataBaseWrapper
    {
        private SqlConnector sqlConnectorPointer;

        public UserService userServiceObj;
        public PermissionService permissionServiceObj;

        public DiseaseService diseaseServiceObj;
        public MedicineService medicineServiceObj;

        public DoctorDataBaseWrapper(SqlConnector sqlConnectorPointer)
        {
            this.sqlConnectorPointer = sqlConnectorPointer;

            userServiceObj = new UserService(this.sqlConnectorPointer);
            permissionServiceObj = new PermissionService(this.sqlConnectorPointer);
            diseaseServiceObj = new DiseaseService(this.sqlConnectorPointer);
            medicineServiceObj = new MedicineService(this.sqlConnectorPointer);
        }

        public class UserService
        {
            private SqlConnector sqlConnectorPointer;

            public UserService(SqlConnector sqlConnectorPointer)
            {
                this.sqlConnectorPointer = sqlConnectorPointer;
            }

            public User Get(int id)
            {
                User user = null;
                string sql = @"
                    SELECT u.id, u.name, u.password, p.id, p.name 
                    FROM users u
                    JOIN permissions p ON u.permission_id = p.id
                    WHERE u.id = @id";

                sqlConnectorPointer.Read(sql, reader =>
                {
                    int userId = reader.GetInt32(0);
                    string userName = reader.GetString(1);
                    string userPassword = reader.GetString(2);
                    int permissionId = reader.GetInt32(3);
                    string permissionName = reader.GetString(4);

                    var permission = new Permission(permissionId, permissionName);
                    user = new User(userId, permission, userName, userPassword);
                }, cmd =>
                {
                    cmd.Parameters.AddWithValue("@id", id);
                });

                return user;
            }

            public void Add(User user)
            {
                if (IsExists(user.name))
                {
                    throw new InvalidOperationException("Пользователь с таким именем уже существует.");
                }

                string insertSql = @"
                INSERT INTO users (name, password, permission_id) 
                VALUES (@name, @password, @permission_id)";

                sqlConnectorPointer.Push(insertSql, cmd =>
                {
                    cmd.Parameters.AddWithValue("@name", user.name);
                    cmd.Parameters.AddWithValue("@password", user.password);
                    cmd.Parameters.AddWithValue("@permission_id", user.permission.id);
                });
            }

            public void Update(User user)
            {
                string updateSql = @"
                UPDATE users 
                SET name = @name, password = @password, permission_id = @permission_id 
                WHERE id = @id";

                sqlConnectorPointer.Push(updateSql, cmd =>
                {
                    cmd.Parameters.AddWithValue("@name", user.name);
                    cmd.Parameters.AddWithValue("@password", user.password);
                    cmd.Parameters.AddWithValue("@permission_id", user.permission.id);
                    cmd.Parameters.AddWithValue("@id", user.id.Value);
                });
            }

            public void Set(User user)
            {
                if (user.id.HasValue)
                {
                    string checkSql = "SELECT COUNT(*) FROM users WHERE id = @id";
                    int count = 0;

                    sqlConnectorPointer.Read(checkSql, reader =>
                    {
                        count = reader.GetInt32(0);
                    }, cmd =>
                    {
                        cmd.Parameters.AddWithValue("@id", user.id.Value);
                    });

                    if (count > 0)
                    {
                        Update(user);
                        return;
                    }
                }
                Add(user);
            }

            public bool IsExists(string name)
            {
                bool exists = false;
                string sql = "SELECT COUNT(*) FROM users WHERE name = @name";

                sqlConnectorPointer.Read(sql, reader =>
                {
                    exists = reader.GetInt32(0) > 0;
                }, cmd =>
                {
                    cmd.Parameters.AddWithValue("@name", name);
                });

                return exists;
            }

            public User FindByNameAndPassword(string name, string password)
            {
                User user = null;
                string sql = "SELECT id FROM users WHERE name = @name AND password = @password";

                sqlConnectorPointer.Read(sql, reader =>
                {
                    int userId = reader.GetInt32(0);
                    user = Get(userId);
                }, cmd =>
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@password", password);
                });

                return user;
            }
        }

        public class PermissionService
        {
            private SqlConnector sqlConnectorPointer;

            public PermissionService(SqlConnector sqlConnectorPointer)
            {
                this.sqlConnectorPointer = sqlConnectorPointer;
            }

            public Permission Get(int id)
            {
                Permission permission = null;
                string sql = "SELECT id, name FROM permissions WHERE id = @id";

                sqlConnectorPointer.Read(sql, reader =>
                {
                    int permId = reader.GetInt32(0);
                    string permName = reader.GetString(1);
                    permission = new Permission(permId, permName);
                }, cmd =>
                {
                    cmd.Parameters.AddWithValue("@id", id);
                });

                return permission;
            }

            public void Set(Permission permission)
            {
                string checkSql = "SELECT COUNT(*) FROM permissions WHERE id = @id";
                int count = 0;

                sqlConnectorPointer.Read(checkSql, reader =>
                {
                    count = reader.GetInt32(0);
                }, cmd =>
                {
                    cmd.Parameters.AddWithValue("@id", permission.id);
                });

                if (count > 0)
                {
                    string updateSql = "UPDATE permissions SET name = @name WHERE id = @id";
                    sqlConnectorPointer.Push(updateSql, cmd =>
                    {
                        cmd.Parameters.AddWithValue("@name", permission.name);
                        cmd.Parameters.AddWithValue("@id", permission.id);
                    });
                }
                else
                {
                    string insertSql = "INSERT INTO permissions (id, name) VALUES (@id, @name)";
                    sqlConnectorPointer.Push(insertSql, cmd =>
                    {
                        cmd.Parameters.AddWithValue("@id", permission.id);
                        cmd.Parameters.AddWithValue("@name", permission.name);
                    });
                }
            }
        }

        public class MedicineNotFoundException : Exception
        {
            public MedicineNotFoundException() { }

            public MedicineNotFoundException(string message) : base(message) { }

            public MedicineNotFoundException(string message, Exception innerException) : base(message, innerException) { }
        }

        public class DiseaseService
        {
            private SqlConnector sqlConnectorPointer;

            public DiseaseService(SqlConnector sqlConnectorPointer)
            {
                this.sqlConnectorPointer = sqlConnectorPointer;
            }

            public List<Disease> LoadAll()
            {
                var diseases = new List<Disease>();
                 
                sqlConnectorPointer.Read("SELECT id, name, procedures FROM disease", reader =>
                {
                    diseases.Add(new Disease(reader.GetString(1), reader.GetString(2), new List<Medicine>(), new List<Symptom>())
                    {
                        id = reader.GetInt32(0)
                    });
                });

                var symptoms = new Dictionary<int, List<Symptom>>();
                sqlConnectorPointer.Read(@"
                    SELECT ds.disease_id, s.id, s.name
                    FROM diseases_symptoms ds
                    JOIN symptoms s ON ds.symptom_id = s.id", reader =>
                {
                    int diseaseId = reader.GetInt32(0);
                    var symptom = new Symptom(reader.GetString(2)) { id = reader.GetInt32(1) };

                    if (!symptoms.ContainsKey(diseaseId))
                        symptoms[diseaseId] = new List<Symptom>();

                    symptoms[diseaseId].Add(symptom);
                });

                var medicines = new Dictionary<int, List<Medicine>>();
                sqlConnectorPointer.Read(@"
                    SELECT dm.diseases_id, m.id, m.name, dm.quantity
                    FROM diseases_medicines dm
                    JOIN medicines m ON dm.medicines_id = m.id", reader =>
                {
                    int diseaseId = reader.GetInt32(0);
                    var medicine = new Medicine(reader.GetString(2), reader.GetInt32(3), new List<Medicine>())
                    {
                        id = reader.GetInt32(1)
                    };

                    if (!medicines.ContainsKey(diseaseId))
                        medicines[diseaseId] = new List<Medicine>();

                    medicines[diseaseId].Add(medicine);
                });

                var interchangeable = new Dictionary<int, List<Medicine>>();
                sqlConnectorPointer.Read(@"
                    SELECT im.medicine_id, m.id, m.name, m.quantity
                    FROM interchangeable_medicines im
                    JOIN medicines m ON im.interchangeable_id = m.id", reader =>
                {
                    int medicineId = reader.GetInt32(0);
                    var med = new Medicine(reader.GetString(2), reader.GetInt32(3), new List<Medicine>())
                    {
                        id = reader.GetInt32(1)
                    };

                    if (!interchangeable.ContainsKey(medicineId))
                        interchangeable[medicineId] = new List<Medicine>();

                    interchangeable[medicineId].Add(med);
                });

                // Сопоставление симптомов и лекарств с болезнями
                foreach (var disease in diseases)
                {
                    if (symptoms.TryGetValue(disease.id ?? 0, out var symList))
                        disease.Symptoms = symList;

                    if (medicines.TryGetValue(disease.id ?? 0, out var medList))
                    {
                        foreach (var med in medList)
                        {
                            if (interchangeable.TryGetValue(med.id ?? 0, out var interList))
                                med.interchangleMedicineList = interList;
                        }
                        disease.Medicines = medList;
                    }
                }

                return diseases;
            }

            private string NormalizeSymptomName(string rawName)
            {
                var parts = rawName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim())
                                   .Where(s => !string.IsNullOrEmpty(s));
                return string.Join(", ", parts);
            }

            public void SaveAll(List<Disease> diseases)
            {
                if (diseases == null)
                    return;

                if(diseases.Count == 0) 
                    return;

                var allMedicines = new Dictionary<string, Medicine>(StringComparer.OrdinalIgnoreCase);
                sqlConnectorPointer.Read("SELECT id, name, quantity FROM medicines", reader =>
                {
                    string name = reader.GetString(1);
                    allMedicines[name] = new Medicine(name, reader.GetInt32(2), new List<Medicine>())
                    {
                        id = reader.GetInt32(0)
                    };
                });

                var allSymptoms = new Dictionary<string, Symptom>(StringComparer.OrdinalIgnoreCase);
                sqlConnectorPointer.Read("SELECT id, name FROM symptoms", reader =>
                {
                    string normalizedName = NormalizeSymptomName(reader.GetString(1));
                    allSymptoms[normalizedName] = new Symptom(normalizedName) { id = reader.GetInt32(0) };
                });

                foreach (var disease in diseases)
                {
                    bool isEmpty = string.IsNullOrWhiteSpace(disease.name)
                                   && string.IsNullOrWhiteSpace(disease.procedures)
                                   && (disease.Medicines == null || disease.Medicines.Count == 0)
                                   && (disease.Symptoms == null || disease.Symptoms.Count == 0);

                    if (disease.id != null && isEmpty)
                    {
                        sqlConnectorPointer.Push("DELETE FROM diseases_medicines WHERE diseases_id = @diseaseId",
                            cmd => cmd.Parameters.AddWithValue("@diseaseId", disease.id.Value));

                        sqlConnectorPointer.Push("DELETE FROM diseases_symptoms WHERE disease_id = @diseaseId",
                            cmd => cmd.Parameters.AddWithValue("@diseaseId", disease.id.Value));

                        sqlConnectorPointer.Push("DELETE FROM disease WHERE id = @id",
                            cmd => cmd.Parameters.AddWithValue("@id", disease.id.Value));

                        continue; 
                    }

                    var medicinesWithId = new List<Medicine>();
                    foreach (var med in disease.Medicines)
                    {
                        if (!allMedicines.TryGetValue(med.name, out var foundMed))
                            throw new MedicineNotFoundException(med.name);

                        medicinesWithId.Add(foundMed);
                    }

                    int? diseaseId = disease.id;

                    if (diseaseId != null)
                    {
                        int count = 0;
                        sqlConnectorPointer.Read("SELECT COUNT(*) FROM disease WHERE id = @id", reader =>
                        {
                            count = reader.GetInt32(0);
                        }, cmd => cmd.Parameters.AddWithValue("@id", diseaseId.Value));

                        if (count == 0)
                            diseaseId = null;
                    }

                    if (diseaseId == null)
                    {
                        sqlConnectorPointer.Read("SELECT id FROM disease WHERE name = @name", reader =>
                        {
                            diseaseId = reader.GetInt32(0);
                        }, cmd => cmd.Parameters.AddWithValue("@name", disease.name));
                    }

                    if (diseaseId == null)
                    {
                        int newId = 0;
                        sqlConnectorPointer.Push(@"
                INSERT INTO disease (name, procedures) VALUES (@name, @procedures);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                            cmd =>
                            {
                                cmd.Parameters.AddWithValue("@name", disease.name);
                                cmd.Parameters.AddWithValue("@procedures", disease.procedures);
                                newId = (int)cmd.ExecuteScalar();
                            });
                        diseaseId = newId;
                    }
                    else
                    {
                        sqlConnectorPointer.Push("UPDATE disease SET name=@name, procedures=@procedures WHERE id=@id",
                            cmd =>
                            {
                                cmd.Parameters.AddWithValue("@name", disease.name);
                                cmd.Parameters.AddWithValue("@procedures", disease.procedures);
                                cmd.Parameters.AddWithValue("@id", diseaseId.Value);
                            });
                    }

                    sqlConnectorPointer.Push("DELETE FROM diseases_medicines WHERE diseases_id=@diseaseId",
                        cmd => cmd.Parameters.AddWithValue("@diseaseId", diseaseId.Value));
                    foreach (var med in medicinesWithId)
                    {
                        sqlConnectorPointer.Push(
                            "INSERT INTO diseases_medicines (medicines_id, diseases_id, quantity) VALUES (@medId, @diseaseId, @qty)",
                            cmd =>
                            {
                                cmd.Parameters.AddWithValue("@medId", med.id.Value);
                                cmd.Parameters.AddWithValue("@diseaseId", diseaseId.Value);
                                cmd.Parameters.AddWithValue("@qty", med.quantity);
                            });
                    }

                    var rawSymptoms = disease.Symptoms?.Select(s => s.name) ?? new List<string>();
                    var normalizedSymptomNames = NormalizeSymptomName(string.Join(",", rawSymptoms))
                                                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(s => s.Trim())
                                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                                    .ToList();

                    var symptomsToLink = new List<Symptom>();
                    foreach (var symName in normalizedSymptomNames)
                    {
                        if (!allSymptoms.TryGetValue(symName, out var symptom))
                        {
                            int newSymptomId = 0;
                            sqlConnectorPointer.Push(
                                "INSERT INTO symptoms (name) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                                cmd =>
                                {
                                    cmd.Parameters.AddWithValue("@name", symName);
                                    newSymptomId = (int)cmd.ExecuteScalar();
                                });
                            symptom = new Symptom(symName) { id = newSymptomId };
                            allSymptoms[symName] = symptom;
                        }
                        symptomsToLink.Add(symptom);
                    }

                    sqlConnectorPointer.Push("DELETE FROM diseases_symptoms WHERE disease_id=@diseaseId",
                        cmd => cmd.Parameters.AddWithValue("@diseaseId", diseaseId.Value));
                    foreach (var sym in symptomsToLink)
                    {
                        sqlConnectorPointer.Push(
                            "INSERT INTO diseases_symptoms (disease_id, symptom_id) VALUES (@diseaseId, @symptomId)",
                            cmd =>
                            {
                                cmd.Parameters.AddWithValue("@diseaseId", diseaseId.Value);
                                cmd.Parameters.AddWithValue("@symptomId", sym.id.Value);
                            });
                    }
                }
            }

            public Disease GetDiseaseByName(string diseaseName)
            {
                Disease result = null;

                string sqlDisease = "SELECT id, name, procedures FROM disease WHERE name = @name";

                sqlConnectorPointer.Read(sqlDisease, reader =>
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    string procedures = reader.IsDBNull(2) ? null : reader.GetString(2);

                    var medicines = new List<Medicine>();
                    sqlConnectorPointer.Read(
                        @"SELECT m.id, m.name, m.quantity FROM medicines m 
                          INNER JOIN diseases_medicines dm ON m.id = dm.medicines_id
                          WHERE dm.diseases_id = @diseaseId",
                        medReader =>
                        {
                            int medId = medReader.GetInt32(0);
                            string medName = medReader.GetString(1);
                            int medQuantity = medReader.GetInt32(2);

                            var interchangeableList = new List<Medicine>();
                            sqlConnectorPointer.Read(
                                @"SELECT im.interchangeable_id, im2.name FROM interchangeable_medicines im
                                  INNER JOIN medicines im2 ON im.interchangeable_id = im2.id
                                  WHERE im.medicine_id = @medicineId",
                                imReader =>
                                {
                                    var interId = imReader.GetInt32(0);
                                    var interName = imReader.GetString(1);
                                    interchangeableList.Add(new Medicine(interId, interName, 0, new List<Medicine>()));
                                },
                                cmd => cmd.Parameters.AddWithValue("@medicineId", medId)
                            );

                            medicines.Add(new Medicine(medId, medName, medQuantity, interchangeableList));
                        },
                        cmd => cmd.Parameters.AddWithValue("@diseaseId", id)
                    );

                    var symptoms = new List<Symptom>();
                    sqlConnectorPointer.Read(
                        @"SELECT s.id, s.name FROM symptoms s
                          INNER JOIN diseases_symptoms ds ON s.id = ds.symptom_id
                          WHERE ds.disease_id = @diseaseId",
                        symReader =>
                        {
                            int symId = symReader.GetInt32(0);
                            string symName = symReader.GetString(1);
                            symptoms.Add(new Symptom(symName) { id = symId });
                        },
                        cmd => cmd.Parameters.AddWithValue("@diseaseId", id)
                    );

                    result = new Disease(id, name, procedures, medicines, symptoms);
                },
                cmd => cmd.Parameters.AddWithValue("@name", diseaseName));

                return result;
            }
        }

        public class MedicineService
        {
            private SqlConnector sqlConnectorPointer;

            public MedicineService(SqlConnector sqlConnectorPointer)
            {
                this.sqlConnectorPointer = sqlConnectorPointer;
            }

            public List<Medicine> GetAllMedicines()
            {
                var medicines = new List<Medicine>();

                sqlConnectorPointer.Read(@"
                    SELECT id, name, quantity 
                    FROM medicines",
                    reader =>
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        int quantity = reader.GetInt32(2);

                        var medicine = new Medicine(name, quantity, new List<Medicine>()) { id = id };

                        medicines.Add(medicine);
                    });

                foreach (var med in medicines)
                {
                    sqlConnectorPointer.Read(@"
                        SELECT m2.id, m2.name, m2.quantity
                        FROM medicines m2
                        JOIN interchangeable_medicines im ON m2.id = im.interchangeable_id
                        WHERE im.medicine_id = @medicineId",
                        reader =>
                        {
                            var interchangeableMedicine = new Medicine(reader.GetString(1), reader.GetInt32(2), new List<Medicine>())
                            {
                                id = reader.GetInt32(0)
                            };
                            med.interchangleMedicineList.Add(interchangeableMedicine);
                        },
                        cmd =>
                        {
                            cmd.Parameters.AddWithValue("@medicineId", med.id);
                        });
                }

                return medicines;
            }

            public int InsertReturnId(string name, int quantity)
            {
                int newId = 0;

                string sql = @"
                    INSERT INTO medicines (name, quantity)
                    VALUES (@name, @quantity);
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                sqlConnectorPointer.RawPush(sql, command =>
                {
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@quantity", quantity);

                    if (command.Connection.State != ConnectionState.Open)
                        command.Connection.Open();

                    newId = Convert.ToInt32(command.ExecuteScalar());
                });

                return newId;
            }

            public Medicine GetMedicineByName(string medicineName)
            {
                Medicine result = null;

                string sqlMedicine = "SELECT id, name, quantity FROM medicines WHERE name = @name";

                sqlConnectorPointer.Read(sqlMedicine, reader =>
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    int quantity = reader.GetInt32(2);
                    
                    var interchangeableList = new List<Medicine>();
                    sqlConnectorPointer.Read(
                        @"SELECT im.interchangeable_id, m.name
                          FROM interchangeable_medicines im
                          INNER JOIN medicines m ON im.interchangeable_id = m.id
                          WHERE im.medicine_id = @medicineId",
                        imReader =>
                        {
                            int interId = imReader.GetInt32(0);
                            string interName = imReader.GetString(1);
                            interchangeableList.Add(new Medicine(interId, interName, 0, new List<Medicine>()));
                        },
                        cmd => cmd.Parameters.AddWithValue("@medicineId", id)
                    );

                    result = new Medicine(id, name, quantity, interchangeableList);
                },
                cmd => cmd.Parameters.AddWithValue("@name", medicineName));

                return result;
            }

            public void SaveAll(List<Medicine> medicines)
            {
                foreach (var med in medicines)
                {
                    if (med.id == null)
                    {
                        med.id = InsertReturnId(med.name, med.quantity);

                        sqlConnectorPointer.Push("DELETE FROM interchangeable_medicines WHERE medicine_id = @medId", cmd =>
                        {
                            cmd.Parameters.AddWithValue("@medId", med.id);
                        });

                        if(med.interchangleMedicineList != null)
                        {
                            foreach (var interMed in med.interchangleMedicineList)
                            {
                                sqlConnectorPointer.Push(@"
                                INSERT INTO interchangeable_medicines (medicine_id, interchangeable_id)
                                VALUES (@medId, @interId)", cmd =>
                                {
                                    cmd.Parameters.AddWithValue("@medId", med.id);
                                    cmd.Parameters.AddWithValue("@interId", interMed.id ?? 0);
                                });
                            }
                        }
                    }
                    else
                    {
                        string updateSql = @"
                        UPDATE medicines 
                        SET name = @name, quantity = @quantity
                        WHERE id = @id";

                        sqlConnectorPointer.Push(updateSql, cmd =>
                        {
                            cmd.Parameters.AddWithValue("@id", med.id);
                            cmd.Parameters.AddWithValue("@name", med.name);
                            cmd.Parameters.AddWithValue("@quantity", med.quantity);
                        });

                        sqlConnectorPointer.Push("DELETE FROM interchangeable_medicines WHERE medicine_id = @medId", cmd =>
                        {
                            cmd.Parameters.AddWithValue("@medId", med.id);
                        });

                        foreach (var interMed in med.interchangleMedicineList)
                        {
                            sqlConnectorPointer.Push(@"
                                INSERT INTO interchangeable_medicines (medicine_id, interchangeable_id)
                                VALUES (@medId, @interId)", cmd =>
                            {
                                cmd.Parameters.AddWithValue("@medId", med.id);
                                cmd.Parameters.AddWithValue("@interId", interMed.id ?? 0);
                            });
                        }
                    }
                }
            }
        }

        public DoctorDataBaseWrapper SetSqlConnectorPointer(SqlConnector pointer)
        {
            sqlConnectorPointer = pointer;  
            return this;
        }
    }

    public class Permission : IEquatable<Permission>
    {
        public int id;
        public string name;

        public Permission(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public override bool Equals(object obj)
        {
            return obj is Permission permission &&
                   id == permission.id;
        }

        public bool Equals(Permission other)
        {
            return !(other is null) &&
                   id == other.id;
        }

        public override int GetHashCode()
        {
            return 1877310944 + id.GetHashCode();
        }
    }

    public class User
    {
        public readonly int? id;
        public readonly Permission permission;
        public string name;
        public string password;

        public User(int? id, Permission permission, string name, string password)
        {
            this.id = id;
            this.permission = permission;
            this.name = name;
            this.password = password;
        }

        public User(Permission permission, string name, string password)
        {
            id = null;
            this.permission = permission;
            this.name = name;
            this.password = password;
        }
    }

    public class Disease
    {
        public int? id;
        public string name;
        public string procedures;
        public List<Medicine> Medicines { get; set; }
        public List<Symptom> Symptoms { get; set; }

        public Disease(string name, string procedures, List<Medicine> medicines, List<Symptom> symptoms)
        {
            id = null;

            this.name = name;
            this.procedures = procedures;
            Medicines = medicines;
            Symptoms = symptoms;
        }

        public Disease(int? id, string name, string procedures, List<Medicine> medicines, List<Symptom> symptoms)
        {
            this.id = id;
            this.name = name;
            this.procedures = procedures;
            Medicines = medicines;
            Symptoms = symptoms;
        }

        public Disease(int? id, string name, string procedures)
        {
            this.id = id;
            this.name = name;
            this.procedures = procedures;
        }

        public Disease(int? id, string name, string procedures, string symptoms, string medicines)
        {
            this.id = id;
            this.name = name;
            this.procedures = procedures;
            this.Symptoms = symptoms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => new Symptom(s.Trim()))
                         .ToList();
            this.Medicines = medicines.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
             .Select(s => new Medicine(s.Trim()))
             .ToList();
        }
    }

    public class Symptom
    {
        public int? id;
        public string name;

        public Symptom(string name)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }

    public class Medicine
    {
        public int? id;
        public string name;
        public int quantity;
        public List<Medicine> interchangleMedicineList { get; set; }

        public Medicine(string name, int quantity, List<Medicine> interchangleMedicineList)
        {
            this.name = name;
            this.quantity = quantity;
            this.interchangleMedicineList = interchangleMedicineList;
        }

        public Medicine(int? id, string name, int quantity, List<Medicine> interchangleMedicineList)
        {
            this.id = id;
            this.name = name;
            this.quantity = quantity;
            this.interchangleMedicineList = interchangleMedicineList;
        }

        public Medicine(string name)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}