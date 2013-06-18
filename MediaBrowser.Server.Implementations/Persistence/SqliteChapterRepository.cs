using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class SqliteChapterRepository
    {
        private SQLiteConnection _connection;

        private readonly ILogger _logger;

        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IApplicationPaths _appPaths;

        private SQLiteCommand _deleteChaptersCommand;
        private SQLiteCommand _saveChapterCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteItemRepository"/> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">
        /// appPaths
        /// or
        /// jsonSerializer
        /// </exception>
        public SqliteChapterRepository(IApplicationPaths appPaths, ILogManager logManager)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }

            _appPaths = appPaths;

            _logger = logManager.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "chapters.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists chapters (ItemId GUID, ChapterIndex INT, StartPositionTicks BIGINT, Name TEXT, ImagePath TEXT, PRIMARY KEY (ItemId, ChapterIndex))",
                                "create index if not exists idx_chapters on chapters(ItemId, ChapterIndex)",

                                //pragmas
                                "pragma temp_store = memory"
                               };

            _connection.RunQueries(queries, _logger);

            PrepareStatements();
        }

        /// <summary>
        /// The _write lock
        /// </summary>
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Prepares the statements.
        /// </summary>
        private void PrepareStatements()
        {
            _deleteChaptersCommand = new SQLiteCommand
            {
                CommandText = "delete from chapters where ItemId=@ItemId"
            };

            _deleteChaptersCommand.Parameters.Add(new SQLiteParameter("@ItemId"));

            _saveChapterCommand = new SQLiteCommand
            {
                CommandText = "replace into chapters (ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath) values (@ItemId, @ChapterIndex, @StartPositionTicks, @Name, @ImagePath)"
            };

            _saveChapterCommand.Parameters.Add(new SQLiteParameter("@ItemId"));
            _saveChapterCommand.Parameters.Add(new SQLiteParameter("@ChapterIndex"));
            _saveChapterCommand.Parameters.Add(new SQLiteParameter("@StartPositionTicks"));
            _saveChapterCommand.Parameters.Add(new SQLiteParameter("@Name"));
            _saveChapterCommand.Parameters.Add(new SQLiteParameter("@ImagePath"));
        }

        /// <summary>
        /// Gets chapters for an item
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>IEnumerable{ChapterInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public IEnumerable<ChapterInfo> GetChapters(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select StartPositionTicks,Name,ImagePath from Chapters where ItemId = @ItemId order by ChapterIndex asc";

                cmd.Parameters.Add("@ItemId", DbType.Guid).Value = id;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        var chapter = new ChapterInfo
                        {
                            StartPositionTicks = reader.GetInt64(0)
                        };

                        if (!reader.IsDBNull(1))
                        {
                            chapter.Name = reader.GetString(1);
                        }

                        if (!reader.IsDBNull(2))
                        {
                            chapter.ImagePath = reader.GetString(2);
                        }

                        yield return chapter;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a single chapter for an item
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="index">The index.</param>
        /// <returns>ChapterInfo.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public ChapterInfo GetChapter(Guid id, int index)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select StartPositionTicks,Name,ImagePath from Chapters where ItemId = @ItemId and ChapterIndex=@ChapterIndex";

                cmd.Parameters.Add("@ItemId", DbType.Guid).Value = id;
                cmd.Parameters.Add("@ChapterIndex", DbType.Int32).Value = index;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        return new ChapterInfo
                        {
                            StartPositionTicks = reader.GetInt64(0),
                            Name = reader.GetString(1),
                            ImagePath = reader.GetString(2)
                        };
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Saves the chapters.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="chapters">The chapters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// id
        /// or
        /// chapters
        /// or
        /// cancellationToken
        /// </exception>
        public async Task SaveChapters(Guid id, IEnumerable<ChapterInfo> chapters, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            if (chapters == null)
            {
                throw new ArgumentNullException("chapters");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            SQLiteTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                // First delete chapters
                _deleteChaptersCommand.Parameters[0].Value = id;
                _deleteChaptersCommand.Transaction = transaction;
                await _deleteChaptersCommand.ExecuteNonQueryAsync(cancellationToken);

                var index = 0;

                foreach (var chapter in chapters)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _saveChapterCommand.Parameters[0].Value = id;
                    _saveChapterCommand.Parameters[1].Value = index;
                    _saveChapterCommand.Parameters[2].Value = chapter.StartPositionTicks;
                    _saveChapterCommand.Parameters[3].Value = chapter.Name;
                    _saveChapterCommand.Parameters[4].Value = chapter.ImagePath;

                    _saveChapterCommand.Transaction = transaction;

                    await _saveChapterCommand.ExecuteNonQueryAsync(cancellationToken);

                    index++;
                }

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            catch (Exception e)
            {
                _logger.ErrorException("Failed to save chapters:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }

                _writeLock.Release();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly object _disposeLock = new object();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                try
                {
                    lock (_disposeLock)
                    {
                        if (_connection != null)
                        {
                            if (_connection.IsOpen())
                            {
                                _connection.Close();
                            }

                            _connection.Dispose();
                            _connection = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing database", ex);
                }
            }
        }
    }
}
