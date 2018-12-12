using System.Runtime.InteropServices;
using MediaBrowser.Server.Mono;
using Moq;
using NUnit.Framework;

namespace EmbyServer.Test
{
    [TestFixture]
    public class ApplicationPathHelperTests
    {
        private const string JellyfinDebugWindowsPath = @"%ApplicationData%\jellyfin-debug\";
        private const string JellyfinWindowsPath = @"%ApplicationData%\jellyfin\";
        private const string JellyfinDebugUnixPath = "/tmp/jellyfin-debug/";
        private const string JellyfinUnixPath = "/tmp/jellyfin/";
        
        [TestCase(true, JellyfinDebugUnixPath)]
        [TestCase(false, JellyfinUnixPath)]
        public void AbsoluteUnixProgramDataPathsShouldBeCorrect(bool debug, string expectedPath)
        {
            var osInfoLookup = new Mock<IOperatingSystemInformationLookup>();
            osInfoLookup.Setup(o => o.GetOperatingSystem()).Returns(OSPlatform.Linux);
            osInfoLookup.Setup(o => o.GetDirectorySeparatorChar()).Returns('/');
            osInfoLookup.Setup(o => o.IsPathRooted(It.IsAny<string>())).Returns(true);

            var subject = new ApplicationPathHelper(
                JellyfinDebugUnixPath,
                JellyfinUnixPath,
                osInfoLookup.Object,
                debug);
            
            var path = subject.GetProgramDataPath("/only-used-with-relative-paths/");
            Assert.That(path, Is.EqualTo(expectedPath));
        }
        
        [TestCase(true, @"C:\ProgramData\jellyfin-debug\")]
        [TestCase(false, @"C:\ProgramData\jellyfin\")]
        public void AbsoluteWindowsProgramDataPathsShouldBeCorrect(bool debug, string expectedPath)
        {           
            var osInfoLookup = new Mock<IOperatingSystemInformationLookup>();
            osInfoLookup.Setup(o => o.GetOperatingSystem()).Returns(OSPlatform.Windows);
            osInfoLookup.Setup(o => o.GetApplicationDataPath()).Returns(@"C:\ProgramData\");
            osInfoLookup.Setup(o => o.GetDirectorySeparatorChar()).Returns('\\');
            osInfoLookup.Setup(o => o.IsPathRooted(It.IsAny<string>())).Returns(true);
            
            var subject = new ApplicationPathHelper(
                JellyfinDebugWindowsPath,
                JellyfinWindowsPath,
                osInfoLookup.Object,
                debug);
            
            var path = subject.GetProgramDataPath("/only-used-with-relative-paths/");
            Assert.That(path, Is.EqualTo(expectedPath));
        }

        [TestCase(@"C:\ProgramData")]
        [TestCase(@"C:\ProgramData\")]
        public void WindowsApplicationDataPathShouldSupportTrailingSlashes(string applicationDataPath)
        {
            var osInfoLookup = new Mock<IOperatingSystemInformationLookup>();
            osInfoLookup.Setup(o => o.GetOperatingSystem()).Returns(OSPlatform.Windows);
            osInfoLookup.Setup(o => o.GetApplicationDataPath()).Returns(@"C:\ProgramData\");
            osInfoLookup.Setup(o => o.GetDirectorySeparatorChar()).Returns('\\');
            osInfoLookup.Setup(o => o.IsPathRooted(It.IsAny<string>())).Returns(true);
            
            var debugSubject = new ApplicationPathHelper(
                JellyfinDebugWindowsPath,
                JellyfinWindowsPath,
                osInfoLookup.Object,
                true);
            
            var releaseSubject = new ApplicationPathHelper(
                JellyfinDebugWindowsPath,
                JellyfinWindowsPath,
                osInfoLookup.Object,
                false);
            
            var releasePath = releaseSubject.GetProgramDataPath("/only-used-with-relative-paths/");
            var debugPath = debugSubject.GetProgramDataPath("/only-used-with-relative-paths/");
            Assert.That(releasePath, Is.EqualTo(@"C:\ProgramData\jellyfin\"));
            Assert.That(debugPath, Is.EqualTo(@"C:\ProgramData\jellyfin-debug\"));
        }
    }
}