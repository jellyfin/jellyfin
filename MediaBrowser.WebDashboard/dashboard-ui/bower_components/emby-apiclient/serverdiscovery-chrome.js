define([], function () {

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

        return new Promise(function (resolve, reject) {

            var servers = [];

            // Expected server properties
            // Name, Id, Address, EndpointAddress (optional)

            var chrome = window.chrome;

            if (!chrome) {
                resolve(servers);
                return;
            }
            if (!chrome.sockets) {
                resolve(servers);
                return;
            }

            var timeout;
            var socketId;

            function onTimerExpired() {
                resolve(servers);

                if (socketId) {
                    chrome.sockets.udp.onReceive.removeListener(onReceive);
                    closeSocket(socketId);
                }
            }

            function startTimer() {

                console.log('starting udp receive timer with timeout ms: ' + timeoutMs);

                timeout = setTimeout(onTimerExpired, timeoutMs);
            }

            function onReceive(info) {

                try {

                    console.log('ServerDiscovery message received');

                    console.log(info);

                    if (info != null && info.socketId == socketId) {
                        var json = arrayBufferToString(info.data);
                        console.log('Server discovery json: ' + json);
                        var server = JSON.parse(json);

                        server.RemoteAddress = info.remoteAddress;

                        if (info.remotePort) {
                            server.RemoteAddress += ':' + info.remotePort;
                        }

                        servers.push(server);
                    }

                } catch (err) {
                    console.log('Error receiving server info: ' + err);
                }
            }

            var port = 7359;
            console.log('chrome.sockets.udp.create');

            startTimer();

            chrome.sockets.udp.create(function (createInfo) {

                if (!createInfo) {
                    console.log('create fail');
                    return;
                }
                if (!createInfo.socketId) {
                    console.log('create fail');
                    return;
                }

                socketId = createInfo.socketId;

                console.log('chrome.sockets.udp.bind');
                chrome.sockets.udp.bind(createInfo.socketId, '0.0.0.0', 0, function (bindResult) {

                    if (getResultCode(bindResult) != 0) {
                        console.log('bind fail: ' + bindResult);
                        return;
                    }

                    var data = stringToArrayBuffer('who is EmbyServer?');

                    console.log('chrome.sockets.udp.send');

                    chrome.sockets.udp.send(createInfo.socketId, data, '255.255.255.255', port, function (sendResult) {

                        if (getResultCode(sendResult) != 0) {
                            console.log('send fail: ' + sendResult);

                        } else {
                            chrome.sockets.udp.onReceive.addListener(onReceive);
                            console.log('sendTo: success ' + port);
                        }
                    });
                });
            });
        });
    }

    return {

        findServers: function (timeoutMs) {

            return new Promise(function (resolve, reject) {

                try {
                    findServersInternal(timeoutMs).then(resolve, function () {

                        resolve([]);
                    });

                } catch (err) {
                    resolve([]);
                }
            });
        }
    };

});