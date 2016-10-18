define(['inputManager'], function (inputManager) {
    'use strict';

    function enableDisplayMirror() {
        return function () {
            inputManager.trigger('enabledisplaymirror');
        };
    }

    return function (result) {

        switch (result.item.deviceid) {
            case 'displaymirroring':
                return enableDisplayMirror();
            default:
                return;
        }
    };

});