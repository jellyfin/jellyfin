using System.IO;

namespace SharpCifs.Util.Sharpen
{
    public class Properties
    {
        protected Hashtable _properties;

        public Properties()
        {
            _properties = new Hashtable();
        }

        public Properties(Properties defaultProp): this()
        {
            PutAll(defaultProp._properties);
        }

        public void PutAll(Hashtable properties)
        {
            foreach (var key in properties.Keys)
            {
                //_properties.Add(key, properties[key]);
                _properties.Put(key, properties[key]);
            }
        }

        public void SetProperty(object key, object value)
        {
            //_properties.Add(key, value);
            _properties.Put(key, value);
        }

        public object GetProperty(object key)
        {
            return _properties.Keys.Contains(key) ? _properties[key] : null;
        }

        public object GetProperty(object key, object def)
        {
            /*if (_properties.ContainsKey(key))
            {
                return _properties[key];
            }
            return def;*/
            object value = _properties.Get(key);

            return value ?? def;            
        }

        public void Load(InputStream input)
        {
            StreamReader sr = new StreamReader(input);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                if (!string.IsNullOrEmpty(line))
                {
                    string[] tokens = line.Split('=');
                    //_properties.Add(tokens[0], tokens[1]);
                    _properties.Put(tokens[0], tokens[1]);
                }
            }
        }

        public void Store(OutputStream output)
        {
            StreamWriter sw = new StreamWriter(output);
            foreach (var key in _properties.Keys)
            {
                string line = string.Format("{0}={1}", key, _properties[key]);
                sw.WriteLine(line);
            }
        }

        public void Store(TextWriter output)
        {            
            foreach (var key in _properties.Keys)
            {
                string line = string.Format("{0}={1}", key, _properties[key]);
                output.WriteLine(line);
            }
        }
    }
}
