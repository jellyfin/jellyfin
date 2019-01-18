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
        public IDictionary<string, Dictionary<string, SwaggerMethod>> paths { get; set; }
        public Dictionary<string, SwaggerDefinition> definitions { get; set; }
        public SwaggerComponents components { get; set; }
    }

    public class SwaggerComponents
    {
        public Dictionary<string, SwaggerSecurityScheme> securitySchemes { get; set; }
    }

    public class SwaggerSecurityScheme
    {
        public string name { get; set; }
        public string type { get; set; }
        public string @in { get; set; }
    }

    public class SwaggerInfo
    {
        public string description { get; set; }
        public string version { get; set; }
        public string title { get; set; }
        public string termsOfService { get; set; }

        public SwaggerConcactInfo contact { get; set; }
    }

    public class SwaggerConcactInfo
    {
        public string email { get; set; }
        public string name { get; set; }
        public string url { get; set; }
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
        public Dictionary<string, string[]>[] security { get; set; }
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

    public class SwaggerService : IService, IRequiresRequest
    {
        private SwaggerSpec _spec;

        public IRequest Request { get; set; }

        public object Get(GetSwaggerSpec request)
        {
            return _spec ?? (_spec = GetSpec());
        }

        private SwaggerSpec GetSpec()
        {
            string host = null;
            Uri uri;
            if (Uri.TryCreate(Request.RawUrl, UriKind.Absolute, out uri))
            {
                host = uri.Host;
            }

            var securitySchemes = new Dictionary<string, SwaggerSecurityScheme>();

            securitySchemes["api_key"] = new SwaggerSecurityScheme
            {
                name = "api_key",
                type = "apiKey",
                @in = "query"
            };

            var spec = new SwaggerSpec
            {
                schemes = new[] { "http" },
                tags = GetTags(),
                swagger = "2.0",
                info = new SwaggerInfo
                {
                    title = "Emby Server API",
                    version = "1.0.0",
                    description = "Explore the Emby Server API",
                    contact = new SwaggerConcactInfo
                    {
                        name = "Emby Developer Community",
                        url = "https://emby.media/community/index.php?/forum/47-developer-api"
                    },
                    termsOfService = "https://emby.media/terms"
                },
                paths = GetPaths(),
                definitions = GetDefinitions(),
                basePath = "/emby",
                host = host,

                components = new SwaggerComponents
                {
                    securitySchemes = securitySchemes
                }
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

        private IDictionary<string, Dictionary<string, SwaggerMethod>> GetPaths()
        {
            var paths = new SortedDictionary<string, Dictionary<string, SwaggerMethod>>();

            var all = ServiceController.Instance.RestPathMap.OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var current in all)
            {
                foreach (var info in current.Value)
                {
                    if (info.IsHidden)
                    {
                        continue;
                    }

                    if (info.Path.StartsWith("/mediabrowser", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (info.Path.StartsWith("/emby", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

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
                var responses = new Dictionary<string, SwaggerResponse>
                {
                };

                responses["200"] = new SwaggerResponse
                {
                    description = "OK"
                };

                var security = new List<Dictionary<string, string[]>>();

                var apiKeySecurity = new Dictionary<string, string[]>();
                apiKeySecurity["api_key"] = Array.Empty<string>();

                security.Add(apiKeySecurity);

                result[verb.ToLower()] = new SwaggerMethod
                {
                    summary = info.Summary,
                    description = info.Description,
                    produces = new[]
                    {
                        "application/json"
                    },
                    consumes = new[]
                    {
                        "application/json"
                    },
                    operationId = info.RequestType.Name,
                    tags = Array.Empty<string>(),

                    parameters = new SwaggerParam[] { },

                    responses = responses,

                    security = security.ToArray()
                };
            }

            return result;
        }
    }
}
