define(['inputManager'], function (inputManager) {
    'use strict';

    function disableDisplayMirror() {
        return function () {
            inputManager.trigger('disabledisplaymirror');
        };
    }

    return function (result) {

        switch (result.item.deviceid) {
            case 'displaymirroring':
                return disableDisplayMirror();
            default:
                return;
        }
    };

});