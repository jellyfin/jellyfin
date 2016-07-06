define(['inputManager'], function (inputManager) {

     return function (result) {
        switch (result.item.deviceid) {
            default:
                result.success = false;
                return;
        }
    }

});