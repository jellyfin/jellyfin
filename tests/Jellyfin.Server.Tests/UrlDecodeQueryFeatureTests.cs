using System.Collections.Generic;
using System.Linq;
using Jellyfin.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Jellyfin.Server.Tests
{
    public static class UrlDecodeQueryFeatureTests
    {
        public static TheoryData<string, string> EmptyValueTest_TestData()
        {
            var data = new TheoryData<string, string>();
            data.Add("e0a72cb2a2c7", "e0a72cb2a2c7"); // isn't encoded
            data.Add("random+test", "random test"); // encoded
            data.Add("random%20test", "random test"); // encoded
            data.Add("++", "  "); // encoded
            return data;
        }

        [Theory]
        [MemberData(nameof(EmptyValueTest_TestData))]
        public static void EmptyValueTest(string query, string key)
        {
            var dict = new Dictionary<string, StringValues>
            {
                { query, StringValues.Empty }
            };
            var test = new UrlDecodeQueryFeature(new QueryFeature(new QueryCollection(dict)));
            Assert.Single(test.Query);
            var (k, v) = test.Query.First();
            Assert.Equal(key, k);
            Assert.Empty(v);
        }
    }
}
