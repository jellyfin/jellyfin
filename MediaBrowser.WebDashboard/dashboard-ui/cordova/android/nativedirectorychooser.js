(function () {

    var currentDeferred;
    function chooseDirectory() {
        var deferred = DeferredBuilder.Deferred();
        AndroidDirectoryChooser.chooseDirectory();
        currentDeferred = deferred;
        return deferred.promise();
    }

    function onChosen(path) {

        var deferred = currentDeferred;

        if (deferred) {
            if (path) {
                deferred.resolveWith(null, [path]);
            } else {
                deferred.reject();
            }

            currentDeferred = null;
        }
    }

    window.NativeDirectoryChooser = {
        chooseDirectory: chooseDirectory,
        onChosen: onChosen
    };

})();