using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Linq;

namespace H24.Modules
{
    /// <summary>
    /// Module for word search results
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Produces("application/json")]
    public class WordExtraController : ControllerBase
    {
        [AcceptVerbs("GET")]
        public IActionResult FindHomophones([FromQuery]string search)
        {
            int resultCount = 0;
            List<string> searchResults = null;
            string errorMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToUpperInvariant();
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

        [AcceptVerbs("GET")]
        public IActionResult FindSynonyms([FromQuery]string search)
        {
            int resultCount = 0;
            List<string> searchResults = null;
            string errorMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToUpperInvariant();
                DbServer.PerformQuery((connection) =>
                    {
                        resultCount = this.GetThesaurusResultCount(search, connection);
                        searchResults = this.GetThesaurusResults(search, connection);
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

        private int GetThesaurusResultCount(string search, NpgsqlConnection connection)
        {
            return DbServer.ExecuteRead(
                "SELECT COUNT(*) FROM thesaurus WHERE UPPER(word) LIKE :queryToExecute",
                connection,
                (dataReader) => (int)(long)dataReader[0],
                new[] { new NpgsqlParameter("queryToExecute", search) }).First();
        }

        private List<string> GetThesaurusResults(string search, NpgsqlConnection connection)
        {
            Dictionary<int, HashSet<string>> synonymIds = new Dictionary<int, HashSet<string>>();

            DbServer.ExecuteRead(
                "SELECT word,synonym_ids FROM thesaurus WHERE UPPER(word) LIKE :queryToExecute ORDER BY word LIMIT 10",
                connection,
                (dataReader) =>
                {
                    string word = (string)dataReader[0];
                    List<int> wordSynonymIds = ((string)dataReader[1]).Split(',').Select(item => int.TryParse(item, out int num) ? num : -1)
                        .Where(num => num != -1).ToList();
                    foreach (int id in wordSynonymIds)
                    {
                        if (!synonymIds.ContainsKey(id))
                        {
                            synonymIds.Add(id, new HashSet<string>());
                        }

                        synonymIds[id].Add(word);
                    }
                },
                new[] { new NpgsqlParameter("queryToExecute", search) });

            List<string> results = new List<string>();
            if (synonymIds.Any())
            {
                results = DbServer.ExecuteRead(
                    $"SELECT id,synonymlist FROM thesaurus_lookup WHERE ID IN ({string.Join(',', synonymIds.Keys)}) LIMIT 1000",
                    connection,
                    (dataReader) =>
                    {
                        int id = (int)dataReader[0];
                        string synonyms = (string)dataReader[1];
                        string wordsList = string.Join(", ", synonymIds[id]);

                        return $"{wordsList} => {synonyms}";
                    });
            }

            return results;
        }



        private int GetResultCount(string search, NpgsqlConnection connection)
        {
            return DbServer.ExecuteRead(
                "SELECT COUNT(*) FROM homophones WHERE UPPER(homophones) LIKE :queryToExecute",
                connection,
                (dataReader) => (int)(long)dataReader[0],
                new[] { new NpgsqlParameter("queryToExecute", search) }).First();
        }

        private List<string> GetResults(string search, NpgsqlConnection connection)
        {
            return DbServer.ExecuteRead(
               "SELECT homophones FROM homophones WHERE UPPER(homophones) LIKE :queryToExecute ORDER BY homophones LIMIT 50",
               connection,
               (dataReader) => (string)dataReader[0],
               new[] { new NpgsqlParameter("queryToExecute", search) });
        }
    }
}