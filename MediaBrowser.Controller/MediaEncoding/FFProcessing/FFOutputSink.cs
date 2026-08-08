using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// Where a process's captured output goes as it arrives. The runner always drains the pipe — a
/// full one would block the child — so the choice here is only what happens to the bytes.
/// </summary>
public abstract class FFOutputSink
{
    /// <summary>
    /// How many trailing lines <see cref="Diagnostic"/> retains. Measured against jellyfin-ffmpeg
    /// 7.1.4: a failure emits three to six lines at <c>warning</c>, and the stream and format dump
    /// that precedes one runs to 42 lines at <c>info</c> and 97 at <c>verbose</c>. This is roughly
    /// double the largest of those, so the useful context survives at any log level the server
    /// actually sets.
    /// </summary>
    public const int DefaultRetainedLines = 200;

    /// <summary>
    /// Gets a value indicating whether nothing written here is ever dropped. The runner checks this
    /// for an action whose stderr is its answer rather than a diagnostic, since a sink that keeps a
    /// trailing window would silently truncate that answer.
    /// </summary>
    public virtual bool RetainsEverything => false;

    /// <summary>
    /// Gets a sink for output that only ever explains a failure. Keeps the last
    /// <see cref="DefaultRetainedLines"/> lines; everything earlier is dropped.
    /// </summary>
    /// <returns>A bounded in-memory sink.</returns>
    public static FFOutputSink Diagnostic() => new BoundedSink(DefaultRetainedLines);

    /// <summary>
    /// Gets a sink for output that <em>is</em> the answer, such as a filter's measurements. Keeps
    /// all of it, since the part that matters could be anywhere.
    /// </summary>
    /// <returns>An unbounded in-memory sink.</returns>
    public static FFOutputSink Complete() => new CompleteSink();

    /// <summary>
    /// Gets a sink that writes through to <paramref name="destination"/> as each line arrives, so a
    /// long-running process can be followed rather than read once it has finished. Nothing is
    /// retained in memory, so <see cref="FFResult.Stderr"/> stays empty.
    /// </summary>
    /// <param name="destination">The stream to write to. The caller owns closing it.</param>
    /// <returns>A pass-through sink.</returns>
    public static FFOutputSink ToStream(Stream destination) => new StreamSink(destination);

    /// <summary>
    /// Accepts one line of output.
    /// </summary>
    /// <param name="line">The line, without its terminator.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the write.</returns>
    public abstract ValueTask WriteLineAsync(string line, CancellationToken cancellationToken);

    /// <summary>
    /// Returns whatever was retained, which is empty for sinks that wrote through.
    /// </summary>
    /// <returns>The retained text.</returns>
    public abstract string GetRetainedText();

    private sealed class BoundedSink : FFOutputSink
    {
        private readonly int _maxLines;
        private readonly Queue<string> _lines = new();

        public BoundedSink(int maxLines) => _maxLines = maxLines;

        public override ValueTask WriteLineAsync(string line, CancellationToken cancellationToken)
        {
            _lines.Enqueue(line);

            // Evicting whole lines keeps the retained text readable and cannot strand half of a
            // UTF-16 surrogate pair, which cutting at a character offset can.
            while (_lines.Count > _maxLines)
            {
                _lines.Dequeue();
            }

            return ValueTask.CompletedTask;
        }

        public override string GetRetainedText() => string.Join(Environment.NewLine, _lines);
    }

    private sealed class CompleteSink : FFOutputSink
    {
        private readonly StringBuilder _builder = new();

        public override bool RetainsEverything => true;

        public override ValueTask WriteLineAsync(string line, CancellationToken cancellationToken)
        {
            if (_builder.Length > 0)
            {
                _builder.Append(Environment.NewLine);
            }

            _builder.Append(line);

            return ValueTask.CompletedTask;
        }

        public override string GetRetainedText() => _builder.ToString();
    }

    private sealed class StreamSink : FFOutputSink
    {
        private readonly Stream _destination;

        public StreamSink(Stream destination) => _destination = destination;

        public override async ValueTask WriteLineAsync(string line, CancellationToken cancellationToken)
        {
            await _destination.WriteAsync(Encoding.UTF8.GetBytes(line + Environment.NewLine), cancellationToken).ConfigureAwait(false);

            // Flush per line: the point of this sink is that the file is readable while the
            // process is still running.
            await _destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public override string GetRetainedText() => string.Empty;
    }
}
