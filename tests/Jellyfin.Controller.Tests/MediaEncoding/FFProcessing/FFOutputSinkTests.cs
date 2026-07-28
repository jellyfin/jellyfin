using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding.FFProcessing;

public static class FFOutputSinkTests
{
    private static async Task<FFOutputSink> FillAsync(FFOutputSink sink, params string[] lines)
    {
        foreach (var line in lines)
        {
            await sink.WriteLineAsync(line, CancellationToken.None);
        }

        return sink;
    }

    [Fact]
    public static async Task Diagnostic_KeepsEverythingUnderBudget()
    {
        var sink = await FillAsync(FFOutputSink.Diagnostic(), "first", "second", "third");

        Assert.Equal(
            string.Join(Environment.NewLine, "first", "second", "third"),
            sink.GetRetainedText());
    }

    [Fact]
    public static async Task Diagnostic_KeepsExactlyTheLastLines()
    {
        var sink = FFOutputSink.Diagnostic();
        var lines = Enumerable.Range(0, FFOutputSink.DefaultRetainedLines * 3).Select(i => $"line {i}").ToArray();
        await FillAsync(sink, lines);

        var retained = sink.GetRetainedText().Split(Environment.NewLine);

        Assert.Equal(FFOutputSink.DefaultRetainedLines, retained.Length);
        Assert.Equal(lines[^FFOutputSink.DefaultRetainedLines..], retained);
    }

    [Fact]
    public static async Task Diagnostic_EvictsWholeLinesNotFragments()
    {
        var sink = FFOutputSink.Diagnostic();
        var lines = Enumerable.Range(0, 1000).Select(i => $"line {i} " + new string('x', 200)).ToArray();
        await FillAsync(sink, lines);

        // Every retained line is intact: none was cut mid-string.
        Assert.All(
            sink.GetRetainedText().Split(Environment.NewLine),
            line => Assert.Contains(lines, original => original == line));
    }

    [Fact]
    public static async Task Diagnostic_DoesNotSplitSurrogatePairs()
    {
        // Each line is entirely non-BMP, so a blind character-offset cut would strand a lone
        // surrogate. Whole-line eviction cannot.
        var sink = FFOutputSink.Diagnostic();
        var line = string.Concat(Enumerable.Repeat("\U0001F600", 500));
        await FillAsync(sink, Enumerable.Repeat(line, FFOutputSink.DefaultRetainedLines * 3).ToArray());

        var retained = sink.GetRetainedText();

        // EnumerateRunes yields the replacement character for a stranded surrogate, so its
        // absence is exactly the property we want.
        Assert.DoesNotContain(retained.EnumerateRunes(), rune => rune == Rune.ReplacementChar);
    }

    [Fact]
    public static async Task Diagnostic_KeepsAVeryLongLineWhole()
    {
        // The bound is a line count, so length does not cause truncation. ReadLineAsync already
        // materialised the string, so cutting it would not save the allocation anyway.
        var huge = new string('y', 512 * 1024);
        var sink = await FillAsync(FFOutputSink.Diagnostic(), huge);

        Assert.Equal(huge, sink.GetRetainedText());
    }

    [Fact]
    public static async Task Complete_KeepsEverything()
    {
        var sink = FFOutputSink.Complete();
        var lines = Enumerable.Range(0, 5000).Select(i => $"line {i}").ToArray();
        await FillAsync(sink, lines);

        Assert.Equal(lines, sink.GetRetainedText().Split(Environment.NewLine));
    }

    [Fact]
    public static async Task ToStream_WritesThroughAndRetainsNothing()
    {
        using var destination = new MemoryStream();
        var sink = await FillAsync(FFOutputSink.ToStream(destination), "alpha", "beta");

        Assert.Equal(string.Empty, sink.GetRetainedText());
        Assert.Equal(
            "alpha" + Environment.NewLine + "beta" + Environment.NewLine,
            Encoding.UTF8.GetString(destination.ToArray()));
    }
}
