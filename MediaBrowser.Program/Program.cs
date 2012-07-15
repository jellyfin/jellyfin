using System;
using System.Configuration;
using System.IO;
using MediaBrowser.Controller;

namespace MediaBrowser.Program
{
    class Program
    {
        static void Main(string[] args)
        {
            LoadKernel();
        }

        private static void LoadKernel()
        {
            DateTime now = DateTime.Now;

            Console.WriteLine("Loading");

            string installDir = ConfigurationManager.AppSettings["DataPath"];

            if (!Path.IsPathRooted(installDir))
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                path = Path.GetDirectoryName(path);

                installDir = Path.Combine(path, installDir);

                installDir = Path.GetFullPath(installDir);
            }

            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
            }

            Kernel kernel = new Kernel(installDir);

            kernel.Init();

            var time = DateTime.Now - now;
            Console.WriteLine("Done in " + time.TotalSeconds + " seconds");
            
            Console.WriteLine("Press Enter to quit.");
            Console.ReadLine();
        }
    }
}
