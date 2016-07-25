pageIdOn('pageinit', 'myPreferencesMenuPage', function () {

    var page = this;

    page.querySelector('.btnLogout').addEventListener('click', function () {

        Dashboard.logout();
    });

});

pageIdOn('pageshow', 'myPreferencesMenuPage', function () {

    var page = this;

    var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

    page.querySelector('.lnkDisplayPreferences').setAttribute('href', 'mypreferencesdisplay.html?userId=' + userId);
    page.querySelector('.lnkLanguagePreferences').setAttribute('href', 'mypreferenceslanguages.html?userId=' + userId);
    page.querySelector('.lnkHomeScreenPreferences').setAttribute('href', 'mypreferenceshome.html?userId=' + userId);
    page.querySelector('.lnkMyProfile').setAttribute('href', 'myprofile.html?userId=' + userId);
    page.querySelector('.lnkSync').setAttribute('href', 'mysyncsettings.html?userId=' + userId);

    if (Dashboard.capabilities().SupportsSync) {
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