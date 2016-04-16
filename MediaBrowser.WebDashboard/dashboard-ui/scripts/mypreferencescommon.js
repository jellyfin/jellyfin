pageIdOn('pageinit', 'myPreferencesMenuPage', function () {

    var page = this;

    $('.btnLogout', page).on('click', function () {

        Dashboard.logout();
    });

});

pageIdOn('pageshow', 'myPreferencesMenuPage', function () {

    var page = this;

    var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

    $('.lnkDisplayPreferences', page).attr('href', 'mypreferencesdisplay.html?userId=' + userId);
    $('.lnkLanguagePreferences', page).attr('href', 'mypreferenceslanguages.html?userId=' + userId);
    $('.lnkHomeScreenPreferences', page).attr('href', 'mypreferenceshome.html?userId=' + userId);
    $('.lnkMyProfile', page).attr('href', 'myprofile.html?userId=' + userId);
    $('.lnkSync', page).attr('href', 'mysyncsettings.html?userId=' + userId);

    if (Dashboard.capabilities().SupportsSync) {
        page.querySelector('.lnkSync').classList.remove('hide');
    } else {
        page.querySelector('.lnkSync').classList.add('hide');
    }

    Dashboard.getCurrentUser().then(function (user) {

        page.querySelector('.headerUser').innerHTML = user.Name;

        if (!(AppInfo.isNativeApp && browserInfo.safari) && user.Policy.IsAdministrator) {
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