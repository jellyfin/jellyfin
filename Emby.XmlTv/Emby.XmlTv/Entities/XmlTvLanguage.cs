using System;

namespace Emby.XmlTv.Entities
{
    public class XmlTvLanguage
    {
        /// <summary>
        /// The name.
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// The relevance (number of occurances) of the language, can be used to order (desc)
        /// </summary>
        public Int32 Relevance { get; set; }
    }
}
