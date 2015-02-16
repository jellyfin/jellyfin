(function (globalScope) {

    globalScope.ServerDiscovery = {

        findServers: function () {
            var deferred = DeferredBuilder.Deferred();
            var servers = [];
            deferred.resolveWith(null, [servers]);
            return deferred.promise();
        }
    };

})(window);