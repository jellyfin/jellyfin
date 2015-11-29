(function (globalScope) {

    globalScope.ServerDiscovery = {

        findServers: function (timeoutMs) {

            return new Promise(function (resolve, reject) {

                var servers = [];

                // Expected server properties
                // Name, Id, Address, EndpointAddress (optional)
                resolve(servers);
            });
        }
    };

})(window);