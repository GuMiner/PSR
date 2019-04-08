using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;

namespace H24.Modules
{
    /// <summary>
    /// Module for retrieving the status of various server resources.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Produces("application/json")]
    public class WordSearchController : ControllerBase
    {
        [AcceptVerbs("GET")]
        public IActionResult FindMatchingWords([FromQuery]string search)
        {
            int resultCount = 0;
            List<string> searchResults = null;
            string errorMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                DbServer.PerformQuery((connection) =>
                    {
                        resultCount = this.GetResultCount(search, connection);
                        searchResults = this.GetResults(search, connection);
                    },
                    (ex) =>
                    {
                        resultCount = -1;
                        errorMessage = ex.ToString();
                    });
            }
            else
            {
                resultCount = 0;
                searchResults = new List<string>();
            }

            return this.Ok(new
            {
                count = resultCount,
                results = searchResults,
                errorMessage = errorMessage
            });
        }

        private int GetResultCount(string search, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM words WHERE word LIKE :queryToExecute", connection);
            command.Parameters.Add(new NpgsqlParameter("queryToExecute", search));

            using (NpgsqlDataReader dataReader = command.ExecuteReader())
            {
                int count = 0;
                while (dataReader.Read())
                {
                    count = (int)(long)dataReader[0];
                }

                return count;
            }
        }

        // TODO: Words need to be ordered, app.config needs to list when throttling happens, words should wrap in the scrolling view.
        private List<string> GetResults(string search, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand("SELECT word FROM words WHERE word LIKE :queryToExecute LIMIT 200", connection);
            command.Parameters.Add(new NpgsqlParameter("queryToExecute", search));

            List<string> words = new List<string>();
            using (NpgsqlDataReader dataReader = command.ExecuteReader())
            {
                while (dataReader.Read())
                {
                    words.Add((string)dataReader[0]);
                }
            }

            return words;
        }
    }
}