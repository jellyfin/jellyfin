define(['inputManager'], function (inputManager) {

    return function (result) {
        result.success = true;
        switch (result.item.deviceid) {
            case 'displaymirroring':
                inputManager.trigger('enabledisplaymirror');
                break;
            default:
                result.success = false;
                return;
        }
    }

});