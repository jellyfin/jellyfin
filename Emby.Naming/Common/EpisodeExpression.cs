#pragma warning disable CS1591

using System;
using System.Text.RegularExpressions;

namespace Emby.Naming.Common
{
    public class EpisodeExpression
    {
        private string _expression;
        private Regex _regex;

        public EpisodeExpression(string expression, bool byDate)
        {
            Expression = expression;
            IsByDate = byDate;
            DateTimeFormats = Array.Empty<string>();
            SupportsAbsoluteEpisodeNumbers = true;
        }

        public EpisodeExpression(string expression)
            : this(expression, false)
        {
        }

        public string Expression
        {
            get => _expression;
            set
            {
                _expression = value;
                _regex = null;
            }
        }

        public bool IsByDate { get; set; }

        public bool IsOptimistic { get; set; }

        public bool IsNamed { get; set; }

        public bool SupportsAbsoluteEpisodeNumbers { get; set; }

        public string[] DateTimeFormats { get; set; }

        public Regex Regex => _regex ??= new Regex(Expression, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
