define([], function () {

     return function (result) {
        switch (result.item.deviceid) {
            default:
                result.success = false;
                return;
        }
    }

});