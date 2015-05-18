(function () {


    function onDeviceFound(e) {

        console.log('device found');
    }

    function onDeviceLost(e) {

        console.log('device lost');
    }

    function initSdk() {

        var manager = ConnectSDK.discoveryManager;

        manager.setPairingLevel(ConnectSDK.PairingLevel.OFF);
        manager.setAirPlayServiceMode(ConnectSDK.AirPlayServiceMode.Media);

        // Show devices that support playing videos and pausing
        //manager.setCapabilityFilters([
        //  new ConnectSDK.CapabilityFilter(["MediaPlayer.Display.Video", "MediaControl.Pause"])
        //]);

        manager.addListener('devicefound', onDeviceFound);
        manager.addListener('devicelost', onDeviceLost);

        manager.startDiscovery();

        requirejs(['thirdparty/cordova/chromecast']);
    }

    document.addEventListener("deviceready", function () {

        initSdk();

    }, false);


})();