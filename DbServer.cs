using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace H24.Modules
{
    internal class DbServer
    {
        private static string ConnectionString = Debugger.IsAttached ? 
            File.ReadAllText(@"C:\Users\Gustave\Desktop\Projects\PuzzleSolveR\PSR\connectionString.txt") :
            File.ReadAllText("connectionString.txt");

        public static void PerformQuery(Action<NpgsqlConnection> queryAction, Action<Exception> errorMessageHandler)
        {
            using (NpgsqlConnection sqlConnection = new NpgsqlConnection(DbServer.ConnectionString))
            {
                sqlConnection.Open();

                try
                {
                    queryAction(sqlConnection);
                }
                catch (Exception ex)
                {
                    errorMessageHandler(ex);
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        /// <summary>
        /// Runs a read command, returning results built up iteratively from the <paramref name="rowReader"/>
        /// </summary>
        public static List<T> ExecuteRead<T>(string command, NpgsqlConnection connection, Func<NpgsqlDataReader, T> rowReader, IEnumerable<NpgsqlParameter> parameters = null)
        {
            parameters = parameters ?? Array.Empty<NpgsqlParameter>();

            List<T> results = new List<T>();
            using (NpgsqlCommand sqlCommand = new NpgsqlCommand(command, connection))
            {
                foreach (NpgsqlParameter parameter in parameters)
                {
                    sqlCommand.Parameters.Add(parameter);
                }

                using (NpgsqlDataReader dataReader = sqlCommand.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        T result = rowReader(dataReader);
                        results.Add(result);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Runs a read command, returning no results.
        /// </summary>
        public static void ExecuteRead(string command, NpgsqlConnection connection, Action<NpgsqlDataReader> rowReader, IEnumerable<NpgsqlParameter> parameters = null)
        {
            parameters = parameters ?? Array.Empty<NpgsqlParameter>();

            using (NpgsqlCommand sqlCommand = new NpgsqlCommand(command, connection))
            {
                foreach (NpgsqlParameter parameter in parameters)
                {
                    sqlCommand.Parameters.Add(parameter);
                }

                using (NpgsqlDataReader dataReader = sqlCommand.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        rowReader(dataReader);
                    }
                }
            }
        }
    }
}