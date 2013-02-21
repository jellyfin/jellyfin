using MediaBrowser.Common.Mef;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.Common.Plugins
{
    public abstract class BaseTheme : BasePlugin
    {
        public sealed override bool DownloadToUi
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the MEF CompositionContainer
        /// </summary>
        private CompositionContainer CompositionContainer { get; set; }

        /// <summary>
        /// Gets the list of global resources
        /// </summary>
        [ImportMany(typeof(ResourceDictionary))]
        public IEnumerable<ResourceDictionary> GlobalResources { get; private set; }

        /// <summary>
        /// Gets the list of pages
        /// </summary>
        [ImportMany(typeof(Page))]
        public IEnumerable<Page> Pages { get; private set; }

        /// <summary>
        /// Gets the pack Uri of the Login page
        /// </summary>
        public abstract Uri LoginPageUri { get; }

        protected override void InitializeInUi()
        {
            base.InitializeInUi();

            ComposeParts();
        }

        private void ComposeParts()
        {
            var catalog = new AssemblyCatalog(GetType().Assembly);

            CompositionContainer = MefUtils.GetSafeCompositionContainer(new ComposablePartCatalog[] { catalog });

            CompositionContainer.ComposeParts(this);

            CompositionContainer.Catalog.Dispose();
        }

        protected override void DisposeInUi()
        {
            base.DisposeInUi();

            CompositionContainer.Dispose();
        }

        protected Uri GeneratePackUri(string relativePath)
        {
            string assemblyName = GetType().Assembly.GetName().Name;

            string uri = string.Format("pack://application:,,,/{0};component/{1}", assemblyName, relativePath);

            return new Uri(uri, UriKind.Absolute);
        }
    }
}
