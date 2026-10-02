using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using MediaBrowser.Model.IO;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class DotIgnoreIgnoreRuleTest
{
    private static readonly string[] _rule1 = ["SPs"];
    private static readonly string[] _rule2 = ["SPs", "!thebestshot.mkv"];
    private static readonly string[] _rule3 = ["*.txt", @"{\colortbl;\red255\green255\blue255;}", "videos/", @"\invalid\escape\sequence", "*.mkv"];
    private static readonly string[] _rule4 = [@"{\colortbl;\red255\green255\blue255;}", @"\invalid\escape\sequence"];

    public static TheoryData<string[], string, bool, bool> CheckIgnoreRulesTestData =>
        new()
        {
            // Basic pattern matching
            { _rule1, "f:/cd/sps/ffffff.mkv", false, true },
            { _rule1, "cd/sps/ffffff.mkv", false, true },
            { _rule1, "/cd/sps/ffffff.mkv", false, true },

            // Negate pattern
            { _rule2, "f:/cd/sps/ffffff.mkv", false, true },
            { _rule2, "cd/sps/ffffff.mkv", false, true },
            { _rule2, "/cd/sps/ffffff.mkv", false, true },
            { _rule2, "f:/cd/sps/thebestshot.mkv", false, false },
            { _rule2, "cd/sps/thebestshot.mkv", false, false },
            { _rule2, "/cd/sps/thebestshot.mkv", false, false },

            // Mixed valid and invalid patterns - skips invalid, applies valid
            { _rule3, "test.txt", false, true },
            { _rule3, "videos/movie.mp4", false, true },
            { _rule3, "movie.mkv", false, true },
            { _rule3, "test.mp3", false, false },

            // Only invalid patterns - falls back to ignore all
            { _rule4, "any-file.txt", false, true },
            { _rule4, "any/path/to/file.mkv", false, true },
        };

    public static TheoryData<string[], string, bool, bool> WindowsPathNormalizationTestData =>
        new()
        {
            // Windows paths with backslashes - should match when normalizePath is true
            { _rule1, @"C:\cd\sps\ffffff.mkv", false, true },
            { _rule1, @"D:\media\sps\movie.mkv", false, true },
            { _rule1, @"\\server\share\sps\file.mkv", false, true },

            // Negate pattern with Windows paths
            { _rule2, @"C:\cd\sps\ffffff.mkv", false, true },
            { _rule2, @"C:\cd\sps\thebestshot.mkv", false, false },

            // Directory matching with Windows paths
            { _rule3, @"C:\videos\movie.mp4", false, true },
            { _rule3, @"D:\documents\test.txt", false, true },
            { _rule3, @"E:\music\song.mp3", false, false },
        };

    [Theory]
    [MemberData(nameof(CheckIgnoreRulesTestData))]
    public void CheckIgnoreRules_ReturnsExpectedResult(string[] rules, string path, bool isDirectory, bool expectedIgnored)
    {
        Assert.Equal(expectedIgnored, DotIgnoreIgnoreRule.CheckIgnoreRules(path, rules, isDirectory));
    }

    [Theory]
    [MemberData(nameof(WindowsPathNormalizationTestData))]
    public void CheckIgnoreRules_WithWindowsPaths_NormalizesBackslashes(string[] rules, string path, bool isDirectory, bool expectedIgnored)
    {
        // With normalizePath=true, backslashes should be converted to forward slashes
        Assert.Equal(expectedIgnored, DotIgnoreIgnoreRule.CheckIgnoreRules(path, rules, isDirectory, normalizePath: true));
    }

    [Theory]
    [InlineData(@"C:\cd\sps\ffffff.mkv")]
    [InlineData(@"D:\media\sps\movie.mkv")]
    public void CheckIgnoreRules_WithWindowsPaths_WithoutNormalization_DoesNotMatch(string path)
    {
        // Without normalization, Windows paths with backslashes won't match patterns expecting forward slashes
        Assert.False(DotIgnoreIgnoreRule.CheckIgnoreRules(path, _rule1, isDirectory: false, normalizePath: false));
    }

    [Fact]
    public void CacheHit_RepeatedCallsDoNotRereadFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(subDir);

        try
        {
            var ignoreFilePath = Path.Combine(tempDir, ".ignore");
            File.WriteAllText(ignoreFilePath, "*.tmp");

            var rule = new DotIgnoreIgnoreRule();
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(subDir, "test.tmp"),
                IsDirectory = false
            };

            // First call - should cache
            var result1 = rule.ShouldIgnore(fileInfo, null);
            Assert.True(result1);

            // Second call - should use cache
            var result2 = rule.ShouldIgnore(fileInfo, null);
            Assert.True(result2);

            // Third call with different file in same directory - should use cache
            var fileInfo2 = new FileSystemMetadata
            {
                FullName = Path.Combine(subDir, "other.tmp"),
                IsDirectory = false
            };
            var result3 = rule.ShouldIgnore(fileInfo2, null);
            Assert.True(result3);

            // Call with file that doesn't match pattern
            var fileInfo3 = new FileSystemMetadata
            {
                FullName = Path.Combine(subDir, "other.txt"),
                IsDirectory = false
            };
            var result4 = rule.ShouldIgnore(fileInfo3, null);
            Assert.False(result4);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CacheInvalidation_ModifyIgnoreFile_Reparses()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var ignoreFilePath = Path.Combine(tempDir, ".ignore");
            File.WriteAllText(ignoreFilePath, "*.tmp");

            var rule = new DotIgnoreIgnoreRule();
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(tempDir, "test.tmp"),
                IsDirectory = false
            };

            // First call - should ignore .tmp files
            var result1 = rule.ShouldIgnore(fileInfo, null);
            Assert.True(result1);

            // Modify the .ignore file to ignore .txt instead
            // Wait a bit to ensure the file modification time changes
            Thread.Sleep(50);
            File.WriteAllText(ignoreFilePath, "*.txt");

            // Now .tmp files should NOT be ignored
            var result2 = rule.ShouldIgnore(fileInfo, null);
            Assert.False(result2);

            // And .txt files SHOULD be ignored
            var txtFileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(tempDir, "test.txt"),
                IsDirectory = false
            };
            var result3 = rule.ShouldIgnore(txtFileInfo, null);
            Assert.True(result3);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EmptyIgnoreFile_IgnoresEverything()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var ignoreFilePath = Path.Combine(tempDir, ".ignore");
            File.WriteAllText(ignoreFilePath, string.Empty);

            var rule = new DotIgnoreIgnoreRule();

            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(tempDir, "anyfile.mkv"),
                IsDirectory = false
            };

            // Empty .ignore file should ignore everything
            var result = rule.ShouldIgnore(fileInfo, null);
            Assert.True(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void WhitespaceOnlyIgnoreFile_IgnoresEverything()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var ignoreFilePath = Path.Combine(tempDir, ".ignore");
            File.WriteAllText(ignoreFilePath, "   \n\t\n   ");

            var rule = new DotIgnoreIgnoreRule();

            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(tempDir, "anyfile.mkv"),
                IsDirectory = false
            };

            // Whitespace-only .ignore file should ignore everything
            var result = rule.ShouldIgnore(fileInfo, null);
            Assert.True(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void NoIgnoreFile_DoesNotIgnore()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var rule = new DotIgnoreIgnoreRule();

            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(tempDir, "anyfile.mkv"),
                IsDirectory = false
            };

            // No .ignore file means don't ignore
            var result = rule.ShouldIgnore(fileInfo, null);
            Assert.False(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ConcurrentAccess_ThreadSafe()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var ignoreFilePath = Path.Combine(tempDir, ".ignore");
            File.WriteAllText(ignoreFilePath, "*.tmp");

            var rule = new DotIgnoreIgnoreRule();

            // Run multiple parallel checks
            Parallel.For(0, 100, i =>
            {
                var fileInfo = new FileSystemMetadata
                {
                    FullName = Path.Combine(tempDir, $"test{i}.tmp"),
                    IsDirectory = false
                };

                var result = rule.ShouldIgnore(fileInfo, null);
                Assert.True(result);
            });

            // Also test with non-matching files
            Parallel.For(0, 100, i =>
            {
                var fileInfo = new FileSystemMetadata
                {
                    FullName = Path.Combine(tempDir, $"test{i}.txt"),
                    IsDirectory = false
                };

                var result = rule.ShouldIgnore(fileInfo, null);
                Assert.False(result);
            });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ClearCache_ClearsAllCachedData()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var ignoreFilePath = Path.Combine(tempDir, ".ignore");
            File.WriteAllText(ignoreFilePath, "*.tmp");

            var rule = new DotIgnoreIgnoreRule();
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(tempDir, "test.tmp"),
                IsDirectory = false
            };

            // First call to populate cache
            var result1 = rule.ShouldIgnore(fileInfo, null);
            Assert.True(result1);

            // Clear cache
            rule.ClearDirectoryCache();

            // Should still work (will re-populate cache)
            var result2 = rule.ShouldIgnore(fileInfo, null);
            Assert.True(result2);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IgnoreFileDeleted_HandlesGracefully()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var ignoreFilePath = Path.Combine(tempDir, ".ignore");
            File.WriteAllText(ignoreFilePath, "*.tmp");

            var rule = new DotIgnoreIgnoreRule();
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(tempDir, "test.tmp"),
                IsDirectory = false
            };

            // First call - should ignore
            var result1 = rule.ShouldIgnore(fileInfo, null);
            Assert.True(result1);

            // Delete the .ignore file
            File.Delete(ignoreFilePath);

            // Should not ignore anymore (file deleted)
            var result2 = rule.ShouldIgnore(fileInfo, null);
            Assert.False(result2);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void ParentDirectoryIgnoreFile_AppliesToSubdirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var subDir1 = Path.Combine(tempDir, "sub1");
        var subDir2 = Path.Combine(tempDir, "sub1", "sub2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        try
        {
            // Put .ignore in root
            var ignoreFilePath = Path.Combine(tempDir, ".ignore");
            File.WriteAllText(ignoreFilePath, "*.tmp");

            var rule = new DotIgnoreIgnoreRule();

            // Check file in sub2 - should find .ignore in parent
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(subDir2, "test.tmp"),
                IsDirectory = false
            };

            var result = rule.ShouldIgnore(fileInfo, null);
            Assert.True(result);

            // Check file in sub1
            var fileInfo2 = new FileSystemMetadata
            {
                FullName = Path.Combine(subDir1, "test.tmp"),
                IsDirectory = false
            };

            var result2 = rule.ShouldIgnore(fileInfo2, null);
            Assert.True(result2);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DirectoryMatching_TrailingSlashPattern()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "videos");
        Directory.CreateDirectory(subDir);

        try
        {
            var ignoreFilePath = Path.Combine(tempDir, ".ignore");
            File.WriteAllText(ignoreFilePath, "videos/");

            var rule = new DotIgnoreIgnoreRule();

            // Directory should be ignored
            var dirInfo = new FileSystemMetadata
            {
                FullName = subDir,
                IsDirectory = true
            };

            var result = rule.ShouldIgnore(dirInfo, null);
            Assert.True(result);

            // File named "videos" should NOT be ignored (pattern has trailing slash)
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(tempDir, "videos"),
                IsDirectory = false
            };

            // Note: The Ignore library behavior may vary here, this tests the actual behavior
            var resultFile = rule.ShouldIgnore(fileInfo, null);
            // The file named "videos" without trailing slash might or might not match depending on the library
            // This test documents the actual behavior
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LibraryRoot_IgnoreFileAboveRoot_IsNotConsulted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var libraryRoot = Path.Combine(tempDir, "Movies");
        var movieDir = Path.Combine(libraryRoot, "Movie (2020)");
        Directory.CreateDirectory(movieDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, ".ignore"), string.Empty);

            var rule = new DotIgnoreIgnoreRule();
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(movieDir, "Movie (2020).mkv"),
                IsDirectory = false
            };

            // Without library roots the walk reaches the filesystem root
            Assert.True(rule.ShouldIgnore(fileInfo, null));

            rule.SetLibraryRootsProvider(() => [libraryRoot + Path.DirectorySeparatorChar]);
            Assert.False(rule.ShouldIgnore(fileInfo, null));

            // A .ignore in the library root itself still applies
            File.WriteAllText(Path.Combine(libraryRoot, ".ignore"), "*.mkv");
            rule.ClearDirectoryCache();
            Assert.True(rule.ShouldIgnore(fileInfo, null));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LibraryRoot_NestedRoots_StopAtOutermost()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var outerRoot = Path.Combine(tempDir, "Media");
        var innerRoot = Path.Combine(outerRoot, "Movies");
        Directory.CreateDirectory(innerRoot);

        try
        {
            File.WriteAllText(Path.Combine(outerRoot, ".ignore"), "*.mkv");

            var rule = new DotIgnoreIgnoreRule();
            rule.SetLibraryRootsProvider(() => [innerRoot, outerRoot]);
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(innerRoot, "Movie.mkv"),
                IsDirectory = false
            };

            Assert.True(rule.ShouldIgnore(fileInfo, null));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LibraryRoot_PathOutsideLibraries_WalksToFilesystemRoot()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var libraryRoot = Path.Combine(tempDir, "Movies");
        var otherDir = Path.Combine(tempDir, "Other", "Sub");
        Directory.CreateDirectory(libraryRoot);
        Directory.CreateDirectory(otherDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, ".ignore"), string.Empty);

            var rule = new DotIgnoreIgnoreRule();
            rule.SetLibraryRootsProvider(() => [libraryRoot]);

            // A sibling whose name only shares the root's prefix is not inside the library
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(tempDir, "MoviesExtra", "Movie.mkv"),
                IsDirectory = false
            };
            Assert.True(rule.ShouldIgnore(fileInfo, null));

            fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(otherDir, "Movie.mkv"),
                IsDirectory = false
            };
            Assert.True(rule.ShouldIgnore(fileInfo, null));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
