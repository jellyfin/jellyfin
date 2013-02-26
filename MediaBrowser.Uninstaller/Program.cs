using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Uninstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            var product = args.Length > 1 ? args[1] : "server";
            //copy the real program to a temp location so we can delete everything here (including us)
            var tempExe = Path.Combine(Path.GetTempPath(), "MediaBrowser.Uninstaller.Execute.exe");
            var tempConfig = Path.Combine(Path.GetTempPath(), "MediaBrowser.Uninstaller.Execute.exe.config");
            //using (var file = File.Create(tempExe, 4096, FileOptions.DeleteOnClose))
            {
                //copy the real uninstaller to temp location
                var sourceDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
                File.WriteAllBytes(tempExe, File.ReadAllBytes(Path.Combine(sourceDir, "MediaBrowser.Uninstaller.Execute.exe")));
                File.Copy(Path.Combine(sourceDir, "MediaBrowser.Uninstaller.Execute.exe.config"), tempConfig);
                //kick off the copy
                Process.Start(tempExe, product);
                //and shut down
            }
        }
    }
}
