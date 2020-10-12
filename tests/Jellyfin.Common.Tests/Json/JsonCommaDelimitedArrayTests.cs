using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Common.Tests.Models;
using MediaBrowser.Model.Session;
using Xunit;

namespace Jellyfin.Common.Tests.Json
{
    public static class JsonCommaDelimitedArrayTests
    {
        [Fact]
        public static void Deserialize_String_Valid_Success()
        {
            var desiredValue = new GenericBodyModel<string>
            {
                Value = new[] { "a", "b", "c" }
            };

            var options = new JsonSerializerOptions();
            var value = JsonSerializer.Deserialize<GenericBodyModel<string>>(@"{ ""Value"": ""a,b,c"" }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public static void Deserialize_GenericCommandType_Valid_Success()
        {
            var desiredValue = new GenericBodyModel<GeneralCommandType>
            {
                Value = new[] { GeneralCommandType.MoveUp, GeneralCommandType.MoveDown }
            };

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            var value = JsonSerializer.Deserialize<GenericBodyModel<GeneralCommandType>>(@"{ ""Value"": ""MoveUp,MoveDown"" }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public static void Deserialize_String_Array_Valid_Success()
        {
            var desiredValue = new GenericBodyModel<string>
            {
                Value = new[] { "a", "b", "c" }
            };

            var options = new JsonSerializerOptions();
            var value = JsonSerializer.Deserialize<GenericBodyModel<string>>(@"{ ""Value"": [""a"",""b"",""c""] }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public static void Deserialize_GenericCommandType_Array_Valid_Success()
        {
            var desiredValue = new GenericBodyModel<GeneralCommandType>
            {
                Value = new[] { GeneralCommandType.MoveUp, GeneralCommandType.MoveDown }
            };

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            var value = JsonSerializer.Deserialize<GenericBodyModel<GeneralCommandType>>(@"{ ""Value"": [""MoveUp"", ""MoveDown""] }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }
    }
}