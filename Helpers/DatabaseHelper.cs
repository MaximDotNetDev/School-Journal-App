using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using SchoolJournalApp.Services;
using SchoolJournalApp.Views;


namespace SchoolJournalApp
{
    public static class DatabaseHelper
    {
        // Метод 1: Отримати таблицю даних (для DataGrid або ComboBox)
        // Ти просто даєш йому SQL-запит, а він повертає заповнену таблицю.
        public static DataTable GetDataTable(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        // Метод 2: Виконати команду (INSERT, UPDATE, DELETE)
        // Повертає кількість змінених рядків (або ID, якщо треба)
        public static void ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection con = new SqlConnection(AppSession.ConnectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}