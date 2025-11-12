using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace SqlExplorer
{
    /// <author>Автор: https://github.com/Rediska4445</author>
    /// <summary>
    /// Представляет обёртку для подключения к SQL Server и выполнения основных операций с базой данных.
    /// Предоставляет методы открытия/закрытия соединения, выполнения запросов и обработки событий ключевых операций.
    /// </summary>
    public class SqlConnector
    {
        /// <summary>
        /// Строка подключения к базе данных.
        /// По умолчанию использует локальную базу данных SQL Server (localdb).
        /// </summary>
        public string connectionString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;";

        /// <summary>
        /// Объект SqlConnection, управляющий состоянием соединения с базой данных.
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// Делегат, вызываемый при успешном открытии соединения.
        /// </summary>
        public Action<SqlConnection> onOpen;

        /// <summary>
        /// Делегат, вызываемый при закрытии соединения.
        /// </summary>
        public Action<SqlConnection> onClose;

        /// <summary>
        /// Делегат, вызываемый после успешной операции вставки или обновления.
        /// </summary>
        public Action<SqlConnection> onPush;

        /// <summary>
        /// Делегат, вызываемый после выполнения операции чтения данных.
        /// </summary>
        public Action<SqlConnection> onRead;

        /// <summary>
        /// Инициализирует новый экземпляр класса, создавая подключение с использованием строки по умолчанию.
        /// </summary>
        public SqlConnector()
        {
            connection = new SqlConnection(connectionString);
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса с предоставленной строкой подключения.
        /// </summary>
        /// <param name="conn"> строка подключения к базе данных </param>
        public SqlConnector(string conn)
        {
            connection = new SqlConnection(connectionString = conn);
        }

        /// <summary>
        /// Выполняет SQL-запрос и возвращает заполненную DataTable.
        /// Используется для получения данных.
        /// </summary>
        /// <param name="sql">SQL-запрос в виде строки</param>
        /// <returns>таблица с результатами запроса</returns>
        public DataTable Fill(string sql)
        {
            DataTable table = new DataTable();

            // Открываем соединение, если оно закрыто
            if (connection.State != ConnectionState.Open)
                Open();

            if (connection.State == ConnectionState.Open)
            {
                SqlDataAdapter adapter = new SqlDataAdapter(sql, connection);
                adapter.Fill(table);
            }

            return table;
        }

        /// <summary>
        /// Открывает соединение с базой данных.
        /// В случае успеха вызывает событие onOpen.
        /// </summary>
        public void Open()
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();

            onOpen?.Invoke(connection);
        }

        /// <summary>
        /// Закрывает соединение.
        /// В случае успеха вызывает событие onClose.
        /// </summary>
        public void Close()
        {
            if (connection.State != ConnectionState.Closed)
                connection.Close();

            onClose?.Invoke(connection);
        }

        public void Read(string sql, Action<SqlDataReader> processRow, Action<SqlCommand> configureCommand)
        {
            if (connection.State != ConnectionState.Open)
                Open();

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                configureCommand?.Invoke(command);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        processRow(reader);
                    }
                }
            }

            onRead?.Invoke(connection);
        }

        /// <summary>
        /// Выполняет оператор SELECT и вызывает переданную лямбду для обработки каждой строки.
        /// </summary>
        /// <param name="sql">SQL-запрос для чтения</param>
        /// <param name="processRow">Лямбда, которая обрабатывает каждую строку</param>
        public void Read(string sql, Action<SqlDataReader> processRow)
        {
            Read(sql, processRow, null);
        }

        /// <summary>
        /// Выполняет оператор INSERT/UPDATE/DELETE.
        /// Перед выполнением вызывается переданный делегат для настройки команды.
        /// </summary>
        /// <param name="sql">SQL-команда</param>
        /// <param name="configureCommand">Лямбда для настройки SqlCommand (например, добавление параметров)</param>
        public void Push(string sql, Action<SqlCommand> configureCommand)
        {
            if (connection.State != ConnectionState.Open)
                Open();

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                if(configureCommand != null) 
                    configureCommand?.Invoke(command);

                command.ExecuteNonQuery();
            }

            onPush?.Invoke(connection);
        }

        /// <summary>
        /// Выполняет оператор INSERT/UPDATE/DELETE.
        /// Перед выполнением вызывается переданный делегат для настройки команды.
        /// </summary>
        /// <param name="sql">SQL-команда</param>
        /// <param name="configureCommand">Лямбда для настройки SqlCommand (например, добавление параметров)</param>
        public void Push(string sql)
        {
            Push(sql, null);
        }

        /// <summary>
        /// Возвращает текущий экземпляр SqlConnection, используемый для запросов.
        /// </summary>
        /// <returns>SqlConnection</returns>
        public SqlConnection GetConnection()
        {
            return connection;
        }
    }
}