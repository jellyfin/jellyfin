(function (globalScope) {

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

    function stringToArrayBuffer(string) {
        // UTF-16LE
        var buf = new ArrayBuffer(string.length * 2);
        var bufView = new Uint16Array(buf);
        for (var i = 0, strLen = string.length; i < strLen; i++) {
            bufView[i] = string.charCodeAt(i);
        }
        return buf;
    }

    // https://github.com/agnat/node_wake_on_lan/blob/master/wake_on_lan.js

    globalScope.WakeOnLan = {

        send: function (info) {

            var deferred = DeferredBuilder.Deferred();

            var chrome = globalScope.chrome;

            if (!chrome) {
                deferred.resolve();
                return deferred.promise();
            }

            var port = info.Port || 9;

            //chrome.sockets.udp.create(function (createInfo) {

            //    if (!createInfo) {
            //        console.log('create fail');
            //        return;
            //    }
            //    if (!createInfo.socketId) {
            //        console.log('create fail');
            //        return;
            //    }

            //    var socketId = createInfo.socketId;

            //    console.log('chrome.sockets.udp.bind');
            //    chrome.sockets.udp.bind(createInfo.socketId, '0.0.0.0', 0, function (bindResult) {

            //        if (getResultCode(bindResult) != 0) {
            //            console.log('bind fail: ' + bindResult);
            //            deferred.resolve();
            //            closeSocket(socketId);
            //        }

            //        var data = stringToArrayBuffer('who is EmbyServer?');

            //        console.log('chrome.sockets.udp.send');

            //        chrome.sockets.udp.send(createInfo.socketId, data, '255.255.255.255', port, function (sendResult) {

            //            deferred.resolve();
            //            closeSocket(socketId);
            //        });
            //    });
            //});

            deferred.resolve();
            return deferred.promise();
        }
    };

})(window);