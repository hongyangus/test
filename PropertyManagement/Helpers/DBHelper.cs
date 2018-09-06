using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PropertyManagement.Helpers
{
    public static class DBHelper<T> where T : class
    {
        public static int ExecuteMySQL(string query, object para = null)
        {
            int result = -1;
            using (MySqlConnection connection = new MySqlConnection(Helpers.GetERPConnectionString()))
            {
                try
                {
                    result = connection.Execute(query, para);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
            return result;
        }

        public static List<T> QueryMySQL(string query, object para = null) 
        {
            List<T> result = new List<T>();
            using (MySqlConnection connection = new MySqlConnection(Helpers.GetERPConnectionString()))
            {
                try
                {
                    result = connection.Query<T>(query, para).ToList<T>();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
            return result;
        }

        public static List<T> QueryObjectMySQL(string tableName, object para = null)
        {
            List<T> result = new List<T>();
            string sqlSelectFromTable = "SELECT * FROM " + tableName;
            using (MySqlConnection connection = new MySqlConnection(Helpers.GetERPConnectionString()))
            {
                try
                {
                    result = connection.Query<T>(sqlSelectFromTable, para).ToList<T>();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
            return result;
        }
    }
}