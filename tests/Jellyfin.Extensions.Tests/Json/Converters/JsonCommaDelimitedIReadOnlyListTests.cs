using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Extensions.Tests.Json.Models;
using MediaBrowser.Model.Session;
using Xunit;

namespace Jellyfin.Extensions.Tests.Json.Converters
{
    public static class JsonCommaDelimitedIReadOnlyListTests
    {
        [Fact]
        public static void Deserialize_String_Valid_Success()
        {
            var desiredValue = new GenericBodyIReadOnlyListModel<string>
            {
                Value = new[] { "a", "b", "c" }
            };

            var options = new JsonSerializerOptions();
            var value = JsonSerializer.Deserialize<GenericBodyIReadOnlyListModel<string>>(@"{ ""Value"": ""a,b,c"" }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public static void Deserialize_String_Space_Valid_Success()
        {
            var desiredValue = new GenericBodyIReadOnlyListModel<string>
            {
                Value = new[] { "a", "b", "c" }
            };

            var options = new JsonSerializerOptions();
            var value = JsonSerializer.Deserialize<GenericBodyIReadOnlyListModel<string>>(@"{ ""Value"": ""a, b, c"" }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public static void Deserialize_GenericCommandType_Valid_Success()
        {
            var desiredValue = new GenericBodyIReadOnlyListModel<GeneralCommandType>
            {
                Value = new[] { GeneralCommandType.MoveUp, GeneralCommandType.MoveDown }
            };

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            var value = JsonSerializer.Deserialize<GenericBodyIReadOnlyListModel<GeneralCommandType>>(@"{ ""Value"": ""MoveUp,MoveDown"" }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public static void Deserialize_GenericCommandType_Space_Valid_Success()
        {
            var desiredValue = new GenericBodyIReadOnlyListModel<GeneralCommandType>
            {
                Value = new[] { GeneralCommandType.MoveUp, GeneralCommandType.MoveDown }
            };

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            var value = JsonSerializer.Deserialize<GenericBodyIReadOnlyListModel<GeneralCommandType>>(@"{ ""Value"": ""MoveUp, MoveDown"" }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public static void Deserialize_String_Array_Valid_Success()
        {
            var desiredValue = new GenericBodyIReadOnlyListModel<string>
            {
                Value = new[] { "a", "b", "c" }
            };

            var options = new JsonSerializerOptions();
            var value = JsonSerializer.Deserialize<GenericBodyIReadOnlyListModel<string>>(@"{ ""Value"": [""a"",""b"",""c""] }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public static void Deserialize_GenericCommandType_Array_Valid_Success()
        {
            var desiredValue = new GenericBodyIReadOnlyListModel<GeneralCommandType>
            {
                Value = new[] { GeneralCommandType.MoveUp, GeneralCommandType.MoveDown }
            };

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            var value = JsonSerializer.Deserialize<GenericBodyIReadOnlyListModel<GeneralCommandType>>(@"{ ""Value"": [""MoveUp"", ""MoveDown""] }", options);
            Assert.Equal(desiredValue.Value, value?.Value);
        }
    }
}
