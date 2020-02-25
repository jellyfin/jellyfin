#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Dlna
{
    public enum ProfileConditionType
    {
        Equals = 0,
        NotEquals = 1,
        LessThanEqual = 2,
        GreaterThanEqual = 3,
        EqualsAny = 4
    }
}
