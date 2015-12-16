define([], function () {

    function send(info) {

        return new Promise(function (resolve, reject) {

            resolve();
        });
    }

    return {
        send: send
    };

});