(function (window, $) {

    function showSyncMenu(items) {

        Dashboard.alert('Coming soon.');

    }

    function isAvailable(item, user) {
        return true;
    }

    window.SyncManager = {

        showMenu: showSyncMenu,

        isAvailable: isAvailable

    };

})(window, jQuery);