using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MediaBrowser.Common.Json
{
    public class JsonSerializer
    {
        public static void Serialize<T>(T o, Stream stream)
        {
            using (StreamWriter streamWriter = new StreamWriter(stream))
            {
                using (Newtonsoft.Json.JsonTextWriter writer = new Newtonsoft.Json.JsonTextWriter(streamWriter))
                {
                    var settings = new Newtonsoft.Json.JsonSerializerSettings()
                    {
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                    };

                    Newtonsoft.Json.JsonSerializer.Create(settings).Serialize(writer, o);
                }
            }
        }

        public static void Serialize<T>(T o, string file)
        {
            using (StreamWriter streamWriter = new StreamWriter(file))
            {
                using (Newtonsoft.Json.JsonTextWriter writer = new Newtonsoft.Json.JsonTextWriter(streamWriter))
                {
                    var settings = new Newtonsoft.Json.JsonSerializerSettings()
                    {
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                    };

                    Newtonsoft.Json.JsonSerializer.Create(settings).Serialize(writer, o);
                }
            }
        }

        public static T Deserialize<T>(string file)
        {
            using (StreamReader streamReader = new StreamReader(file))
            {
                using (Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(streamReader))
                {
                    return Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings() { }).Deserialize<T>(reader);
                }
            }
        }
    }
}
