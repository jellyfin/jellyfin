
define([], function () {

     return function (result) {
        result.success = true;
        switch (result.item.deviceid) {
            case 'displaymirroring':
                MediaController.enableDisplayMirroring(false);
                break;
            default:
                result.success = false;
                return;
        }
    }

});