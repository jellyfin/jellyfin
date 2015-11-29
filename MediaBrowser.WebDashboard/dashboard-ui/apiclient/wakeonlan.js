(function (globalScope) {

    function send(info) {

        return new Promise(function (resolve, reject) {

            resolve();
        });
    }

    globalScope.WakeOnLan = {
        send: send
    };

})(window);