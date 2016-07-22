define(['inputManager'], function (inputManager) {

    function enableDisplayMirror() {
        return function () {
            inputManager.trigger('enabledisplaymirror');
        };
    }

    return function (result) {

        switch (result.item.deviceid) {
            case 'displaymirroring':
                return enableDisplayMirror();
                break;
            default:
                return;
        }
    }

});