using System;
using System.Collections.Generic;
using System.Reflection;
using Emby.Server.Core;
using Emby.Server.Implementations;
using Emby.Server.Implementations.FFMpeg;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using Emby.Server.Mac.Native;
using System.Diagnostics;

namespace MediaBrowser.Server.Mac
{
	public class MacAppHost : ApplicationHost
	{
        public MacAppHost(ServerApplicationPaths applicationPaths, ILogManager logManager, StartupOptions options, IFileSystem fileSystem, IPowerManagement powerManagement, string releaseAssetFilename, IEnvironmentInfo environmentInfo, MediaBrowser.Controller.Drawing.IImageEncoder imageEncoder, ISystemEvents systemEvents, IMemoryStreamFactory memoryStreamFactory, MediaBrowser.Common.Net.INetworkManager networkManager, Action<string, string> certificateGenerator, Func<string> defaultUsernameFactory) : base(applicationPaths, logManager, options, fileSystem, powerManagement, releaseAssetFilename, environmentInfo, imageEncoder, systemEvents, memoryStreamFactory, networkManager, certificateGenerator, defaultUsernameFactory)
        {
        }

        public override bool CanSelfRestart
        {
            get
            {
                return false;
            }
        }

        public override bool CanSelfUpdate
        {
            get
            {
                return false;
            }
        }

		protected override bool SupportsDualModeSockets
		{
			get
			{
				return true;
			}
		}

        protected override FFMpegInstallInfo GetFfmpegInstallInfo()
        {
            var info = new FFMpegInstallInfo();

            info.ArchiveType = "7z";

            switch (EnvironmentInfo.SystemArchitecture)
            {
                case Architecture.X64:
                    info.Version = "20160124";
                    break;
                case Architecture.X86:
                    info.Version = "20150110";
                    break;
            }

            info.DownloadUrls = GetDownloadUrls();

            return info;
        }

        private string[] GetDownloadUrls()
        {
            switch (EnvironmentInfo.SystemArchitecture)
            {
                case Architecture.X64:
                    return new[]
                    {
                                "https://github.com/MediaBrowser/Emby.Resources/raw/master/ffmpeg/osx/ffmpeg-x64-2.8.5.7z"
                            };
            }

            // No version available 
            return new string[] { };
        }

        protected override void RestartInternal()
        {
            MainClass.Restart();
        }

        protected override List<Assembly> GetAssembliesWithPartsInternal()
        {
            var list = new List<Assembly>();

            list.Add(GetType().Assembly);

            return list;
        }

        protected override void ShutdownInternal()
        {
            MainClass.Shutdown();
        }

        protected override void AuthorizeServer()
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureAutoRunInternal(bool autorun)
        {
            throw new NotImplementedException();
        }

        public override void LaunchUrl(string url)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = url
                },

                EnableRaisingEvents = true,
            };

            process.Exited += ProcessExited;

            process.Start();
        }

        private static void ProcessExited(object sender, EventArgs e)
        {
            ((Process)sender).Dispose();
        }

        protected override void EnableLoopbackInternal(string appName)
        {
        }

        public override bool SupportsRunningAsService
        {
            get
            {
                return false;
            }
        }

        public override bool SupportsAutoRunAtStartup
        {
            get { return false; }
        }

        public override bool IsRunningAsService
        {
            get
            {
                return false;
            }
        }
    }
}
