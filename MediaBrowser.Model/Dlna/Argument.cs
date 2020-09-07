#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    public class Argument
    {
        public string Name { get; set; } = string.Empty;

        public string Direction { get; set; } = string.Empty;

        public string RelatedStateVariable { get; set; } = string.Empty;
    }
}
