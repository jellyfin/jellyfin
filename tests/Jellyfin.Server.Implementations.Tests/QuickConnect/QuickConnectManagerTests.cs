using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.QuickConnect;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.LiveTv
{
    public class QuickConnectManagerTests
    {
        private readonly Fixture _fixture;
        private readonly ServerConfiguration _config;
        private readonly QuickConnectManager _quickConnectManager;

        public QuickConnectManagerTests()
        {
            _config = new ServerConfiguration();
            var configManager = new Mock<IServerConfigurationManager>();
            configManager.Setup(x => x.Configuration).Returns(_config);

            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization
            {
                ConfigureMembers = true
            }).Inject(configManager.Object);
            _quickConnectManager = _fixture.Create<QuickConnectManager>();
        }

        [Fact]
        public void IsEnabled_QuickConnectUnavailable_False()
            => Assert.False(_quickConnectManager.IsEnabled);

        [Fact]
        public void TryConnect_QuickConnectUnavailable_ThrowsAuthenticationException()
            => Assert.Throws<AuthenticationException>(_quickConnectManager.TryConnect);

        [Fact]
        public void CheckRequestStatus_QuickConnectUnavailable_ThrowsAuthenticationException()
            => Assert.Throws<AuthenticationException>(() => _quickConnectManager.CheckRequestStatus(string.Empty));

        [Fact]
        public void AuthorizeRequest_QuickConnectUnavailable_ThrowsAuthenticationException()
            => Assert.Throws<AuthenticationException>(() => _quickConnectManager.AuthorizeRequest(Guid.Empty, string.Empty));

        [Fact]
        public void IsEnabled_QuickConnectAvailable_True()
        {
            _config.QuickConnectAvailable = true;
            Assert.True(_quickConnectManager.IsEnabled);
        }

        [Fact]
        public void CheckRequestStatus_QuickConnectAvailable_Success()
        {
            _config.QuickConnectAvailable = true;
            var res1 = _quickConnectManager.TryConnect();
            var res2 = _quickConnectManager.CheckRequestStatus(res1.Secret);
            Assert.Equal(res1, res2);
        }

        [Fact]
        public void AuthorizeRequest_QuickConnectAvailable_Success()
        {
            _config.QuickConnectAvailable = true;
            var res = _quickConnectManager.TryConnect();
            Assert.True(_quickConnectManager.AuthorizeRequest(Guid.Empty, res.Code));
        }
    }
}
