(function ($, window, document) {

    function populateLanguages(select, languages) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = languages.length; i < length; i++) {

            var culture = languages[i];

            html += "<option value='" + culture.ThreeLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        $(select).html(html).selectmenu("refresh");
    }

    function loadForm(page, user, loggedInUser, allCulturesPromise) {

        allCulturesPromise.done(function (allCultures) {

            populateLanguages($('#selectAudioLanguage', page), allCultures);
            populateLanguages($('#selectSubtitleLanguage', page), allCultures);

            $('#selectAudioLanguage', page).val(user.Configuration.AudioLanguagePreference || "").selectmenu("refresh");
            $('#selectSubtitleLanguage', page).val(user.Configuration.SubtitleLanguagePreference || "").selectmenu("refresh");
        });

        $('#selectSubtitlePlaybackMode', page).val(user.Configuration.SubtitleMode || "").selectmenu("refresh").trigger('change');
        $('#chkPlayDefaultAudioTrack', page).checked(user.Configuration.PlayDefaultAudioTrack || false).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        user.Configuration.AudioLanguagePreference = $('#selectAudioLanguage', page).val();
        user.Configuration.SubtitleLanguagePreference = $('#selectSubtitleLanguage', page).val();

        user.Configuration.SubtitleMode = $('#selectSubtitlePlaybackMode', page).val();
        user.Configuration.PlayDefaultAudioTrack = $('#chkPlayDefaultAudioTrack', page).checked();

        ApiClient.updateUser(user).done(function () {
            Dashboard.alert(Globalize.translate('SettingsSaved'));
        });
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

    $(document).on('pageinit', "#languagePreferencesPage", function () {

        var page = this;

        $('#selectSubtitlePlaybackMode', page).on('change', function () {

            $('.subtitlesHelp', page).hide();
            $('.subtitles' + this.value + 'Help', page).show();
        });

    }).on('pageshow', "#languagePreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        var promise1 = ApiClient.getUser(userId);

        var promise2 = Dashboard.getCurrentUser();

        var allCulturesPromise = ApiClient.getCultures();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadForm(page, response1[0] || response1, response2[0], allCulturesPromise);

        });

    });

    window.LanguagePreferencesPage = {
        onSubmit: onSubmit
    };

})(jQuery, window, document);