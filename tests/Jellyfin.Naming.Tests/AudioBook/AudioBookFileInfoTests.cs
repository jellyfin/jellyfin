using Emby.Naming.AudioBook;
using Xunit;

namespace Jellyfin.Naming.Tests.AudioBook
{
    public class AudioBookFileInfoTests
    {
        [Fact]
        public void CompareTo_Same_Success()
        {
            var info = new AudioBookFileInfo();
            Assert.Equal(0, info.CompareTo(info));
        }

        [Fact]
        public void CompareTo_Null_Success()
        {
            var info = new AudioBookFileInfo();
            Assert.Equal(1, info.CompareTo(null));
        }

        [Fact]
        public void CompareTo_Empty_Success()
        {
            var info1 = new AudioBookFileInfo();
            var info2 = new AudioBookFileInfo();
            Assert.Equal(0, info1.CompareTo(info2));
        }
    }
}
