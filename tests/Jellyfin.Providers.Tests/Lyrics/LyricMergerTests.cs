using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Providers.Lyric;
using Xunit;

namespace Jellyfin.Providers.Tests.Lyrics;

public class LyricMergerTests
{
    private static LyricLine Line(string text, int? seconds, bool karaoke = false)
        => new(text, seconds * TimeSpan.TicksPerSecond)
        {
            End = (seconds + 1) * TimeSpan.TicksPerSecond,
            Syllables = karaoke && seconds.HasValue ? [new LyricSyllable { Text = text, Start = seconds.Value * TimeSpan.TicksPerSecond, End = (seconds.Value + 1) * TimeSpan.TicksPerSecond }] : []
        };

    private static LyricDto Source(params LyricLine[] lines)
        => new() { Tracks = [new LyricTrack { Type = LyricTrackType.Main, Lines = lines }] };

    private static LyricDto WithTranslation(LyricDto source, string? language, params LyricLine[] lines)
        => new() { Tracks = source.Tracks.Concat([new LyricTrack { Type = LyricTrackType.Translation, Language = language, Lines = lines }]).ToArray() };

    [Fact]
    public void SameTimestampLinesUseNextDistinctTimestamp()
    {
        var parsed = new LrcLyricParser().ParseLyrics(new LyricFile("sample.lrc", "[00:01.00]Hello\n[00:01.00]Bonjour\n[00:03.00]World\n[00:03.00]Monde"));
        Assert.NotNull(parsed);
        Assert.Equal(3 * TimeSpan.TicksPerSecond, parsed.Lyrics[0].End);
        Assert.Equal(parsed.Lyrics[0].End, parsed.Lyrics[1].End);
        Assert.Null(parsed.Lyrics[2].End);
        Assert.Null(parsed.Lyrics[3].End);
    }

    [Theory]
    [InlineData("Hello there", "Bonjour à tous", "Goodbye now", "Au revoir")]
    [InlineData("Buenos días", "Bom dia", "Hasta mañana", "Até amanhã")]
    [InlineData("مرحبا بالعالم", "नमस्ते दुनिया", "إلى اللقاء", "फिर मिलेंगे")]
    [InlineData("こんにちは", "안녕하세요", "また明日", "내일 만나요")]
    public void PairedLrcDoesNotDependOnLanguageOrScript(string first, string translation, string second, string secondTranslation)
    {
        var source = Source(Line(first, 1), Line(translation, 1), Line(second, 3), Line(secondTranslation, 3));
        var merged = LyricMerger.Merge([source]);
        Assert.NotNull(merged);
        Assert.Equal([first, second], merged.Lyrics.Select(l => l.Text));
        var translated = Assert.Single(merged.Tracks, t => t.Type == LyricTrackType.Translation);
        Assert.Equal([translation, secondTranslation], translated.Lines.Select(l => l.Text));
        Assert.Null(translated.Language);
        Assert.Equal(4, source.Lyrics.Count);
    }

    [Fact]
    public void DifferentSpeakersAreNotAssumedToBeTranslations()
    {
        var source = Source(Line("v1: Hello", 1), Line("v2: Different words", 1), Line("v1: Goodbye", 3), Line("v2: More words", 3));
        var merged = LyricMerger.Merge([source]);
        Assert.NotNull(merged);
        Assert.Single(merged.Tracks);
        Assert.Equal(4, merged.Lyrics.Count);
    }

    [Fact]
    public void IndependentTimedVocalPartsAreNotSplit()
    {
        var merged = LyricMerger.Merge([Source(Line("Hello", 1, true), Line("Bonjour", 1, true), Line("Goodbye", 3, true), Line("Au revoir", 3, true))]);
        Assert.NotNull(merged);
        Assert.Single(merged.Tracks);
    }

    [Fact]
    public void SingleUnlabelledPairIsNotEnoughEvidence()
    {
        var merged = LyricMerger.Merge([Source(Line("Hello", 1), Line("Bonjour", 1))]);
        Assert.NotNull(merged);
        Assert.Single(merged.Tracks);
    }

