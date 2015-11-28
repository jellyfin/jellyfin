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

    function getResultCode(result) {

        if (result != null && result.resultCode != null) {
            return result.resultCode;
        }

        return result;
    }

    function closeSocket(socketId) {

        try {
            chrome.sockets.udp.close(socketId);
        } catch (err) {

        }
    }

    function findServersInternal(timeoutMs) {

        var deferred = DeferredBuilder.Deferred();

        var servers = [];

        // Expected server properties
        // Name, Id, Address, EndpointAddress (optional)

        var chrome = globalScope.chrome;

        if (!chrome) {
            deferred.resolveWith(null, [servers]);
            return deferred.promise();
        }

        var timeout;
        var socketId;

        function onTimerExpired() {
            deferred.resolveWith(null, [servers]);

            if (socketId) {
                chrome.sockets.udp.onReceive.removeListener(onReceive);
                closeSocket(socketId);
            }
        }

        function startTimer() {

            Logger.log('starting udp receive timer with timeout ms: ' + timeoutMs);

            timeout = setTimeout(onTimerExpired, timeoutMs);
        }

        function onReceive(info) {

            try {

                Logger.log('ServerDiscovery message received');

                Logger.log(info);

                if (info != null && info.socketId == socketId) {
                    var json = arrayBufferToString(info.data);
                    Logger.log('Server discovery json: ' + json);
                    var server = JSON.parse(json);

                    server.RemoteAddress = info.remoteAddress;

                    if (info.remotePort) {
                        server.RemoteAddress += ':' + info.remotePort;
                    }

                    servers.push(server);
                }

            } catch (err) {
                Logger.log('Error receiving server info: ' + err);
            }
        }

        var port = 7359;
        Logger.log('chrome.sockets.udp.create');

        startTimer();

        chrome.sockets.udp.create(function (createInfo) {

            if (!createInfo) {
                Logger.log('create fail');
                return;
            }
            if (!createInfo.socketId) {
                Logger.log('create fail');
                return;
            }

            socketId = createInfo.socketId;

            Logger.log('chrome.sockets.udp.bind');
            chrome.sockets.udp.bind(createInfo.socketId, '0.0.0.0', 0, function (bindResult) {

                if (getResultCode(bindResult) != 0) {
                    Logger.log('bind fail: ' + bindResult);
                    return;
                }

                var data = stringToArrayBuffer('who is EmbyServer?');

                Logger.log('chrome.sockets.udp.send');

                chrome.sockets.udp.send(createInfo.socketId, data, '255.255.255.255', port, function (sendResult) {

                    if (getResultCode(sendResult) != 0) {
                        Logger.log('send fail: ' + sendResult);

                    } else {
                        chrome.sockets.udp.onReceive.addListener(onReceive);
                        Logger.log('sendTo: success ' + port);
                    }
                });
            });
        });

        return deferred.promise();
    }

    globalScope.ServerDiscovery = {

        findServers: function (timeoutMs) {

            var deferred = DeferredBuilder.Deferred();

            deviceReadyPromise.then(function () {

                try {
                    findServersInternal(timeoutMs).then(function (result) {

                        deferred.resolveWith(null, [result]);

                    }, function () {

                        deferred.resolveWith(null, [[]]);
                    });

                } catch (err) {
                    deferred.resolveWith(null, [[]]);
                }
            });

            return deferred.promise();
        }
    };

    var deviceReadyDeferred = DeferredBuilder.Deferred();
    var deviceReadyPromise = deviceReadyDeferred.promise();

    document.addEventListener("deviceready", function () {

        deviceReadyDeferred.resolve();

    }, false);


})(window);