using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace H24.Modules
{
    /// <summary>
    /// Module for word search results
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
        public IActionResult FindAnagrams([FromQuery]string search)
        {
            int resultCount = 0;
            List<string> searchResults = null;
            string errorMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToUpperInvariant();
                DbServer.PerformQuery((connection) =>
                {
                    resultCount = this.GetAnagramResultCount(search, connection);
                    searchResults = this.GetAnagramResults(search, connection);
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

        // TODO: words should wrap in the scrolling view.
        private List<string> GetResults(string search, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand("SELECT word FROM words WHERE word LIKE :queryToExecute ORDER BY word LIMIT 200", connection);
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

        private int GetAnagramResultCount(string search, NpgsqlConnection connection)
        {
            string anagramSearch = this.GetAnagramSearchQuery(search);
            NpgsqlCommand command = new NpgsqlCommand($"SELECT COUNT(*) FROM words WHERE {anagramSearch}", connection);

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

        private List<string> GetAnagramResults(string search, NpgsqlConnection connection)
        {
            string anagramSearch = this.GetAnagramSearchQuery(search);
            NpgsqlCommand command = new NpgsqlCommand($"SELECT word FROM words WHERE {anagramSearch} ORDER BY word LIMIT 200", connection);
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

        private string GetAnagramSearchQuery(string search)
        {
            Dictionary<char, int> characters = new Dictionary<char, int>();
            foreach (char character in search)
            {
                if (!characters.ContainsKey(character))
                {
                    characters.Add(character, 0);
                }

                characters[character]++;
            }

            string comp = "=";
            if (characters.ContainsKey('_'))
            {
                // Greater than or equals, to support anagrams with unknown characters.
                comp = ">=";
            }

            StringBuilder searchQuery = new StringBuilder();
            searchQuery.Append($"(LENGTH(word) = {search.Length})");
            foreach (KeyValuePair<char, int> keyValuePair in characters)
            {
                if (keyValuePair.Key != '_')
                {
                    searchQuery.Append($" AND (LENGTH(word) - LENGTH(REPLACE(word, '{keyValuePair.Key}', '')) {comp} {keyValuePair.Value})");
                }
            }

            return searchQuery.ToString();
        }
    }
}