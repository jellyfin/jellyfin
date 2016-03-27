using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Power;

namespace MediaBrowser.Server.Mac
{
    public abstract class BaseMonoApp : INativeApp
    {
        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public abstract void Shutdown();

        /// <summary>
        /// Restarts this instance.
        /// </summary>
		public virtual void Restart(StartupOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether this instance [can self restart].
        /// </summary>
        /// <returns><c>true</c> if this instance [can self restart]; otherwise, <c>false</c>.</returns>
        public virtual bool CanSelfRestart
        {
            get
            {
                return false;
            }
        }

		public virtual bool SupportsLibraryMonitor
		{
			get
			{
				return true;
			}
		}

        /// <summary>
        /// Gets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public bool CanSelfUpdate
        {
            get
            {
                return false;
            }
        }

        public bool SupportsAutoRunAtStartup
        {
            get { return false; }
        }

        public void PreventSystemStandby()
        {

        }

        public List<Assembly> GetAssembliesWithParts()
        {
            var list = new List<Assembly>();

            list.Add(GetType().Assembly);

            return list;
        }

		public void AuthorizeServer(int udpPort, int httpServerPort, int httpsPort, string applicationPath, string tempDirectory)
        {
        }

        private NativeEnvironment _nativeEnvironment;
        public NativeEnvironment Environment
        {
            get { return _nativeEnvironment ?? (_nativeEnvironment = GetEnvironmentInfo()); }
        }

        public bool SupportsRunningAsService
        {
            get
            {
                return false;
            }
        }

        public bool IsRunningAsService
        {
            get
            {
                return false;
            }
        }

        public void ConfigureAutoRun(bool autorun)
        {
        }

        public INetworkManager CreateNetworkManager(ILogger logger)
        {
            return new NetworkManager(logger);
        }

		public IPowerManagement GetPowerManagement() 
		{
			return new NullPowerManagement ();
		}

        private NativeEnvironment GetEnvironmentInfo()
        {
            var info = new NativeEnvironment
            {
                OperatingSystem = Startup.Common.OperatingSystem.Linux
            };

            var uname = GetUnixName();

            var sysName = uname.sysname ?? string.Empty;

			info.OperatingSystem = Startup.Common.OperatingSystem.Osx;

            var archX86 = new Regex("(i|I)[3-6]86");

            if (archX86.IsMatch(uname.machine))
            {
                info.SystemArchitecture = Architecture.X86;
            }
            else if (string.Equals(uname.machine, "x86_64", StringComparison.OrdinalIgnoreCase))
            {
                info.SystemArchitecture = Architecture.X86_X64;
            }
            else if (uname.machine.StartsWith("arm", StringComparison.OrdinalIgnoreCase))
            {
                info.SystemArchitecture = Architecture.Arm;
            }

            info.OperatingSystemVersionString = string.IsNullOrWhiteSpace(sysName) ?
                System.Environment.OSVersion.VersionString :
                sysName;

            return info;
        }

        private Uname _unixName;
        private Uname GetUnixName()
        {
            if (_unixName == null)
            {
                var uname = new Uname();
                Utsname utsname;
                var callResult = Syscall.uname(out utsname);
                if (callResult == 0)
                {
                    uname.sysname = utsname.sysname;
                    uname.machine = utsname.machine;
                }

                _unixName = uname;
            }
            return _unixName;
        }

        private class Uname
        {
            public string sysname = string.Empty;
            public string machine = string.Empty;
        }

		private class NullPowerManagement : IPowerManagement
		{
			public void ScheduleWake(DateTime utcTime) 
			{
				throw new NotImplementedException ();
			}
		}
    }
}
