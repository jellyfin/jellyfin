namespace SharpCifs.Util.Sharpen
{
    public class StringTokenizer
    {
        private string[] _tokens;
        private int _pos;

        public StringTokenizer(string text, string delim)
        {
            _tokens = text.Split(delim);
        }

        public int CountTokens()
        {
            return _tokens.Length;
        }

        public string NextToken()
        {
            string value = _tokens[_pos];

            _pos++;

            return value;
        }

        public bool HasMoreTokens()
        {
            return _pos < _tokens.Length;
        }
    }
}
