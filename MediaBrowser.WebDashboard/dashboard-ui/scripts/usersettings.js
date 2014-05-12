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

    function loadUser(page, user, loggedInUser, allCulturesPromise) {

        Dashboard.setPageTitle(user.Name);

        allCulturesPromise.done(function (allCultures) {

            populateLanguages($('#selectAudioLanguage', page), allCultures);
            populateLanguages($('#selectSubtitleLanguage', page), allCultures);

            $('#selectAudioLanguage', page).val(user.Configuration.AudioLanguagePreference || "").selectmenu("refresh");
            $('#selectSubtitleLanguage', page).val(user.Configuration.SubtitleLanguagePreference || "").selectmenu("refresh");
        });

        $('#chkDisplayMissingEpisodes', page).checked(user.Configuration.DisplayMissingEpisodes || false).checkboxradio("refresh");
        $('#chkDisplayUnairedEpisodes', page).checked(user.Configuration.DisplayUnairedEpisodes || false).checkboxradio("refresh");

        $('#chkGroupMoviesIntoCollections', page).checked(user.Configuration.GroupMoviesIntoBoxSets || false).checkboxradio("refresh");

        $('#selectSubtitlePlaybackMode', page).val(user.Configuration.SubtitleMode || "").selectmenu("refresh").trigger('change');
        $('#chkPlayDefaultAudioTrack', page).checked(user.Configuration.PlayDefaultAudioTrack || false).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function onSaveComplete(page) {

        Dashboard.hideLoadingMsg();

        var userId = getParameterByName("userId");

        Dashboard.validateCurrentUser(page);

        if (userId) {
            Dashboard.alert(Globalize.translate("SettingsSaved"));
        } else {
            Dashboard.navigate("userprofiles.html");
        }
    }

    function saveUser(user, page) {

        user.Configuration.AudioLanguagePreference = $('#selectAudioLanguage', page).val();
        user.Configuration.SubtitleLanguagePreference = $('#selectSubtitleLanguage', page).val();
        user.Configuration.DisplayMissingEpisodes = $('#chkDisplayMissingEpisodes', page).checked();
        user.Configuration.DisplayUnairedEpisodes = $('#chkDisplayUnairedEpisodes', page).checked();
        user.Configuration.GroupMoviesIntoBoxSets = $('#chkGroupMoviesIntoCollections', page).checked();

        user.Configuration.SubtitleMode = $('#selectSubtitlePlaybackMode', page).val();
        user.Configuration.PlayDefaultAudioTrack = $('#chkPlayDefaultAudioTrack', page).checked();

        ApiClient.updateUser(user).done(function () {
            onSaveComplete(page);
        });
    }

    function userSettingsPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $(this).parents('.page');

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            ApiClient.getUser(userId).done(function (result) {
                saveUser(result, page);
            });

            // Disable default form submission
            return false;
        };
    }

    window.UserSettingsPage = new userSettingsPage();

    $(document).on('pageinit', "#userSettingsPage", function () {

        var page = this;

        $('#selectSubtitlePlaybackMode', page).on('change', function () {

            $('.subtitlesHelp', page).hide();
            $('.subtitles' + this.value + 'Help', page).show();
        });

    }).on('pagebeforeshow', "#userSettingsPage", function () {

        var page = this;

        Dashboard.getCurrentUser().done(function (loggedInUser) {

            if (loggedInUser.Configuration.IsAdministrator) {
                $('#lnkParentalControl', page).show();
            } else {
                $('#lnkParentalControl', page).hide();
            }
        });

    }).on('pageshow', "#userSettingsPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");

        var promise1;

        if (!userId) {

            var deferred = $.Deferred();

            deferred.resolveWith(null, [{
                Configuration: {}
            }]);

            promise1 = deferred.promise();
        } else {

            promise1 = ApiClient.getUser(userId);
        }

        var promise2 = Dashboard.getCurrentUser();

        var allCulturesPromise = ApiClient.getCultures();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadUser(page, response1[0] || response1, response2[0], allCulturesPromise);

        });

        $("#userSettingsForm input:first").focus();
    });

})(jQuery, window, document);