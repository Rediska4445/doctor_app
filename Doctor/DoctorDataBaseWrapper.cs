using SqlExplorer;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Doctor
{
    public class DoctorDataBaseWrapper
    {
        private SqlConnector sqlConnectorPointer;

        public UserService userServiceObj;
        public PermissionService permissionServiceObj;

        public DiseaseService diseaseServiceObj;

        public DoctorDataBaseWrapper(SqlConnector sqlConnectorPointer)
        {
            this.sqlConnectorPointer = sqlConnectorPointer;

            userServiceObj = new UserService(this.sqlConnectorPointer);
            permissionServiceObj = new PermissionService(this.sqlConnectorPointer);
            diseaseServiceObj = new DiseaseService(this.sqlConnectorPointer);
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

        public class DiseaseService
        {
            private SqlConnector sqlConnectorPointer;

            public DiseaseService(SqlConnector sqlConnectorPointer)
            {
                this.sqlConnectorPointer = sqlConnectorPointer;
            }

            public Disease GetDiseaseByName(string name)
            {
                Disease disease = null;

                // 1. Основная информация о болезни
                sqlConnectorPointer.Read("SELECT id, name, procedures FROM disease WHERE name = @name", reader =>
                {
                    int id = reader.GetInt32(0);
                    string diseaseName = reader.GetString(1);
                    string procedures = reader.GetString(2);
                    disease = new Disease(diseaseName, procedures, new List<Medicine>(), new List<Symptom>()) { id = id };
                }, cmd =>
                {
                    cmd.Parameters.AddWithValue("@name", name);
                });

                if (disease == null)
                    return null;

                int diseaseId = disease.id ?? 0;

                // 2. Симптомы
                sqlConnectorPointer.Read(@"
                    SELECT s.id, s.name 
                    FROM symptoms s
                    JOIN diseases_symptoms ds ON s.id = ds.symptom_id
                    WHERE ds.disease_id = @diseaseId", reader =>
                {
                    disease.Symptoms.Add(new Symptom(reader.GetString(1)) { id = reader.GetInt32(0) });
                }, cmd =>
                {
                    cmd.Parameters.AddWithValue("@diseaseId", diseaseId);
                });

                // 3. Лекарства с привязкой к болезни и quantity из diseases_medicines
                var medicines = new List<Medicine>();
                sqlConnectorPointer.Read(@"
                    SELECT m.id, m.name, dm.quantity 
                    FROM medicines m
                    JOIN diseases_medicines dm ON m.id = dm.medicines_id
                    WHERE dm.diseases_id = @diseaseId", reader =>
                {
                    medicines.Add(new Medicine(reader.GetString(1), reader.GetInt32(2), new List<Medicine>()) { id = reader.GetInt32(0) });
                }, cmd =>
                {
                    cmd.Parameters.AddWithValue("@diseaseId", diseaseId);
                });

                // 4. Взаимозаменяемые лекарства для каждого medicine
                foreach (var med in medicines)
                {
                    sqlConnectorPointer.Read(@"
                        SELECT m2.id, m2.name, m2.quantity
                        FROM medicines m2
                        JOIN interchangeable_medicines im ON m2.id = im.interchangeable_id
                        WHERE im.medicine_id = @medicineId", reader =>
                    {
                        med.interchangleMedicineList.Add(new Medicine(reader.GetString(1), reader.GetInt32(2), new List<Medicine>()) { id = reader.GetInt32(0) });
                    }, cmd =>
                    {
                        cmd.Parameters.AddWithValue("@medicineId", med.id);
                    });
                }

                disease.Medicines = medicines;
                return disease;
            }
        }

        public DoctorDataBaseWrapper SetSqlConnectorPointer(SqlConnector pointer)
        {
            sqlConnectorPointer = pointer;  
            return this;
        }
    }

    public class Permission
    {
        public int id;
        public string name;

        public Permission(int id, string name)
        {
            this.id = id;
            this.name = name;
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
    }

    public class Symptom
    {
        public int? id;
        public string name;

        public Symptom(string name)
        {
            this.name = name;
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
    }
}