(function (globalScope, navigator) {

    function networkStatus() {

        var self = this;

        self.isNetworkAvailable = function () {

            var online = navigator.onLine;

            if (online == null) {
                online = true;
            }

            return online;
        };

        self.isAnyLocalNetworkAvailable = function () {

            return self.isNetworkAvailable();
        };
    }

    globalScope.NetworkStatus = new networkStatus();

})(window, window.navigator);