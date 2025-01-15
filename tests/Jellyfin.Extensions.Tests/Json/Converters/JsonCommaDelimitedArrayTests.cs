using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Extensions.Tests.Json.Models;
using MediaBrowser.Model.Session;
using Xunit;

namespace Jellyfin.Extensions.Tests.Json.Converters
{
    public class JsonCommaDelimitedArrayTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        [Fact]
        public void Deserialize_String_Null_Success()
        {
            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<string>>(@"{ ""Value"": null }", _jsonOptions);
            Assert.Null(value?.Value);
        }

        [Fact]
        public void Deserialize_Empty_Success()
        {
            var desiredValue = new GenericBodyArrayModel<string>
            {
                Value = Array.Empty<string>()
            };

            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<string>>(@"{ ""Value"": """" }", _jsonOptions);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public void Deserialize_String_Valid_Success()
        {
            var desiredValue = new GenericBodyArrayModel<string>
            {
                Value = ["a", "b", "c"]
            };

            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<string>>(@"{ ""Value"": ""a,b,c"" }", _jsonOptions);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public void Deserialize_String_Space_Valid_Success()
        {
            var desiredValue = new GenericBodyArrayModel<string>
            {
                Value = ["a", "b", "c"]
            };

            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<string>>(@"{ ""Value"": ""a, b, c"" }", _jsonOptions);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public void Deserialize_GenericCommandType_Valid_Success()
        {
            var desiredValue = new GenericBodyArrayModel<GeneralCommandType>
            {
                Value = [GeneralCommandType.MoveUp, GeneralCommandType.MoveDown]
            };

            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<GeneralCommandType>>(@"{ ""Value"": ""MoveUp,MoveDown"" }", _jsonOptions);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public void Deserialize_GenericCommandType_EmptyEntry_Success()
        {
            var desiredValue = new GenericBodyArrayModel<GeneralCommandType>
            {
                Value = [GeneralCommandType.MoveUp, GeneralCommandType.MoveDown]
            };

            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<GeneralCommandType>>(@"{ ""Value"": ""MoveUp,,MoveDown"" }", _jsonOptions);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public void Deserialize_GenericCommandType_Invalid_Success()
        {
            var desiredValue = new GenericBodyArrayModel<GeneralCommandType>
            {
                Value = [GeneralCommandType.MoveUp, GeneralCommandType.MoveDown]
            };

            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<GeneralCommandType>>(@"{ ""Value"": ""MoveUp,TotallyNotAValidCommand,MoveDown"" }", _jsonOptions);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public void Deserialize_GenericCommandType_Space_Valid_Success()
        {
            var desiredValue = new GenericBodyArrayModel<GeneralCommandType>
            {
                Value = [GeneralCommandType.MoveUp, GeneralCommandType.MoveDown]
            };

            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<GeneralCommandType>>(@"{ ""Value"": ""MoveUp, MoveDown"" }", _jsonOptions);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public void Deserialize_String_Array_Valid_Success()
        {
            var desiredValue = new GenericBodyArrayModel<string>
            {
                Value = ["a", "b", "c"]
            };

            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<string>>(@"{ ""Value"": [""a"",""b"",""c""] }", _jsonOptions);
            Assert.Equal(desiredValue.Value, value?.Value);
        }

        [Fact]
        public void Deserialize_GenericCommandType_Array_Valid_Success()
        {
            var desiredValue = new GenericBodyArrayModel<GeneralCommandType>
            {
                Value = [GeneralCommandType.MoveUp, GeneralCommandType.MoveDown]
            };

            var value = JsonSerializer.Deserialize<GenericBodyArrayModel<GeneralCommandType>>(@"{ ""Value"": [""MoveUp"", ""MoveDown""] }", _jsonOptions);
            Assert.Equal(desiredValue.Value, value?.Value);
        }
    }
}
