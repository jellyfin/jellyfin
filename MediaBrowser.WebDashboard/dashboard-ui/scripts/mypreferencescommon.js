$(document).on('pageshowready', "#myPreferencesMenuPage", function () {

    var page = this;

    var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

    $('.lnkDisplayPreferences', page).attr('href', 'mypreferencesdisplay.html?userId=' + userId);
    $('.lnkLanguagePreferences', page).attr('href', 'mypreferenceslanguages.html?userId=' + userId);
    $('.lnkHomeScreenPreferences', page).attr('href', 'mypreferenceshome.html?userId=' + userId);
    $('.lnkMyProfile', page).attr('href', 'myprofile.html?userId=' + userId);
    $('.lnkSync', page).attr('href', 'mysyncsettings.html?userId=' + userId);

    if (AppInfo.supportsSyncPathSetting) {
        page.querySelector('.lnkSync').classList.remove('hide');
    } else {
        page.querySelector('.lnkSync').classList.add('hide');
    }
});