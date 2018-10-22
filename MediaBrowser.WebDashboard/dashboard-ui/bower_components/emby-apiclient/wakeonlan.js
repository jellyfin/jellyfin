define([], function() {
    "use strict";

    function send(info) {
        return Promise.reject()
    }

    function isSupported() {
        return !1
    }
    return {
        send: send,
        isSupported: isSupported
    }
});