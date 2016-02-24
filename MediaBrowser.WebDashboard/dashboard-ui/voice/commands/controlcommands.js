
define([], function () {

    return function (result) {
        result.success = true;
        if (result.properties.devicename)
            MediaController.trySetActiveDeviceName(result.properties.devicename);

        return;
    }
});