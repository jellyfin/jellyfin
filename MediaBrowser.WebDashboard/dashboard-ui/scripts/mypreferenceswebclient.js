(function ($, window, document) {

    function loadForm(page, user) {

        $('#selectThemeSong', page).val(LocalSettings.val('enableThemeSongs', user.Id) || '').selectmenu("refresh");
        $('#selectBackdrop', page).val(LocalSettings.val('enableBackdrops', user.Id) || '').selectmenu("refresh");

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        LocalSettings.val('enableThemeSongs', user.Id, $('#selectThemeSong', page).val());
        LocalSettings.val('enableBackdrops', user.Id, $('#selectBackdrop', page).val());

        //ApiClient.updateUser(user).done(function () {
            //Dashboard.alert(Globalize.translate("SettingsSaved"));
        //});
        Dashboard.alert(Globalize.translate("SettingsSaved"));
    }

    function onSubmit() {

        var page = $(this).parents('.page');

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (result) {

            saveUser(page, result);

        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#webClientPreferencesPage", function () {

        var page = this;

    }).on('pagebeforeshow', "#webClientPreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            loadForm(page, user);

        });

    });

    window.WebClientPreferencesPage = {
        onSubmit: onSubmit
    };

})(jQuery, window, document);