    [Fact]
    public void UsesKaraokeOriginalAndAlignsShiftedTranslationByText()
    {
        var primary = Source(Line("Hello, WORLD!", 10, true), Line("Goodbye now", 20, true));
        var secondary = WithTranslation(Source(Line("hello world", 100), Line("goodbye now", 200)), "fr", Line("Bonjour le monde", 100), Line("Au revoir", 200));
        var merged = LyricMerger.Merge([secondary, primary]);
        Assert.NotNull(merged);
        Assert.Same(primary.Lyrics[0], merged.Lyrics[0]);
        var translations = Assert.Single(merged.Tracks, t => t.Type == LyricTrackType.Translation);
        Assert.Equal([10 * TimeSpan.TicksPerSecond, 20 * TimeSpan.TicksPerSecond], translations.Lines.Select(l => l.Start));
        Assert.Equal(primary.Lyrics[0].End, translations.Lines[0].End);
        Assert.Equal("fr", translations.Language);
        Assert.Equal(100 * TimeSpan.TicksPerSecond, secondary.Tracks[1].Lines[0].Start);
    }

    [Fact]
    public void SyncedOriginalBeatsUnsyncedOriginal()
    {
        var synced = Source(Line("Hello", 1), Line("Goodbye", 3));
        var plain = WithTranslation(Source(Line("Hello", null), Line("Goodbye", null)), "fr", Line("Bonjour", null), Line("Au revoir", null));
        var merged = LyricMerger.Merge([plain, synced]);
        Assert.NotNull(merged);
        Assert.Equal(1 * TimeSpan.TicksPerSecond, merged.Lyrics[0].Start);
        Assert.Equal(1 * TimeSpan.TicksPerSecond, merged.Tracks.Single(t => t.Type == LyricTrackType.Translation).Lines[0].Start);
    }

    [Fact]
    public void RepeatedChorusIsMatchedInContextAfterMissingVerse()
    {
        var primary = Source(Line("Opening line", 1, true), Line("Come back home", 2, true), Line("Bridge changes everything", 3, true), Line("Come back home", 4, true), Line("Closing line", 5, true));
        var secondary = WithTranslation(Source(Line("Opening line", 101), Line("Bridge changes everything", 103), Line("Come back home", 104), Line("Closing line", 105)), "de", Line("Anfang", 101), Line("Brücke", 103), Line("Komm nach Hause", 104), Line("Ende", 105));
        var merged = LyricMerger.Merge([primary, secondary]);
        Assert.NotNull(merged);
        var translated = merged.Tracks.Single(t => t.Type == LyricTrackType.Translation);
        Assert.Equal(4 * TimeSpan.TicksPerSecond, translated.Lines.Single(l => l.Text == "Komm nach Hause").Start);
        Assert.DoesNotContain(translated.Lines, l => l.Start == 2 * TimeSpan.TicksPerSecond);
    }

    [Fact]
    public void MatchingTimestampsDoNotMatchUnrelatedOriginalText()
    {
        var primary = Source(Line("A completely different sentence", 1, true));
        var other = WithTranslation(Source(Line("Nothing related to that", 1)), "fr", Line("Sans rapport", 1));
        var merged = LyricMerger.Merge([primary, other]);
        Assert.NotNull(merged);
        Assert.DoesNotContain(merged.Tracks, t => t.Type == LyricTrackType.Translation);
    }

    [Fact]
    public void ExistingTranslationWinsAndMissingLinesAreFilled()
    {
        var primary = WithTranslation(Source(Line("Hello", 1, true), Line("Goodbye", 3, true)), "fr", Line("Original translation", 1));
        var other = WithTranslation(Source(Line("Hello", 10), Line("Goodbye", 30)), "fr", Line("Replacement translation", 10), Line("Au revoir", 30));
        var merged = LyricMerger.Merge([primary, other]);
        Assert.NotNull(merged);
        var track = Assert.Single(merged.Tracks, t => t.Type == LyricTrackType.Translation);
        Assert.Equal(["Original translation", "Au revoir"], track.Lines.Select(l => l.Text));
        Assert.Single(primary.Tracks[1].Lines);
    }

    [Fact]
    public void OriginalAnchorIdentifiesTranslationFirstLrc()
    {
        var primary = Source(Line("Hello there", 10, true), Line("Goodbye now", 20, true));
        var other = Source(Line("Bonjour à tous", 1), Line("Hello there", 1), Line("Au revoir", 2), Line("Goodbye now", 2));
        var merged = LyricMerger.Merge([primary, other]);
        Assert.NotNull(merged);
        Assert.Equal(["Bonjour à tous", "Au revoir"], merged.Tracks.Single(t => t.Type == LyricTrackType.Translation).Lines.Select(l => l.Text));
    }

