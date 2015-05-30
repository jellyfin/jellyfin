(function () {

    window.FileSystem = {

        fileExists: function (path) {
            return NativeFileSystem.fileExists(path);
        }
    };

})();