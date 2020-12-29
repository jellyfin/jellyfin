using System;
using System.Globalization;
using System.Text.Json;
using MediaBrowser.Common.Json.Converters;
using Xunit;

namespace Jellyfin.Common.Tests.Json
{
    public class JsonGuidConverterTests
    {
        private readonly JsonSerializerOptions _options;

        public JsonGuidConverterTests()
        {
            _options = new JsonSerializerOptions();
            _options.Converters.Add(new JsonGuidConverter());
        }

        [Fact]
        public void Deserialize_Valid_Success()
        {
            Guid value = JsonSerializer.Deserialize<Guid>(@"""a852a27afe324084ae66db579ee3ee18""", _options);
            Assert.Equal(new Guid("a852a27afe324084ae66db579ee3ee18"), value);
        }

        [Fact]
        public void Deserialize_ValidDashed_Success()
        {
            Guid value = JsonSerializer.Deserialize<Guid>(@"""e9b2dcaa-529c-426e-9433-5e9981f27f2e""", _options);
            Assert.Equal(new Guid("e9b2dcaa-529c-426e-9433-5e9981f27f2e"), value);
        }

        [Fact]
        public void Roundtrip_Valid_Success()
        {
            Guid guid = new Guid("a852a27afe324084ae66db579ee3ee18");
            string value = JsonSerializer.Serialize(guid, _options);
            Assert.Equal(guid, JsonSerializer.Deserialize<Guid>(value, _options));
        }

        [Fact]
        public void Deserialize_Null_EmptyGuid()
        {
            Assert.Equal(Guid.Empty, JsonSerializer.Deserialize<Guid>("null", _options));
        }

        [Fact]
        public void Serialize_EmptyGuid_EmptyGuid()
        {
            Assert.Equal($"\"{Guid.Empty:N}\"", JsonSerializer.Serialize(Guid.Empty, _options));
        }

        [Fact]
        public void Serialize_Valid_NoDash_Success()
        {
            var guid = new Guid("531797E9-9457-40E0-88BC-B1D6D38752FA");
            var str = JsonSerializer.Serialize(guid, _options);
            Assert.Equal($"\"{guid:N}\"", str);
        }

        [Fact]
        public void Serialize_Nullable_Success()
        {
            Guid? guid = new Guid("531797E9-9457-40E0-88BC-B1D6D38752FA");
            var str = JsonSerializer.Serialize(guid, _options);
            Assert.Equal($"\"{guid:N}\"", str);
        }
    }
}
