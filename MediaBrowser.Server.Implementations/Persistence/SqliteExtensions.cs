using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.Persistence
{
    /// <summary>
    /// Class SQLiteExtensions
    /// </summary>
    public static class SqliteExtensions
    {
        /// <summary>
        /// Connects to db.
        /// </summary>
        /// <param name="dbPath">The db path.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Task{IDbConnection}.</returns>
        /// <exception cref="System.ArgumentNullException">dbPath</exception>
        public static async Task<IDbConnection> ConnectToDb(string dbPath, ILogger logger)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentNullException("dbPath");
            }

            logger.Info("Sqlite {0} opening {1}", SQLiteConnection.SQLiteVersion, dbPath);

            var connectionstr = new SQLiteConnectionStringBuilder
            {
                PageSize = 4096,
                CacheSize = 2000,
                SyncMode = SynchronizationModes.Normal,
                DataSource = dbPath,
                JournalMode = SQLiteJournalModeEnum.Wal
            };

            var connection = new SQLiteConnection(connectionstr.ConnectionString);

            await connection.OpenAsync().ConfigureAwait(false);

            return connection;
        }

        public static void BindGetSimilarityScore(IDbConnection connection, ILogger logger)
        {
            var sqlConnection = (SQLiteConnection) connection;
            SimiliarToFunction.Logger = logger;
            sqlConnection.BindFunction(new SimiliarToFunction());
        }

        public static void BindFunction(this SQLiteConnection connection, SQLiteFunction function)
        {
            var attributes = function.GetType().GetCustomAttributes(typeof(SQLiteFunctionAttribute), true).Cast<SQLiteFunctionAttribute>().ToArray();
            if (attributes.Length == 0)
            {
                throw new InvalidOperationException("SQLiteFunction doesn't have SQLiteFunctionAttribute");
            }
            connection.BindFunction(attributes[0], function);
        }
    }

    [SQLiteFunction(Name = "GetSimilarityScore", Arguments = 12, FuncType = FunctionType.Scalar)]
    public class SimiliarToFunction : SQLiteFunction
    {
        internal static ILogger Logger;

        public override object Invoke(object[] args)
        {
            var score = 0;

            var inputOfficialRating = args[0] as string;
            var rowOfficialRating = args[1] as string;
            if (!string.IsNullOrWhiteSpace(inputOfficialRating) && string.Equals(inputOfficialRating, rowOfficialRating))
            {
                score += 10;
            }

            long? inputYear = args[2] == null ? (long?)null : (long)args[2];
            long? rowYear = args[3] == null ? (long?)null : (long)args[3];

            if (inputYear.HasValue && rowYear.HasValue)
            {
                var diff = Math.Abs(inputYear.Value - rowYear.Value);

                // Add if they came out within the same decade
                if (diff < 10)
                {
                    score += 2;
                }

                // And more if within five years
                if (diff < 5)
                {
                    score += 2;
                }
            }

            // genres
            score += GetListScore(args, 4, 5);

            // tags
            score += GetListScore(args, 6, 7);

            // keywords
            score += GetListScore(args, 8, 9);

            // studios
            score += GetListScore(args, 10, 11, 3);


            // TODO: People
    //        var item2PeopleNames = allPeople.Where(i => i.ItemId == item2.Id)
    //.Select(i => i.Name)
    //.Where(i => !string.IsNullOrWhiteSpace(i))
    //.DistinctNames()
    //.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

    //        points += item1People.Where(i => item2PeopleNames.ContainsKey(i.Name)).Sum(i =>
    //        {
    //            if (string.Equals(i.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Director, StringComparison.OrdinalIgnoreCase))
    //            {
    //                return 5;
    //            }
    //            if (string.Equals(i.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
    //            {
    //                return 3;
    //            }
    //            if (string.Equals(i.Type, PersonType.Composer, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Composer, StringComparison.OrdinalIgnoreCase))
    //            {
    //                return 3;
    //            }
    //            if (string.Equals(i.Type, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase))
    //            {
    //                return 3;
    //            }
    //            if (string.Equals(i.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
    //            {
    //                return 2;
    //            }

    //            return 1;
    //        });

    //        return points;

            //Logger.Debug("Returning score {0}", score);
            return score;
        }

        private int GetListScore(object[] args, int index1, int index2, int value = 10)
        {
            var score = 0;

            var inputGenres = args[index1] as string;
            var rowGenres = args[index2] as string;
            var inputGenreList = string.IsNullOrWhiteSpace(inputGenres) ? new string[] { } : inputGenres.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var rowGenresList = string.IsNullOrWhiteSpace(rowGenres) ? new string[] { } : rowGenres.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var genre in inputGenreList)
            {
                if (rowGenresList.Contains(genre, StringComparer.OrdinalIgnoreCase))
                {
                    score += value;
                }
            }

            return score;
        }
    }
}
