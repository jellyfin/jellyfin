using System.Deployment.Application;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;

namespace MediaBrowser.ClickOnce
{
    /// <summary>
    /// Class ClickOnceHelper
    /// </summary>
    public class ClickOnceHelper
    {
        /// <summary>
        /// The uninstall string
        /// </summary>
        private const string UninstallString = "UninstallString";
        /// <summary>
        /// The display name key
        /// </summary>
        private const string DisplayNameKey = "DisplayName";
        /// <summary>
        /// The uninstall string file
        /// </summary>
        private const string UninstallStringFile = "UninstallString.bat";
        /// <summary>
        /// The appref extension
        /// </summary>
        private const string ApprefExtension = ".appref-ms";
        /// <summary>
        /// The uninstall registry key
        /// </summary>
        private readonly RegistryKey UninstallRegistryKey;

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <value>The location.</value>
        private static string Location
        {
            get { return Assembly.GetExecutingAssembly().Location; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is network deployed.
        /// </summary>
        /// <value><c>true</c> if this instance is network deployed; otherwise, <c>false</c>.</value>
        public static bool IsNetworkDeployed
        {
            get { return ApplicationDeployment.IsNetworkDeployed; }
        }

        /// <summary>
        /// Gets the name of the publisher.
        /// </summary>
        /// <value>The name of the publisher.</value>
        public string PublisherName { get; private set; }
        /// <summary>
        /// Gets the name of the product.
        /// </summary>
        /// <value>The name of the product.</value>
        public string ProductName { get; private set; }
        /// <summary>
        /// Gets the uninstall file.
        /// </summary>
        /// <value>The uninstall file.</value>
        public string UninstallFile { get; private set; }
        /// <summary>
        /// Gets the name of the suite.
        /// </summary>
        /// <value>The name of the suite.</value>
        public string SuiteName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickOnceHelper" /> class.
        /// </summary>
        /// <param name="publisherName">Name of the publisher.</param>
        /// <param name="productName">Name of the product.</param>
        /// <param name="suiteName">Name of the suite.</param>
        public ClickOnceHelper(string publisherName, string productName, string suiteName)
        {
            PublisherName = publisherName;
            ProductName = productName;
            SuiteName = suiteName;

            var publisherFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PublisherName);

            if (!Directory.Exists(publisherFolder))
            {
                Directory.CreateDirectory(publisherFolder);
            }

            UninstallFile = Path.Combine(publisherFolder, UninstallStringFile);

            UninstallRegistryKey = GetUninstallRegistryKeyByProductName(ProductName);
        }

        /// <summary>
        /// Gets the shortcut path.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetShortcutPath()
        {
            var allProgramsPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

            var shortcutPath = Path.Combine(allProgramsPath, PublisherName);

            if (!string.IsNullOrEmpty(SuiteName))
            {
                shortcutPath = Path.Combine(shortcutPath, SuiteName);
            }

            return Path.Combine(shortcutPath, ProductName) + ApprefExtension;
        }

        /// <summary>
        /// Gets the startup shortcut path.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetStartupShortcutPath()
        {
            var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            
            return Path.Combine(startupPath, ProductName) + ApprefExtension;
        }

        /// <summary>
        /// Adds the shortcut to startup.
        /// </summary>
        public void AddShortcutToStartup()
        {
            var startupPath = GetStartupShortcutPath();

            if (!File.Exists(startupPath))
            {
                File.Copy(GetShortcutPath(), startupPath);
            }
        }

        /// <summary>
        /// Removes the shortcut from startup.
        /// </summary>
        public void RemoveShortcutFromStartup()
        {
            var startupPath = GetStartupShortcutPath();

            if (File.Exists(startupPath))
            {
                File.Delete(startupPath);
            }
        }

        /// <summary>
        /// Updates the uninstall parameters.
        /// </summary>
        /// <param name="uninstallerFileName">Name of the uninstaller file.</param>
        public void UpdateUninstallParameters(string uninstallerFileName)
        {
            if (UninstallRegistryKey != null)
            {
                UpdateUninstallString(uninstallerFileName);
            }
        }

        /// <summary>
        /// Gets the name of the uninstall registry key by product.
        /// </summary>
        /// <param name="productName">Name of the product.</param>
        /// <returns>RegistryKey.</returns>
        private RegistryKey GetUninstallRegistryKeyByProductName(string productName)
        {
            var subKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");

            if (subKey == null)
            {
                return null;
            }

            return subKey.GetSubKeyNames()
                .Select(name => subKey.OpenSubKey(name, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.QueryValues | RegistryRights.ReadKey | RegistryRights.SetValue))
                .Where(application => application != null)
                .FirstOrDefault(application => application.GetValueNames().Where(appKey => appKey.Equals(DisplayNameKey)).Any(appKey => application.GetValue(appKey).Equals(productName)));
        }

        /// <summary>
        /// Updates the uninstall string.
        /// </summary>
        /// <param name="uninstallerFileName">Name of the uninstaller file.</param>
        private void UpdateUninstallString(string uninstallerFileName)
        {
            var uninstallString = (string)UninstallRegistryKey.GetValue(UninstallString);

            if (!string.IsNullOrEmpty(UninstallFile) && uninstallString.StartsWith("rundll32.exe", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(UninstallFile, uninstallString);
            }

            var str = String.Format("\"{0}\" uninstall", Path.Combine(Path.GetDirectoryName(Location), uninstallerFileName));

            UninstallRegistryKey.SetValue(UninstallString, str);
        }

        /// <summary>
        /// Uninstalls this instance.
        /// </summary>
        public void Uninstall()
        {
            RemoveShortcutFromStartup();

            var uninstallString = File.ReadAllText(UninstallFile);

            new Process
            {
                StartInfo =
                {
                    Arguments = uninstallString.Substring(uninstallString.IndexOf(" ", StringComparison.OrdinalIgnoreCase) + 1),
                    FileName = uninstallString.Substring(0, uninstallString.IndexOf(" ", StringComparison.OrdinalIgnoreCase)),
                    UseShellExecute = false
                }

            }.Start();
        }

        /// <summary>
        /// Configures the click once startup.
        /// </summary>
        /// <param name="publisherName">Name of the publisher.</param>
        /// <param name="productName">Name of the product.</param>
        /// <param name="suiteName">Name of the suite.</param>
        /// <param name="runAtStartup">if set to <c>true</c> [run at startup].</param>
        /// <param name="uninstallerFilename">The uninstaller filename.</param>
        public static void ConfigureClickOnceStartupIfInstalled(string publisherName, string productName, string suiteName, bool runAtStartup, string uninstallerFilename)
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                return;
            }

            var clickOnceHelper = new ClickOnceHelper(publisherName, productName, suiteName);

            if (runAtStartup)
            {
                clickOnceHelper.UpdateUninstallParameters(uninstallerFilename);
                clickOnceHelper.AddShortcutToStartup();
            }
            else
            {
                clickOnceHelper.RemoveShortcutFromStartup();
            }
        }
    }
}
