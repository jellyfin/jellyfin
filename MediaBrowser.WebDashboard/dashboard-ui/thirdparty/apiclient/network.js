(function (globalScope, navigator) {

    function networkStatus() {

        var self = this;

        self.isOnline = function () {

            var online = navigator.onLine;

            if (online == null) {
                online = true;
            }

            return online;
        };
    }

    globalScope.NetworkStatus = new networkStatus();

})(window, window.navigator);