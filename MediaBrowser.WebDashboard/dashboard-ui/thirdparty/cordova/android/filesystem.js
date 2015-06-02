(function () {

    window.FileSystem = {

        fileExists: function (path) {
            return false;
            return NativeFileSystem.fileExists(path);
        },

        translateFilePath: function (path) {
            return NativeFileSystem.translateFilePath(path);
        }
    };

})();