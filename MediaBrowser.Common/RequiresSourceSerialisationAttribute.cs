using System;

namespace MediaBrowser.Common;

/// <summary>
/// Marks a BaseItem as needing custom serialisation from the Data field of the db.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class RequiresSourceSerialisationAttribute : System.Attribute
{
}
