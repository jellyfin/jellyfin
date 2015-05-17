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

        var isTimedOut = false;
        var timeout;
        var socketId;

        function startTimer() {

            console.log('starting udp receive timer with timeout ms: ' + timeoutMs);

            timeout = setTimeout(function () {

                isTimedOut = true;
                deferred.resolveWith(null, [servers]);

                if (socketId) {
                    chrome.sockets.udp.onReceive.removeListener(onReceive);
                    chrome.sockets.udp.close(socketId);
                }

            }, timeoutMs);
        }

        function onReceive(info) {

            console.log('ServerDiscovery message received');

            console.log(info);

            if (info.socketId == socketId) {

                try {
                    var json = arrayBufferToString(info.data);
                    console.log('Server discovery json: ' + json);
                    var server = JSON.parse(json);

                    server.RemoteAddress = info.remoteAddress;

                    if (info.remotePort) {
                        server.RemoteAddress += ':' + info.remotePort;
                    }

                    servers.push(server);
                } catch (err) {
                    console.log('Error receiving server info: ' + err);
                }
            }
        }

        var port = 7359;
        console.log('chrome.sockets.udp.create');
        chrome.sockets.udp.create(function (createInfo) {

            socketId = createInfo.socketId;

            console.log('chrome.sockets.udp.bind');

            chrome.sockets.udp.bind(createInfo.socketId, '0.0.0.0', 0, function (result) {

                if (getResultCode(result) != 0) {
                    console.log('bind fail: ' + result);
                    deferred.resolveWith(null, [servers]);
                    chrome.sockets.udp.close(createInfo.socketId);
                    return;
                }

                var data = stringToArrayBuffer('who is EmbyServer?');

                console.log('chrome.sockets.udp.send');
                chrome.sockets.udp.send(createInfo.socketId, data, '255.255.255.255', port, function (result) {

                    if (getResultCode(result) != 0) {
                        console.log('send fail: ' + result);
                        deferred.resolveWith(null, [servers]);
                        chrome.sockets.udp.close(createInfo.socketId);

                    } else {

                        console.log('sendTo: success ' + port);

                        startTimer();
                        chrome.sockets.udp.onReceive.addListener(onReceive);
                    }
                });
            });
        });

        return deferred.promise();
    }

    globalScope.ServerDiscovery = {

        findServers: function (timeoutMs) {

            var deferred = DeferredBuilder.Deferred();

            deviceReadyPromise.done(function () {

                findServersInternal(timeoutMs).done(function (result) {

                    deferred.resolveWith(null, [result]);

                }).fail(function () {

                    deferred.reject();
                });
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