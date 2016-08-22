define(['apphost', 'listViewStyle'], function (appHost) {

    return function (view, params) {

        view.querySelector('.btnLogout').addEventListener('click', function () {

            Dashboard.logout();
        });

        view.addEventListener('viewshow', function () {

            var page = this;

            var userId = params.userId || Dashboard.getCurrentUserId();

            page.querySelector('.lnkDisplayPreferences').setAttribute('href', 'mypreferencesdisplay.html?userId=' + userId);
            page.querySelector('.lnkLanguagePreferences').setAttribute('href', 'mypreferenceslanguages.html?userId=' + userId);
            page.querySelector('.lnkHomeScreenPreferences').setAttribute('href', 'mypreferenceshome.html?userId=' + userId);
            page.querySelector('.lnkMyProfile').setAttribute('href', 'myprofile.html?userId=' + userId);
            page.querySelector('.lnkSync').setAttribute('href', 'mysyncsettings.html?userId=' + userId);
            page.querySelector('.lnkCameraUpload').setAttribute('href', 'camerauploadsettings.html?userId=' + userId);

            if (appHost.supports('cameraupload')) {
                page.querySelector('.lnkCameraUpload').classList.remove('hide');
            } else {
                page.querySelector('.lnkCameraUpload').classList.add('hide');
            }

            if (appHost.supports('sync')) {
                page.querySelector('.lnkSync').classList.remove('hide');
            } else {
                page.querySelector('.lnkSync').classList.add('hide');
            }

            Dashboard.getCurrentUser().then(function (user) {

                page.querySelector('.headerUser').innerHTML = user.Name;

                if (user.Policy.IsAdministrator) {
                    page.querySelector('.adminSection').classList.remove('hide');
                } else {
                    page.querySelector('.adminSection').classList.add('hide');
                }
            });

            if (Dashboard.isConnectMode()) {
                page.querySelector('.selectServer').classList.remove('hide');
            } else {
                page.querySelector('.selectServer').classList.add('hide');
            }
        });
    };
});