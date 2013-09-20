using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Server.Implementations;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Win32;

namespace MediaBrowser.ServerApplication
{
    public class MainStartup
    {
        /// <summary>
        /// The single instance mutex
        /// </summary>
        private static Mutex _singleInstanceMutex;

        private static IApplicationInterface _applicationInterface;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            bool createdNew;

            var runningPath = Process.GetCurrentProcess().MainModule.FileName.Replace(Path.DirectorySeparatorChar.ToString(), string.Empty);

            _singleInstanceMutex = new Mutex(true, @"Local\" + runningPath, out createdNew);

            if (!createdNew)
            {
                _singleInstanceMutex = null;
                return;
            }

            // Look for the existence of an update archive
            var appPaths = new ServerApplicationPaths();
            var updateArchive = Path.Combine(appPaths.TempUpdatePath, Constants.MbServerPkgName + ".zip");
            if (File.Exists(updateArchive))
            {
                // Update is there - execute update
                try
                {
                    new ApplicationUpdater().UpdateApplication(MBApplication.MBServer, appPaths, updateArchive);

                    // And just let the app exit so it can update
                    return;
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format("Error attempting to update application.\n\n{0}\n\n{1}", e.GetType().Name, e.Message));
                }
            }

            StartApplication();
        }

        private static void StartApplication()
        {
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            var commandLineArgs = Environment.GetCommandLineArgs();

            if (commandLineArgs.Length > 1 && commandLineArgs[1].Equals("-service"))
            {
                // Start application as a service
                StartBackgroundService();
            }
            else
            {
                StartWpfApp();
            }
        }

        static void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            // Try to shutdown gracefully
            if (_applicationInterface != null)
            {
                _applicationInterface.ShutdownApplication();
            }
        }

        private static void StartWpfApp()
        {
            var app = new App();

            _applicationInterface = app;

            app.Run();
        }

        private static void StartBackgroundService()
        {

        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;

            if (_applicationInterface != null)
            {
                _applicationInterface.OnUnhandledException(exception);
            }

            if (!Debugger.IsAttached)
            {
                Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(exception));
            }
        }

        /// <summary>
        /// Releases the mutex.
        /// </summary>
        internal static void ReleaseMutex()
        {
            if (_singleInstanceMutex == null)
            {
                return;
            }

            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Close();
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }
}
