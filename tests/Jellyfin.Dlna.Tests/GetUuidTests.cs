using Emby.Dlna.PlayTo;
using Xunit;

namespace Jellyfin.Dlna.Tests
{
    public static class GetUuidTests
    {
        [Theory]
        [InlineData("uuid:fc4ec57e-b051-11db-88f8-0060085db3f6::urn:schemas-upnp-org:device:WANDevice:1", "fc4ec57e-b051-11db-88f8-0060085db3f6")]
        [InlineData("uuid:IGD{8c80f73f-4ba0-45fa-835d-042505d052be}000000000000", "8c80f73f-4ba0-45fa-835d-042505d052be")]
        [InlineData("uuid:IGD{8c80f73f-4ba0-45fa-835d-042505d052be}000000000000::urn:schemas-upnp-org:device:InternetGatewayDevice:1", "8c80f73f-4ba0-45fa-835d-042505d052be")]
        [InlineData("uuid:00000000-0000-0000-0000-000000000000::upnp:rootdevice", "00000000-0000-0000-0000-000000000000")]
        [InlineData("uuid:fc4ec57e-b051-11db-88f8-0060085db3f6", "fc4ec57e-b051-11db-88f8-0060085db3f6")]
        public static void GetUuid_Valid_Success(string usn, string uuid)
            => Assert.Equal(uuid, PlayToManager.GetUuid(usn));
    }
}
