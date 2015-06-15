(function (globalScope) {

    function send(info) {

        var deferred = DeferredBuilder.Deferred();
        deferred.resolve();
        return deferred.promise();
    }

    globalScope.WakeOnLan = {
        send: send
    };

})(window);