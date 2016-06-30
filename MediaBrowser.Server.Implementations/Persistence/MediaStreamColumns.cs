using System;
using System.Data;
using System.Text;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class MediaStreamColumns
    {
        private readonly IDbConnection _connection;
        private readonly ILogger _logger;

        public MediaStreamColumns(IDbConnection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public void AddColumns()
        {
            AddPixelFormatColumnCommand();
            AddBitDepthCommand();
            AddIsAnamorphicColumn();
            AddKeyFramesColumn();
            AddRefFramesCommand();
            AddCodecTagColumn();
            AddCommentColumn();
            AddNalColumn();
            AddIsAvcColumn();
            AddTitleColumn();
            AddTimeBaseColumn();
            AddCodecTimeBaseColumn();
        }

        private void AddIsAvcColumn()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "IsAvc", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column IsAvc BIT NULL");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddTimeBaseColumn()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "TimeBase", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column TimeBase TEXT");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddCodecTimeBaseColumn()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "CodecTimeBase", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column CodecTimeBase TEXT");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddTitleColumn()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "Title", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column Title TEXT");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddNalColumn()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "NalLengthSize", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column NalLengthSize TEXT");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddCommentColumn()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "Comment", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column Comment TEXT");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddCodecTagColumn()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "CodecTag", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column CodecTag TEXT");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddPixelFormatColumnCommand()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "PixelFormat", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column PixelFormat TEXT");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddBitDepthCommand()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "BitDepth", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column BitDepth INT NULL");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddRefFramesCommand()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "RefFrames", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column RefFrames INT NULL");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddKeyFramesColumn()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "KeyFrames", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column KeyFrames TEXT NULL");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

        private void AddIsAnamorphicColumn()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(mediastreams)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "IsAnamorphic", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table mediastreams");
            builder.AppendLine("add column IsAnamorphic BIT NULL");

            _connection.RunQueries(new[] { builder.ToString() }, _logger);
        }

    }
}
