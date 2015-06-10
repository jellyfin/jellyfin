(function () {

    window.FileSystemBridge = {

        fileExists: function (path) {
            return false;
        },

        translateFilePath: function (path) {
            return 'file://' + path;
        }
    };
})();