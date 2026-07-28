using System;
using System.Diagnostics;
using System.IO;
using MediaBrowser.Controller.Extensions;

namespace Jellyfin.MediaEncoding.Tests.FFProcessing;

/// <summary>
/// Builds the media these tests run against, so the repository ships no binary fixtures.
/// Everything is synthesized from lavfi sources plus <c>-attach</c>.
/// </summary>
public sealed class SynthesizedMediaFixture : IDisposable
{
    private const string DefaultEncoder = "/usr/share/jellyfin-ffmpeg/ffmpeg";

    public SynthesizedMediaFixture()
    {
        EncoderPath = ResolveEncoderPath();

        // The server never configures ffprobe separately; MediaEncoder derives it from the encoder
        // path, so do the same here rather than adding a knob core does not have.
        ProberPath = Path.Combine(
            Path.GetDirectoryName(EncoderPath) ?? string.Empty,
            "ffprobe" + Path.GetExtension(EncoderPath));

        // These tests are opt-in, so a missing binary is a hard failure rather than a silent skip:
        // a run that was explicitly asked for must not report green without having run.
        if (!File.Exists(EncoderPath) || !File.Exists(ProberPath))
        {
            throw new InvalidOperationException(
                $"ffmpeg not found at '{EncoderPath}' / '{ProberPath}'. Point {EnvironmentVariable} "
                + "at an ffmpeg binary, or install jellyfin-ffmpeg.");
        }

        Root = Directory.CreateTempSubdirectory("jf-ffmpeg-tests-").FullName;

        var font = Path.Combine(Root, "TestFont.ttf");
        File.WriteAllText(font, "not really a font, but ffmpeg only copies the bytes");
        AttachmentName = Path.GetFileName(font);

        var srt = Path.Combine(Root, "s.srt");
        File.WriteAllText(srt, "1\n00:00:00,000 --> 00:00:02,000\nhello\n\n");

        const string Attach = "-metadata:s:t mimetype=application/x-truetype-font";

        // Video + audio + one attachment.
        VideoWithAttachment = Path.Combine(Root, "av_with_attachment.mkv");
        Run($"-y -f lavfi -i testsrc=d=1:s=64x64 -f lavfi -i sine=d=1 -attach \"{font}\" {Attach} "
            + $"-c:v libx264 -c:a aac -shortest \"{VideoWithAttachment}\"");

        // Subtitles + one attachment, no audio or video. This is the shape Jellyfin writes when it
        // round-trips VobSub.
        SubtitlesWithAttachment = Path.Combine(Root, "subs_only.mks");
        Run($"-y -i \"{srt}\" -attach \"{font}\" {Attach} -c:s srt -f matroska \"{SubtitlesWithAttachment}\"");

        // No attachments at all.
        VideoWithoutAttachment = Path.Combine(Root, "no_attachment.mkv");
        Run($"-y -f lavfi -i testsrc=d=1:s=64x64 -c:v libx264 \"{VideoWithoutAttachment}\"");

        // MP4 carries no attachment streams, so it exercises the same empty-map path.
        Mp4 = Path.Combine(Root, "video.mp4");
        Run($"-y -f lavfi -i testsrc=d=1:s=64x64 -f lavfi -i sine=d=1 -c:v libx264 -c:a aac -shortest \"{Mp4}\"");
    }

    /// <summary>
    /// The environment variable the server itself honours for the encoder path, which is what
    /// <c>--ffmpeg</c> sets. Configuration keys are matched case-insensitively, so the
    /// conventional upper-case spelling works too.
    /// </summary>
    private static string EnvironmentVariable => "JELLYFIN_" + ConfigurationExtensions.FfmpegPathKey;

    public string EncoderPath { get; }

    public string ProberPath { get; }

    public string Root { get; }

    public string AttachmentName { get; }

    public string VideoWithAttachment { get; }

    public string SubtitlesWithAttachment { get; }

    public string VideoWithoutAttachment { get; }

    public string Mp4 { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test run over.
        }
    }

    private static string ResolveEncoderPath()
    {
        var configured = Environment.GetEnvironmentVariable(EnvironmentVariable)
                         ?? Environment.GetEnvironmentVariable(EnvironmentVariable.ToUpperInvariant());

        return string.IsNullOrWhiteSpace(configured) ? DefaultEncoder : configured;
    }

    private void Run(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = EncoderPath,
            Arguments = "-hide_banner -loglevel error -nostdin " + arguments,
            UseShellExecute = false,
            RedirectStandardError = true
        })!;

        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Fixture setup failed ({process.ExitCode}): {stderr}");
        }
    }
}
