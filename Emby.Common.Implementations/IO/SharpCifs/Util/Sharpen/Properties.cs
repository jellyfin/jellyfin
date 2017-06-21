using System.IO;

namespace SharpCifs.Util.Sharpen
{
    public class Properties
    {
        protected Hashtable _properties;


        public Properties()
        {
            this._properties = new Hashtable();
        }

        public Properties(Properties defaultProp) : this()
        {
            this.PutAll(defaultProp._properties);
        }

        public void PutAll(Hashtable properties)
        {
            foreach (var key in properties.Keys)
            {
                this._properties.Put(key, properties[key]);
            }
        }

        public void SetProperty(object key, object value)
        {
            this._properties.Put(key, value);
        }

        public object GetProperty(object key)
        {
            return this._properties.Keys.Contains(key) 
                ? this._properties[key] 
                : null;
        }

        public object GetProperty(object key, object def)
        {
            return this._properties.Get(key) ?? def;
        }

        public void Load(InputStream input)
        {
            using (var reader = new StreamReader(input))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                        continue;

                    var tokens = line.Split('=');

                    if (tokens.Length < 2)
                        continue;

                    this._properties.Put(tokens[0], tokens[1]);
                }
            }
        }

        public void Store(OutputStream output)
        {
            using (var writer = new StreamWriter(output))
            {
                foreach (var pair in this._properties)
                    writer.WriteLine($"{pair.Key}={pair.Value}");
            }
        }

        public void Store(TextWriter output)
        {
            foreach (var pair in this._properties)
                output.WriteLine($"{pair.Key}={pair.Value}");
        }
    }
}
