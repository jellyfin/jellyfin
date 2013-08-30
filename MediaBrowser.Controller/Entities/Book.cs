
namespace MediaBrowser.Controller.Entities
{
    public class Book : BaseItem
    {
        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Book;
            }
        }

        public string SeriesName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override string MetaLocation
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        protected override bool UseParentPathToCreateResolveArgs
        {
            get
            {
                return !IsInMixedFolder;
            }
        }
    }
}
