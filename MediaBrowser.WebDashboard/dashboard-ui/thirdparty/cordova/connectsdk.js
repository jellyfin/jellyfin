(function () {

    function initSdk() {

        var manager = ConnectSDK.discoveryManager;

        //manager.setPairingLevel(ConnectSDK.PairingLevel.OFF);
        manager.setAirPlayServiceMode(ConnectSDK.AirPlayServiceMode.Media);

        // Show devices that support playing videos and pausing
        //manager.setCapabilityFilters([
        //  new ConnectSDK.CapabilityFilter(["MediaPlayer.Display.Video", "MediaControl.Pause"])
        //]);

        manager.startDiscovery();

        requirejs(['thirdparty/cordova/chromecast', 'thirdparty/cordova/generaldevice']);
    }

    Dashboard.ready(initSdk);

})();