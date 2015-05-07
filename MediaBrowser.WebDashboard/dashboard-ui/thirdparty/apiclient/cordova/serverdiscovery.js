(function (globalScope) {

    function stringToArrayBuffer(string) {
        // UTF-16LE
        var buf = new ArrayBuffer(string.length * 2);
        var bufView = new Uint16Array(buf);
        for (var i = 0, strLen = string.length; i < strLen; i++) {
            bufView[i] = string.charCodeAt(i);
        }
        return buf;
    }

    function arrayBufferToString(buf) {
        return String.fromCharCode.apply(null, new Uint16Array(buf));
    }

    globalScope.ServerDiscovery = {

        findServers: function (timeoutMs) {

            var deferred = DeferredBuilder.Deferred();

            var servers = [];

            // Expected server properties
            // Name, Id, Address, EndpointAddress (optional)

            var chrome = globalScope.chrome;

            if (!chrome) {
                deferred.resolveWith(null, [servers]);
                return deferred.promise();
            }

            var isTimedOut = false;
            var socketId;

            var timeout = setTimeout(function () {

                isTimedOut = true;
                deferred.resolveWith(null, [servers]);

                if (socketId) {
                    chrome.sockets.udp.onReceive.removeListener(onReceive);
                    chrome.sockets.udp.close(socketId);


                }

            }, timeoutMs);

            function onReceive(info) {

                console.log('ServerDiscovery message received');

                console.log(info);

                if (info.socketId == socketId) {

                    var json = arrayBufferToString(info.data);

                    var server = JSON.parse(json);

                    server.RemoteAddress = info.remoteAddress;

                    if (info.remotePort) {
                        server.RemoteAddress += ':' + info.remotePort;
                    }

                    servers.push(server);
                }
            }

            var port = 7359;
            chrome.sockets.udp.create(function (createInfo) {

                socketId = createInfo.socketId;

                chrome.sockets.udp.bind(createInfo.socketId, '0.0.0.0', port, function (result) {

                    var data = stringToArrayBuffer('who is EmbyServer?');

                    chrome.sockets.udp.send(createInfo.socketId, data, '255.255.255.255', port, function (result) {
                        if (result < 0) {
                            console.log('send fail: ' + result);
                            chrome.sockets.udp.close(createInfo.socketId);

                            if (!isTimedOut) {
                                clearTimeout(timeout);
                                deferred.resolveWith(null, [servers]);
                            }

                        } else {

                            console.log('sendTo: success ' + port);

                            if (!isTimedOut) {
                                chrome.sockets.udp.onReceive.addListener(onReceive);
                            }
                        }
                    });
                });
            });

            return deferred.promise();
        }
    };

})(window);