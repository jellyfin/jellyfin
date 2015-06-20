(function () {

    Dashboard.exit = function () {

        if (navigator.app && navigator.app.exitApp) {
            navigator.app.exitApp();
        } else {
            Dashboard.logout();
        }
    };

    function onBackKeyDown(e) {
        if (Dashboard.exitOnBack()) {
            e.preventDefault();
            Dashboard.exit();
        }
        else {
            history.back();
        }
    }

    document.addEventListener("backbutton", onBackKeyDown, false);

})();