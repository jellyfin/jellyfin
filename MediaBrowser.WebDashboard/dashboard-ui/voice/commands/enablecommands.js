
define([], function () {

    return function (result) {
        result.success = true;
        switch (result.item.deviceid) {
            case 'displaymirroring':
                MediaController.enableDisplayMirroring(true);
                break;
            default:
                result.success = false;
                return;
        }
    }

});