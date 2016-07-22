define(['inputManager'], function (inputManager) {

    function toggleDisplayMirror() {
        return function () {
            inputManager.trigger('toggledisplaymirror');
        };
    }

    return function (result) {

        switch (result.item.deviceid) {
            case 'displaymirroring':
                return toggleDisplayMirror();
                break;
            default:
                return;
        }
    }

});