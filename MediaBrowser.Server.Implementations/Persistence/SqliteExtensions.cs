using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
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
            var sqlConnection = (SQLiteConnection)connection;
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

    [SQLiteFunction(Name = "GetSimilarityScore", Arguments = 6, FuncType = FunctionType.Scalar)]
    public class SimiliarToFunction : SQLiteFunction
    {
        internal static ILogger Logger;

        private readonly Dictionary<string, int> _personTypeScores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { PersonType.Actor, 3},
            { PersonType.Director, 5},
            { PersonType.Composer, 2},
            { PersonType.GuestStar, 3},
            { PersonType.Writer, 2},
            { PersonType.Conductor, 2},
            { PersonType.Producer, 2},
            { PersonType.Lyricist, 2}
        };

        public override object Invoke(object[] args)
        {
            var score = 0;

            // Official rating equals
            if ((long)args[0] == 1)
            {
                score += 10;
            }

            // Year difference
            long? yearDifference = args[1] == null ? (long?)null : (long)args[1];
            if (yearDifference.HasValue)
            {
                var diff = Math.Abs(yearDifference.Value);

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
            score += Convert.ToInt32((long)args[2]) * 10;

            // tags
            score += Convert.ToInt32((long)args[3]) * 10;

            // # of common keywords
            score += Convert.ToInt32((long)args[4]) *10;

            // # of common studios
            score += Convert.ToInt32((long)args[5]) * 3;

            // studios
            //score += GetListScore(args, 7, 8, 3);

            //var rowPeopleNamesText = (args[12] as string) ?? string.Empty;
            //var rowPeopleNames = rowPeopleNamesText.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            //foreach (var name in rowPeopleNames)
            //{
            //    // TODO: Send along person types
            //    score += 3;
            //}

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
