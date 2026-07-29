using System;
using System.IO;
using Jellyfin.LiveTv.Recordings;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Recordings
{
    public sealed class RecordingsManagerOrphanCleanupTests : IDisposable
    {
        private readonly string _dir;

        public RecordingsManagerOrphanCleanupTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "jf-rec-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [Fact]
        public void DeleteOrphanedRecordingArtifacts_EmptyVideo_RemovesVideoAndNfo()
        {
            var recordingPath = Path.Combine(_dir, "show.ts");
            var nfoPath = Path.Combine(_dir, "show.nfo");
            File.WriteAllBytes(recordingPath, Array.Empty<byte>());
            File.WriteAllText(nfoPath, "<episodedetails></episodedetails>");

            var cleaned = RecordingsManager.DeleteOrphanedRecordingArtifacts(recordingPath, NullLogger.Instance);

            Assert.True(cleaned);
            Assert.False(File.Exists(recordingPath));
            Assert.False(File.Exists(nfoPath));
        }

        [Fact]
        public void DeleteOrphanedRecordingArtifacts_MissingVideo_RemovesOrphanedNfo()
        {
            var recordingPath = Path.Combine(_dir, "show.ts");
            var nfoPath = Path.Combine(_dir, "show.nfo");
            File.WriteAllText(nfoPath, "<episodedetails></episodedetails>");

            var cleaned = RecordingsManager.DeleteOrphanedRecordingArtifacts(recordingPath, NullLogger.Instance);

            Assert.True(cleaned);
            Assert.False(File.Exists(nfoPath));
        }

        [Fact]
        public void DeleteOrphanedRecordingArtifacts_NonEmptyVideo_KeepsVideoAndNfo()
        {
            var recordingPath = Path.Combine(_dir, "show.ts");
            var nfoPath = Path.Combine(_dir, "show.nfo");
            File.WriteAllBytes(recordingPath, new byte[] { 1, 2, 3, 4 });
            File.WriteAllText(nfoPath, "<episodedetails></episodedetails>");

            var cleaned = RecordingsManager.DeleteOrphanedRecordingArtifacts(recordingPath, NullLogger.Instance);

            Assert.False(cleaned);
            Assert.True(File.Exists(recordingPath));
            Assert.True(File.Exists(nfoPath));
        }

        [Fact]
        public void DeleteOrphanedRecordingArtifacts_EmptyVideo_RemovesEmptyRecordingFolder()
        {
            var folder = Path.Combine(_dir, "Scooby-Doo and Scrappy-Doo");
            Directory.CreateDirectory(folder);
            var recordingPath = Path.Combine(folder, "episode.ts");
            File.WriteAllBytes(recordingPath, Array.Empty<byte>());

            var cleaned = RecordingsManager.DeleteOrphanedRecordingArtifacts(recordingPath, NullLogger.Instance);

            Assert.True(cleaned);
            Assert.False(Directory.Exists(folder));
        }

        [Fact]
        public void DeleteOrphanedRecordingArtifacts_NonEmptyVideo_KeepsRecordingFolder()
        {
            var folder = Path.Combine(_dir, "Some Show");
            Directory.CreateDirectory(folder);
            var recordingPath = Path.Combine(folder, "episode.ts");
            File.WriteAllBytes(recordingPath, new byte[] { 1, 2, 3, 4 });

            var cleaned = RecordingsManager.DeleteOrphanedRecordingArtifacts(recordingPath, NullLogger.Instance);

            Assert.False(cleaned);
            Assert.True(Directory.Exists(folder));
        }

        [Fact]
        public void DeleteOrphanedRecordingArtifacts_EmptyVideo_KeepsFolderWithOtherRecordings()
        {
            var folder = Path.Combine(_dir, "Series With Episodes");
            Directory.CreateDirectory(folder);
            var recordingPath = Path.Combine(folder, "failed.ts");
            File.WriteAllBytes(recordingPath, Array.Empty<byte>());
            var goodEpisode = Path.Combine(folder, "good.ts");
            File.WriteAllBytes(goodEpisode, new byte[] { 1, 2, 3, 4 });

            var cleaned = RecordingsManager.DeleteOrphanedRecordingArtifacts(recordingPath, NullLogger.Instance);

            Assert.True(cleaned);
            Assert.False(File.Exists(recordingPath));
            Assert.True(Directory.Exists(folder));
            Assert.True(File.Exists(goodEpisode));
        }

        [Fact]
        public void HasUsableRecording_NonEmptyFile_ReturnsTrue()
        {
            var recordingPath = Path.Combine(_dir, "show.ts");
            File.WriteAllBytes(recordingPath, new byte[] { 1, 2, 3, 4 });

            Assert.True(RecordingsManager.HasUsableRecording(recordingPath));
        }

        [Fact]
        public void HasUsableRecording_EmptyFile_ReturnsFalse()
        {
            var recordingPath = Path.Combine(_dir, "show.ts");
            File.WriteAllBytes(recordingPath, Array.Empty<byte>());

            Assert.False(RecordingsManager.HasUsableRecording(recordingPath));
        }

        [Fact]
        public void HasUsableRecording_MissingFile_ReturnsFalse()
        {
            var recordingPath = Path.Combine(_dir, "does-not-exist.ts");

            Assert.False(RecordingsManager.HasUsableRecording(recordingPath));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_dir, true);
            }
            catch (IOException)
            {
                // best effort cleanup
            }
        }
    }
}
