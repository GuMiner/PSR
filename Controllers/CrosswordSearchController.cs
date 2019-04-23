using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;

namespace H24.Modules
{
    /// <summary>
    /// Module for retrieving crossword results
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Produces("application/json")]
    public class CrosswordSearchController : ControllerBase
    {
        [AcceptVerbs("GET")]
        public IActionResult FindMatchingWords([FromQuery]string search)
        {
            int resultCount = 0;
            List<string> clueSearchResults = new List<string>();
            List<string> answerSearchResults = new List<string>();
            string errorMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToUpperInvariant();
                DbServer.PerformQuery((connection) =>
                    {
                        resultCount = this.GetResultCount(search, connection);
                        clueSearchResults = this.GetClueResults(search, connection);
                        answerSearchResults = this.GetAnswerResults(search, connection);
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
                clueSearchResults = new List<string>();
            }

            return this.Ok(new
            {
                count = resultCount,
                clueResults = clueSearchResults,
                answerResults = answerSearchResults,
                errorMessage = errorMessage
            });
        }

        private int GetResultCount(string search, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM crosswords WHERE clue LIKE :queryToExecute OR answer LIKE :queryToExecute2", connection);
            command.Parameters.Add(new NpgsqlParameter("queryToExecute", search));
            command.Parameters.Add(new NpgsqlParameter("queryToExecute2", search));

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

        // TODO: words should wrap in the scrolling view
        private List<string> GetClueResults(string search, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand("SELECT clue, answer FROM crosswords WHERE clue LIKE :queryToExecute ORDER BY clue LIMIT 200", connection);
            command.Parameters.Add(new NpgsqlParameter("queryToExecute", search));

            List<string> words = new List<string>();
            using (NpgsqlDataReader dataReader = command.ExecuteReader())
            {
                while (dataReader.Read())
                {
                    words.Add($"{(string)dataReader[0]} => {(string)dataReader[1]}");
                }
            }

            return words;
        }

        private List<string> GetAnswerResults(string search, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand("SELECT clue, answer FROM crosswords WHERE answer LIKE :queryToExecute ORDER BY clue LIMIT 200", connection);
            command.Parameters.Add(new NpgsqlParameter("queryToExecute", search));

            List<string> words = new List<string>();
            using (NpgsqlDataReader dataReader = command.ExecuteReader())
            {
                while (dataReader.Read())
                {
                    words.Add($"{(string)dataReader[0]} => {(string)dataReader[1]}");
                }
            }

            return words;
        }
    }
}