(function () {

    window.FileSystem = {

        fileExists: function (path) {
            var exists = NativeFileSystem.fileExists(path);
            return false;
        }
    };

})();