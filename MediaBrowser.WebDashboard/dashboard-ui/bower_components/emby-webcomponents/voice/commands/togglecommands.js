define(['inputManager'], function (inputManager) {
    'use strict';

    function toggleDisplayMirror() {
        return function () {
            inputManager.trigger('toggledisplaymirror');
        };
    }

    return function (result) {

        switch (result.item.deviceid) {
            case 'displaymirroring':
                return toggleDisplayMirror();
            default:
                return;
        }
    };

});