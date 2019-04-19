using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM thesaurus WHERE UPPER(word) LIKE :queryToExecute", connection);
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

        private List<string> GetThesaurusResults(string search, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand("SELECT word,synonym_ids FROM thesaurus WHERE UPPER(word) LIKE :queryToExecute ORDER BY word LIMIT 10", connection);
            command.Parameters.Add(new NpgsqlParameter("queryToExecute", search));

            Dictionary<int, HashSet<string>> synonymIds = new Dictionary<int, HashSet<string>>();
            using (NpgsqlDataReader dataReader = command.ExecuteReader())
            {
                while (dataReader.Read())
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
                }
            }

            List<string> results = new List<string>();
            if (synonymIds.Any())
            {
                command = new NpgsqlCommand($"SELECT id,synonymlist FROM thesaurus_lookup WHERE ID IN ({string.Join(',', synonymIds.Keys)}) LIMIT 1000", connection);
                using (NpgsqlDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        int id = (int)dataReader[0];
                        string synonyms = (string)dataReader[1];
                        string wordsList = string.Join(", ", synonymIds[id]);

                        results.Add($"{wordsList} => {synonyms}");
                    }
                }
            }

            return results;
        }

        private int GetResultCount(string search, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM homophones WHERE UPPER(homophones) LIKE :queryToExecute", connection);
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

        // TODO: homophones and synonyms should wrap in the scrolling view.
        private List<string> GetResults(string search, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand("SELECT homophones FROM homophones WHERE UPPER(homophones) LIKE :queryToExecute ORDER BY homophones LIMIT 50", connection);
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