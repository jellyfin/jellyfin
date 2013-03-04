using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
    [ProtoContract]
    public class ImageDownloadOptions
    {
        /// <summary>
        /// Download Art Image
        /// </summary>
        [ProtoMember(1)]
        public bool Art { get; set; }

        /// <summary>
        /// Download Logo Image
        /// </summary>
        [ProtoMember(2)]
        public bool Logo { get; set; }

        /// <summary>
        /// Download Primary Image
        /// </summary>
        [ProtoMember(3)]
        public bool Primary { get; set; }

        /// <summary>
        /// Download Backdrop Images
        /// </summary>
        [ProtoMember(4)]
        public bool Backdrops { get; set; }

        /// <summary>
        /// Download Disc Image
        /// </summary>
        [ProtoMember(5)]
        public bool Disc { get; set; }

        /// <summary>
        /// Download Thumb Image
        /// </summary>
        [ProtoMember(6)]
        public bool Thumb { get; set; }

        /// <summary>
        /// Download Banner Image
        /// </summary>
        [ProtoMember(7)]
        public bool Banner { get; set; }

    }
}
