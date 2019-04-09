using Npgsql;
using System;
using System.Diagnostics;
using System.IO;

namespace H24.Modules
{
    internal class DbServer
    {
        // This is a localhost connection string, so no point in trying to use it remotely.
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
    }
}