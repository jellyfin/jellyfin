define(['inputManager'], function (inputManager) {

    function disableDisplayMirror() {
        return function () {
            inputManager.trigger('disabledisplaymirror');
        };
    }

    return function (result) {

        switch (result.item.deviceid) {
            case 'displaymirroring':
                return disableDisplayMirror();
                break;
            default:
                return;
        }
    }

});