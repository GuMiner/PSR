using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Linq;

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
                        clueSearchResults = this.ExecuteClueAnswerQuery("clue", search, connection);
                        answerSearchResults = this.ExecuteClueAnswerQuery("answer", search, connection);
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
            return DbServer.ExecuteRead(
                "SELECT COUNT(*) FROM crosswords WHERE clue LIKE :queryToExecute OR answer LIKE :queryToExecute2",
                connection,
                (dataReader) => (int)(long)dataReader[0],
                new[]
                {
                    new NpgsqlParameter("queryToExecute", search),
                    new NpgsqlParameter("queryToExecute2", search),
                }).First();
        }

        private List<string> ExecuteClueAnswerQuery(string filterColumn, string search, NpgsqlConnection connection)
        {
            return DbServer.ExecuteRead(
                $"SELECT clue, answer FROM crosswords WHERE {filterColumn} LIKE :queryToExecute ORDER BY clue LIMIT 200",
                connection,
                (dataReader) => $"{(string)dataReader[0]} => {(string)dataReader[1]}",
                new[] { new NpgsqlParameter("queryToExecute", search) });
        }
    }
}