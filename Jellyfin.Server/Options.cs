using System;
using System.CommandLine;
using System.Reflection;

namespace Jellyfin.Server
{
    /// <summary>
    /// Base class used to parse command line arguments.
    /// </summary>
    public class Options
    {
#pragma warning disable CA1200
        /// <summary>
        /// Initializes a new instance of the <see cref="Options"/> class.
        /// </summary>
        /// <description>
        /// To be called with the result of calling <see cref="o:Command.Parse"/> on the <see cref="RootCommand"/> given to the <see cref="StartupOptions.Setup"/> method.
        /// </description>
        /// <param name="parseResult">Instance of the <see cref="ParseResult"/> interface.</param>
#pragma warning restore CA1200
        public Options(ParseResult parseResult)
        {
            ParseResult = parseResult;
        }

        /// <summary>
        /// Gets or sets the parse result.
        /// </summary>
        protected ParseResult ParseResult { get; set; }

        /// <summary>
        /// Generic function to setup options from class fields.
        /// </summary>
        /// <param name="cmd">The <see cref="RootCommand"/> or <see cref="Command"/> to add the arguments to.</param>
        /// <param name="type">The <see cref="Type"/> of the class to read the fields from.</param>
        protected static void Setup(Command cmd, Type type)
        {
            foreach (FieldInfo prop in type.GetFields(BindingFlags.NonPublic | BindingFlags.Static))
            {
                cmd.Options.Add((Option)prop.GetValue(null)!);
            }
        }
    }
}
