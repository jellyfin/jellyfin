(function (globalScope) {

    globalScope.ServerDiscovery = {

        findServers: function (timeoutMs) {

            var deferred = DeferredBuilder.Deferred();

            var servers = [];

            // Expected server properties
            // Name, Id, Address, EndpointAddress (optional)

            deferred.resolveWith(null, [servers]);
            return deferred.promise();
        }
    };

})(window);