using MediaBrowser.ClickOnce;
using System;
using System.IO;

namespace MediaBrowser.UI.Uninstall
{
    /// <summary>
    /// Class Program
    /// </summary>
    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The args.</param>
        static void Main(string[] args)
        {
            new ClickOnceHelper(Globals.PublisherName, Globals.ProductName, Globals.SuiteName).Uninstall();

            // Delete all files from publisher folder and folder itself on uninstall

            var publisherFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Globals.PublisherName);

            if (Directory.Exists(publisherFolder))
            {
                Directory.Delete(publisherFolder, true);
            }
        }
    }
}
