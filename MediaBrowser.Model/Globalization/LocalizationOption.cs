#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Globalization
{
    public class LocalizationOption
    {
        public LocalizationOption(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }
}
