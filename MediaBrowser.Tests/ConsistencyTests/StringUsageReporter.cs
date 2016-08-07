using MediaBrowser.Tests.ConsistencyTests.TextIndexing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MediaBrowser.Tests.ConsistencyTests
{
    /// <summary>
    /// This class contains tests for reporting the usage of localization string tokens
    /// in the dashboard-ui or similar.
    /// </summary>
    /// <remarks>
    /// <para>Run one of the two tests using Visual Studio's "Test Explorer":</para>
    /// <para>
    /// <list type="bullet">
    /// <item><see cref="ReportStringUsage"/></item>
    /// <item><see cref="ReportUnusedStrings"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// On successful run, the bottom section of the test explorer will contain a link "Output".
    /// This link will open the test results, displaying the trace and two attachment links.
    /// One link will open the output folder, the other link will open the output xml file.
    /// </para>
    /// <para>
    /// The output xml file contains a stylesheet link to render the results as html.
    /// How that works depends on the default application configured for XML files:
    /// </para>
    /// <para><list type="bullet">
    /// <item><term>Visual Studio</term>
    /// <description>Will open in XML source view. To view the html result, click menu
    /// 'XML' => 'Start XSLT without debugging'</description></item>
    /// <item><term>Internet Explorer</term>
    /// <description>XSL transform will be applied automatically.</description></item>
    /// <item><term>Firefox</term>
    /// <description>XSL transform will be applied automatically.</description></item>
    /// <item><term>Chrome</term>
    /// <description>Does not work. Chrome is unable/unwilling to apply xslt transforms from local files.</description></item>
    /// </list></para>
    /// </remarks>
    [TestClass]
    public class StringUsageReporter
    {
        /// <summary>
        /// Root path of the web application
        /// </summary>
        /// <remarks>
        /// Can be an absolute path or a path relative to the binaries folder (bin\Debug).
        /// </remarks>
        public const string WebFolder = @"..\..\..\MediaBrowser.WebDashboard\dashboard-ui";

        /// <summary>
        /// Path to the strings file, relative to <see cref="WebFolder"/>.
        /// </summary>
        public const string StringsFile = @"strings\en-US.json";

        /// <summary>
        /// Path to the output folder
        /// </summary>
        /// <remarks>
        /// Can be an absolute path or a path relative to the binaries folder (bin\Debug).
        /// Important: When changing the output path, make sure that "StringCheck.xslt" is present
        /// to make the XML transform work.
        /// </remarks>
        public const string OutputPath = @".";

        /// <summary>
        /// List of file extension to search.
        /// </summary>
        public static string[] TargetExtensions = new[] { ".js", ".html" };

        /// <summary>
        /// List of paths to exclude from search.
        /// </summary>
        public static string[] ExcludePaths = new[] { @"\bower_components\", @"\thirdparty\" };

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        //[TestMethod]
        //public void ReportStringUsage()
        //{
        //    this.CheckDashboardStrings(false);
        //}

        [TestMethod]
        public void ReportUnusedStrings()
        {
            this.CheckDashboardStrings(true);
        }

        private void CheckDashboardStrings(Boolean unusedOnly)
        {
            // Init Folders
            var currentDir = System.IO.Directory.GetCurrentDirectory();
            Trace("CurrentDir: {0}", currentDir);

            var rootFolderInfo = ResolveFolder(currentDir, WebFolder);
            Trace("Web Root: {0}", rootFolderInfo.FullName);

            var outputFolderInfo = ResolveFolder(currentDir, OutputPath);
            Trace("Output Path: {0}", outputFolderInfo.FullName);

            // Load Strings
            var stringsFileName = Path.Combine(rootFolderInfo.FullName, StringsFile);

            if (!File.Exists(stringsFileName))
            {
                throw new Exception(string.Format("Strings file not found: {0}", stringsFileName));
            }

            int lineNumbers;
            var stringsDic = this.CreateStringsDictionary(new FileInfo(stringsFileName), out lineNumbers);

            Trace("Loaded {0} strings from strings file containing {1} lines", stringsDic.Count, lineNumbers);

            var allFiles = rootFolderInfo.GetFiles("*", SearchOption.AllDirectories);

            var filteredFiles1 = allFiles.Where(f => TargetExtensions.Any(e => string.Equals(e, f.Extension, StringComparison.OrdinalIgnoreCase)));
            var filteredFiles2 = filteredFiles1.Where(f => !ExcludePaths.Any(p => f.FullName.Contains(p)));

            var selectedFiles = filteredFiles2.OrderBy(f => f.FullName).ToList();

            var wordIndex = IndexBuilder.BuildIndexFromFiles(selectedFiles, rootFolderInfo.FullName);

            Trace("Created word index from {0} files containing {1} individual words", selectedFiles.Count, wordIndex.Keys.Count);

            var outputFileName = Path.Combine(outputFolderInfo.FullName, string.Format("StringCheck_{0:yyyyMMddHHmmss}.xml", DateTime.Now));
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                WriteEndDocumentOnClose = true
            };

            Trace("Output file: {0}", outputFileName);

            using (XmlWriter writer = XmlWriter.Create(outputFileName, settings))
            {
                writer.WriteStartDocument(true);

                // Write the Processing Instruction node.
                string xslText = "type=\"text/xsl\" href=\"StringCheck.xslt\"";
                writer.WriteProcessingInstruction("xml-stylesheet", xslText);

                writer.WriteStartElement("StringUsages");
                writer.WriteAttributeString("ReportTitle", unusedOnly ? "Unused Strings Report" : "String Usage Report");
                writer.WriteAttributeString("Mode", unusedOnly ? "UnusedOnly" : "All");

                foreach (var kvp in stringsDic)
                {
                    var occurences = wordIndex.Find(kvp.Key);

                    if (occurences == null || !unusedOnly)
                    {
                        ////Trace("{0}: {1}", kvp.Key, kvp.Value);
                        writer.WriteStartElement("Dictionary");
                        writer.WriteAttributeString("Token", kvp.Key);
                        writer.WriteAttributeString("Text", kvp.Value);

                        if (occurences != null && !unusedOnly)
                        {
                            foreach (var occurence in occurences)
                            {
                                writer.WriteStartElement("Occurence");
                                writer.WriteAttributeString("FileName", occurence.FileName);
                                writer.WriteAttributeString("FullPath", occurence.FullPath);
                                writer.WriteAttributeString("LineNumber", occurence.LineNumber.ToString());
                                writer.WriteEndElement();
                                ////Trace("    {0}:{1}", occurence.FileName, occurence.LineNumber);
                            }
                        }

                        writer.WriteEndElement();
                    }
                }
            }

            TestContext.AddResultFile(outputFileName);
            TestContext.AddResultFile(outputFolderInfo.FullName);
        }

        private SortedDictionary<string, string> CreateStringsDictionary(FileInfo file, out int lineNumbers)
        {
            var dic = new SortedDictionary<string, string>();
            lineNumbers = 0;

            using (var reader = file.OpenText())
            {
                while (!reader.EndOfStream)
                {
                    lineNumbers++;
                    var words = reader
                         .ReadLine()
                         .Split(new[] { "\":" }, StringSplitOptions.RemoveEmptyEntries);


                    if (words.Length == 2)
                    {
                        var token = words[0].Replace("\"", string.Empty).Trim();
                        var text = words[1].Replace("\",", string.Empty).Replace("\"", string.Empty).Trim();

                        if (dic.Keys.Contains(token))
                        {
                            throw new Exception(string.Format("Double string entry found: {0}", token));
                        }

                        dic.Add(token, text);
                    }
                }
            }

            return dic;
        }

        private DirectoryInfo ResolveFolder(string currentDir, string folderPath)
        {
            if (folderPath.IndexOf(@"\:") != 1)
            {
                folderPath = Path.Combine(currentDir, folderPath);
            }

            var folderInfo = new DirectoryInfo(folderPath);

            if (!folderInfo.Exists)
            {
                throw new Exception(string.Format("Folder not found: {0}", folderInfo.FullName));
            }

            return folderInfo;
        }


        private void Trace(string message, params object[] parameters)
        {
            var formatted = string.Format(message, parameters);
            System.Diagnostics.Trace.WriteLine(formatted);
        }
    }
}
