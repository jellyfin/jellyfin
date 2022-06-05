using System;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class UserLibraryControllerTests
    {
        private readonly Mock<IUserManager> userManager;
        private readonly Mock<IUserDataManager> userDataRepository;
        private readonly Mock<ILibraryManager> libraryManager;
        private readonly Mock<IDtoService> dtoService;
        private readonly Mock<IUserViewManager> userViewManager;
        private readonly Mock<IFileSystem> fileSystem;
        private readonly UserLibraryController _target;

        public UserLibraryControllerTests()
        {
            userManager = new Mock<IUserManager>();
            userDataRepository = new Mock<IUserDataManager>();
            libraryManager = new Mock<ILibraryManager>();
            dtoService = new Mock<IDtoService>();
            userViewManager = new Mock<IUserViewManager>();
            fileSystem = new Mock<IFileSystem>();
            _target = new UserLibraryController(userManager.Object, userDataRepository.Object, libraryManager.Object, dtoService.Object, userViewManager.Object, fileSystem.Object);
        }

        [Fact]
        public async Task GetItem_NotFound()
        {
            Folder? folder = null;

            userManager.Setup(u => u.GetUserById(It.IsAny<Guid>()))
                .Returns(new User("Test", "Test", "Test"));

            libraryManager.Setup(u => u.GetUserRootFolder())
                .Returns(folder!);

            var response = await _target.GetItem(Guid.Empty, Guid.NewGuid());

            Assert.IsType<NotFoundResult>(response.Result);
        }

        [Fact]
        public async Task GetItemWhenValidItemId_NotFound()
        {
            Folder? folder = null;

            userManager.Setup(u => u.GetUserById(It.IsAny<Guid>()))
                .Returns(new User("Test", "Test", "Test"));

            libraryManager.Setup(u => u.GetItemById(It.IsAny<Guid>()))
                .Returns(folder!);

            var response = await _target.GetItem(Guid.Empty, Guid.NewGuid());

            Assert.IsType<NotFoundResult>(response.Result);
        }
    }
}
