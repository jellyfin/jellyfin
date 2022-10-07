#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.Model.Session;

public class GeneralCommand
{
    public GeneralCommand()
        : this(new Dictionary<string, string>())
    {
    }

    [JsonConstructor]
    public GeneralCommand(Dictionary<string, string>? arguments)
    {
        Arguments = arguments ?? new Dictionary<string, string>();
    }

    public GeneralCommandType Name { get; set; }

    public Guid ControllingUserId { get; set; }

    public Dictionary<string, string> Arguments { get; }
}
