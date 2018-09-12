using System.Text.RegularExpressions;
using System;

namespace Emby.Naming.Common
{
    public class EpisodeExpression
    {
        private string _expression;
        public string Expression { get { return _expression; } set { _expression = value; _regex = null; } }

        public bool IsByDate { get; set; }
        public bool IsOptimistic { get; set; }
        public bool IsNamed { get; set; }
        public bool SupportsAbsoluteEpisodeNumbers { get; set; }

        public string[] DateTimeFormats { get; set; }

        private Regex _regex;
        public Regex Regex
        {
            get
            {
                return _regex ?? (_regex = new Regex(Expression, RegexOptions.IgnoreCase | RegexOptions.Compiled));
            }
        }

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

        public EpisodeExpression()
            : this(null)
        {
        }
    }
}