    [Fact]
    public void UnsyncedAlternatingTextNeedsIndependentOriginalAnchor()
    {
        var other = Source(Line("Hello there", null), Line("Bonjour à tous", null), Line("Goodbye now", null), Line("Au revoir", null));
        var alone = LyricMerger.Merge([other]);
        Assert.NotNull(alone);
        Assert.Single(alone.Tracks);
        var merged = LyricMerger.Merge([Source(Line("Hello there", 10, true), Line("Goodbye now", 20, true)), other]);
        Assert.NotNull(merged);
        Assert.Equal(["Bonjour à tous", "Au revoir"], merged.Tracks.Single(t => t.Type == LyricTrackType.Translation).Lines.Select(l => l.Text));
    }

    [Fact]
    public void PreservesDifferentExplicitTranslationLanguages()
    {
        var first = WithTranslation(Source(Line("Hello", 1, true)), "fr", Line("Bonjour", 1));
        var second = WithTranslation(Source(Line("Hello", 11)), "es", Line("Hola", 11));
        var merged = LyricMerger.Merge([first, second]);
        Assert.NotNull(merged);
        Assert.Equal(["fr", "es"], merged.Tracks.Where(t => t.Type == LyricTrackType.Translation).Select(t => t.Language));
    }

    [Fact]
    public void DoesNotInferTranslationLanguageFromOneCommonShortWord()
    {
        var primary = WithTranslation(Source(Line("Hello", 1, true), Line("Goodbye", 3, true)), "fr", Line("Oh", 1), Line("Au revoir", 3));
        var secondary = WithTranslation(Source(Line("Hello", 10), Line("Goodbye", 30)), null, Line("Oh", 10), Line("Hasta luego", 30));
        var merged = LyricMerger.Merge([primary, secondary]);
        Assert.NotNull(merged);
        Assert.Equal(2, merged.Tracks.Count(t => t.Type == LyricTrackType.Translation));
        Assert.Equal("Hasta luego", merged.Tracks.Single(t => t.Type == LyricTrackType.Translation && t.Language is null).Lines[1].Text);
    }

    [Fact]
    public void MatchesMinorTextVariationWithoutUsingItsClock()
    {
        var primary = Source(Line("You are always here beside me", 1, true));
        var other = WithTranslation(Source(Line("You are always here besides me", 100)), "fr", Line("Toujours avec moi", 100));
        var merged = LyricMerger.Merge([primary, other]);
        Assert.NotNull(merged);
        Assert.Equal(1 * TimeSpan.TicksPerSecond, merged.Tracks.Single(t => t.Type == LyricTrackType.Translation).Lines[0].Start);
    }

    [Theory]
    [InlineData("4. shouldn't i give up.m4a", "shouldn’t i give up.ttml")]
    [InlineData("2. 4Me 4Me.m4a", "4Me 4Me.ttml")]
    [InlineData("1. Lose My Mind.m4a", "Lose My Mind (feat. Doja Cat).ttml")]
    public void FindsUnambiguousSidecarsWithoutRenamingFiles(string audio, string lyric)
    {
        var files = new List<FileSystemMetadata> { new() { FullName = audio }, new() { FullName = lyric } };
        Assert.Equal(lyric, Assert.Single(LyricSourcePaths.Find(audio, files, [".ttml"])));
    }

    [Fact]
    public void DoesNotGuessAmbiguousSidecarAssociation()
    {
        var files = new List<FileSystemMetadata> { new() { FullName = "1. Song.m4a" }, new() { FullName = "2. Song.m4a" }, new() { FullName = "Song.ttml" } };
        Assert.Empty(LyricSourcePaths.Find("1. Song.m4a", files, [".ttml"]));
    }

    [Theory]
    [InlineData("कल", "कला")]
    [InlineData("की", "कि")]
    [InlineData("कु", "कू")]
    public void KeepsMeaningfulCombiningMarksWhenMatchingOriginalText(string first, string second)
    {
        Assert.NotEqual(LyricMerger.Normalize(first), LyricMerger.Normalize(second));
    }
}
