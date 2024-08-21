using System;

namespace MediaBrowser.Model.Search
{
    public class SearchHint
    {
        // Existing properties...

        /// <summary>
        /// Calculates the Levenshtein distance between the item name and the search query.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <returns>The Levenshtein distance.</returns>
        public int CalculateLevenshteinDistance(string query)
        {
            string name = this.Name;
            int[,] d = new int[name.Length + 1, query.Length + 1];

            for (int i = 0; i <= name.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= query.Length; j++) d[0, j] = j;

            for (int i = 1; i <= name.Length; i++)
            {
                for (int j = 1; j <= query.Length; j++)
                {
                    int cost = (query[j - 1] == name[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[name.Length, query.Length];
        }
    }
}
