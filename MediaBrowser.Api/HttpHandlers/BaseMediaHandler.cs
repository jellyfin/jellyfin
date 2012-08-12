using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public abstract class BaseMediaHandler<T> : BaseHandler
        where T : BaseItem, new()
    {
        private T _LibraryItem;
        /// <summary>
        /// Gets the library item that will be played, if any
        /// </summary>
        protected T LibraryItem
        {
            get
            {
                if (_LibraryItem == null)
                {
                    string id = QueryString["id"];

                    if (!string.IsNullOrEmpty(id))
                    {
                        _LibraryItem = Kernel.Instance.GetItemById(Guid.Parse(id)) as T;
                    }
                }

                return _LibraryItem;
            }
        }

        public int? AudioChannels
        {
            get
            {
                string val = QueryString["audiochannels"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        public int? AudioSampleRate
        {
            get
            {
                string val = QueryString["audiosamplerate"];

                if (string.IsNullOrEmpty(val))
                {
                    return 44100;
                }

                return int.Parse(val);
            }
        }

        public override string ContentType
        {
            get
            {
                return MimeTypes.GetMimeType("." + GetOutputFormat());
            }
        }

        public override bool CompressResponse
        {
            get
            {
                return false;
            }
        }

        public override void ProcessRequest(HttpListenerContext ctx)
        {
            HttpListenerContext = ctx;

            if (!RequiresConversion())
            {
                new StaticFileHandler() { Path = LibraryItem.Path }.ProcessRequest(ctx);
                return;
            }

            base.ProcessRequest(ctx);
        }

        protected abstract string GetCommandLineArguments();
        protected abstract string GetOutputFormat();
        protected abstract bool RequiresConversion();

        protected async override Task WriteResponseToOutputStream(Stream stream)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.CreateNoWindow = true;

            startInfo.UseShellExecute = false;

            // Must consume both or ffmpeg may hang due to deadlocks. See comments below.
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.FileName = ApiService.FFMpegPath;
            startInfo.WorkingDirectory = ApiService.FFMpegDirectory;
            startInfo.Arguments = GetCommandLineArguments();

            Logger.LogInfo(startInfo.FileName + " " + startInfo.Arguments);

            Process process = new Process();
            process.StartInfo = startInfo;

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            FileStream logStream = new FileStream(Path.Combine(ApplicationPaths.LogDirectoryPath, "ffmpeg-" + Guid.NewGuid().ToString() + ".txt"), FileMode.Create);

            try
            {
                process.Start();

                // MUST read both stdout and stderr asynchronously or a deadlock may occurr
                // If we ever decide to disable the ffmpeg log then you must uncomment the below line.
                //process.BeginErrorReadLine();

                Task debugLogTask = Task.Run(async () => { await process.StandardError.BaseStream.CopyToAsync(logStream); });

                await process.StandardOutput.BaseStream.CopyToAsync(stream);

                process.WaitForExit();

                Logger.LogInfo("FFMpeg exited with code " + process.ExitCode);

                await debugLogTask;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);

                // Hate having to do this
                try
                {
                    process.Kill();
                }
                catch
                {
                }
            }
            finally
            {
                logStream.Dispose();
                process.Dispose();
            }
        }
    }
}
