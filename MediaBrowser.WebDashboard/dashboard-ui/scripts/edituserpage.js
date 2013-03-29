var EditUserPage = {

    onPageShow: function () {
        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");
        
        if (userId) {
            $('#userProfileNavigation', this).show();
        } else {
            $('#userProfileNavigation', this).hide();
        }

        var promise4 = ApiClient.getCultures();

        var promise3 = ApiClient.getParentalRatings();

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

        $.when(promise1, promise2, promise3, promise4).done(function (response1, response2, response3, response4) {

            EditUserPage.loadUser(response1[0] || response1, response2[0], response3[0], response4[0]);

        });

        $("#editUserProfileForm input:first").focus();
    },

    loadUser: function (user, loggedInUser, allParentalRatings, allCultures) {

        var page = $($.mobile.activePage);

        EditUserPage.populateLanguages($('#selectAudioLanguage', page), allCultures);
        EditUserPage.populateLanguages($('#selectSubtitleLanguage', page), allCultures);
        EditUserPage.populateRatings(allParentalRatings, page);

        if (!loggedInUser.Configuration.IsAdministrator || user.Id == loggedInUser.Id) {

            $('#fldIsAdmin', page).hide();
            $('#fldMaxParentalRating', page).hide();
        } else {
            $('#fldIsAdmin', page).show();
            $('#fldMaxParentalRating', page).show();
        }

        Dashboard.setPageTitle(user.Name || "Add User");

        $('#txtUserName', page).val(user.Name);

        var ratingValue = "";

        if (user.Configuration.MaxParentalRating) {

            for (var i = 0, length = allParentalRatings.length; i < length; i++) {

                var rating = allParentalRatings[i];

                if (user.Configuration.MaxParentalRating >= rating.Value) {
                    ratingValue = rating.Value;
                }
            }
        }

        $('#selectMaxParentalRating', page).val(ratingValue).selectmenu("refresh");

        $('#selectAudioLanguage', page).val(user.Configuration.AudioLanguagePreference || "").selectmenu("refresh");
        $('#selectSubtitleLanguage', page).val(user.Configuration.SubtitleLanguagePreference || "").selectmenu("refresh");

        $('#chkForcedSubtitlesOnly', page).checked(user.Configuration.UseForcedSubtitlesOnly || false).checkboxradio("refresh");
        $('#chkIsAdmin', page).checked(user.Configuration.IsAdministrator || false).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    },

    populateLanguages: function (select, allCultures) {

        var html = "";

        html += "<option value=''>None</option>";

        for (var i = 0, length = allCultures.length; i < length; i++) {

            var culture = allCultures[i];

            html += "<option value='" + culture.ThreeLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        select.html(html).selectmenu("refresh");
    },

    populateRatings: function (allParentalRatings, page) {

        var html = "";

        html += "<option value=''>None</option>";

        for (var i = 0, length = allParentalRatings.length; i < length; i++) {

            var rating = allParentalRatings[i];

            html += "<option value='" + rating.Value + "'>" + rating.Name + "</option>";
        }

        $('#selectMaxParentalRating', page).html(html).selectmenu("refresh");
    },

    saveUser: function (user) {

        var page = $($.mobile.activePage);

        user.Name = $('#txtUserName', page).val();
        user.Configuration.MaxParentalRating = $('#selectMaxParentalRating', page).val() || null;

        user.Configuration.IsAdministrator = $('#chkIsAdmin', page).checked();

        user.Configuration.AudioLanguagePreference = $('#selectAudioLanguage', page).val();
        user.Configuration.SubtitleLanguagePreference = $('#selectSubtitleLanguage', page).val();
        user.Configuration.UseForcedSubtitlesOnly = $('#chkForcedSubtitlesOnly', page).checked();

        var userId = getParameterByName("userId");

        if (userId) {
            ApiClient.updateUser(user).done(EditUserPage.saveComplete);
        } else {
            ApiClient.createUser(user).done(EditUserPage.saveComplete);
        }
    },

    saveComplete: function () {
        Dashboard.hideLoadingMsg();

        var userId = getParameterByName("userId");

        Dashboard.validateCurrentUser();

        if (userId) {
            Dashboard.alert("Settings saved.");
        } else {
            Dashboard.navigate("userProfiles.html");
        }
    },

    onSubmit: function () {
        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");

        if (!userId) {
            EditUserPage.saveUser({
                Configuration: {}
            });
        } else {
            ApiClient.getUser(userId).done(EditUserPage.saveUser);
        }

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#editUserPage", EditUserPage.onPageShow);
