using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.Services
{
    [Route("/swagger", "GET", Summary = "Gets the swagger specifications")]
    [Route("/swagger.json", "GET", Summary = "Gets the swagger specifications")]
    public class GetSwaggerSpec : IReturn<SwaggerSpec>
    {
    }

    public class SwaggerSpec
    {
        public string swagger { get; set; }
        public string[] schemes { get; set; }
        public SwaggerInfo info { get; set; }
        public string host { get; set; }
        public string basePath { get; set; }
        public SwaggerTag[] tags { get; set; }
        public Dictionary<string, Dictionary<string, SwaggerMethod>> paths { get; set; }
        public Dictionary<string, SwaggerDefinition> definitions { get; set; }
    }

    public class SwaggerInfo
    {
        public string description { get; set; }
        public string version { get; set; }
        public string title { get; set; }

        public SwaggerConcactInfo contact { get; set; }
    }

    public class SwaggerConcactInfo
    {
        public string email { get; set; }
    }

    public class SwaggerTag
    {
        public string description { get; set; }
        public string name { get; set; }
    }

    public class SwaggerMethod
    {
        public string summary { get; set; }
        public string description { get; set; }
        public string[] tags { get; set; }
        public string operationId { get; set; }
        public string[] consumes { get; set; }
        public string[] produces { get; set; }
        public SwaggerParam[] parameters { get; set; }
        public Dictionary<string, SwaggerResponse> responses { get; set; }
    }

    public class SwaggerParam
    {
        public string @in { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public bool required { get; set; }
        public string type { get; set; }
        public string collectionFormat { get; set; }
    }

    public class SwaggerResponse
    {
        public string description { get; set; }

        // ex. "$ref":"#/definitions/Pet"
        public Dictionary<string, string> schema { get; set; }
    }

    public class SwaggerDefinition
    {
        public string type { get; set; }
        public Dictionary<string, SwaggerProperty> properties { get; set; }
    }

    public class SwaggerProperty
    {
        public string type { get; set; }
        public string format { get; set; }
        public string description { get; set; }
        public string[] @enum { get; set; }
        public string @default { get; set; }
    }

    public class SwaggerService : IService
    {
        private SwaggerSpec _spec;

        public object Get(GetSwaggerSpec request)
        {
            return _spec ?? (_spec = GetSpec());
        }

        private SwaggerSpec GetSpec()
        {
            var spec = new SwaggerSpec
            {
                schemes = new[] { "http" },
                tags = GetTags(),
                swagger = "2.0",
                info = new SwaggerInfo
                {
                    title = "Emby Server API",
                    version = "1",
                    description = "Explore the Emby Server API",
                    contact = new SwaggerConcactInfo
                    {
                        email = "api@emby.media"
                    }
                },
                paths = GetPaths(),
                definitions = GetDefinitions()
            };

            return spec;
        }


        private SwaggerTag[] GetTags()
        {
            return new SwaggerTag[] { };
        }

        private Dictionary<string, SwaggerDefinition> GetDefinitions()
        {
            return new Dictionary<string, SwaggerDefinition>();
        }

        private Dictionary<string, Dictionary<string, SwaggerMethod>> GetPaths()
        {
            var paths = new Dictionary<string, Dictionary<string, SwaggerMethod>>();

            var all = ServiceController.Instance.RestPathMap.ToList();

            foreach (var current in all)
            {
                foreach (var info in current.Value)
                {
                    paths[info.Path] = GetPathInfo(info);
                }
            }

            return paths;
        }

        private Dictionary<string, SwaggerMethod> GetPathInfo(RestPath info)
        {
            var result = new Dictionary<string, SwaggerMethod>();
            
            foreach (var verb in info.Verbs)
            {
                result[verb] = new SwaggerMethod
                {
                    summary = info.Summary,
                    produces = new[]
                    {
                        "application/json",
                        "application/xml"
                    },
                    consumes = new[]
                    {
                        "application/json",
                        "application/xml"
                    },
                    operationId = info.RequestType.Name,
                    tags = new string[] { },

                    parameters = new SwaggerParam[] { }
                };
            }

            return result;
        }
    }
}
