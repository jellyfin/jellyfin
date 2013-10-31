using MediaBrowser.Common.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Common.Implementations.IO;

namespace MediaBrowser.ServerApplication.IO
{
	/// <summary>
	/// Class FileSystemFactory
	/// </summary>
	public static class FileSystemFactory
	{
		/// <summary>
		/// Creates the file system manager.
		/// </summary>
		/// <returns>IFileSystem.</returns>
		public static IFileSystem CreateFileSystemManager(ILogManager logManager)
		{
			return new CommonFileSystem(logManager.GetLogger("FileSystem"), false);
		}
	}
}
