(function () {

    window.FileSystemBridge = {

        fileExists: function (path) {
            return NativeFileSystem.fileExists(path);
        },

        translateFilePath: function (path) {
            return 'file://' + NativeFileSystem.translateFilePath(path);
        }
    };

})();