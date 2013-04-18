(function ($, window, document) {

    function populateLanguages(select, allCultures) {

        var html = "";

        html += "<option value=''>None</option>";

        for (var i = 0, length = allCultures.length; i < length; i++) {

            var culture = allCultures[i];

            html += "<option value='" + culture.ThreeLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        select.html(html).selectmenu("refresh");
    }

    function populateRatings(allParentalRatings, page) {

        var html = "";

        html += "<option value=''>None</option>";

        for (var i = 0, length = allParentalRatings.length; i < length; i++) {

            var rating = allParentalRatings[i];

            html += "<option value='" + rating.Value + "'>" + rating.Name + "</option>";
        }

        $('#selectMaxParentalRating', page).html(html).selectmenu("refresh");
    }
    
    function loadUser(page, user, loggedInUser, parentalRatingsPromise, allCulturesPromise) {

        if (loggedInUser.Configuration.IsAdministrator) {
            $('.lnkMediaLibrary', page).show();
        }

        if (!loggedInUser.Configuration.IsAdministrator || user.Id == loggedInUser.Id) {

            $('#fldIsAdmin', page).hide();
            $('#fldMaxParentalRating', page).hide();
        } else {
            $('#fldIsAdmin', page).show();
            $('#fldMaxParentalRating', page).show();
        }

        Dashboard.setPageTitle(user.Name || "Add User");

        $('#txtUserName', page).val(user.Name);

        parentalRatingsPromise.done(function (allParentalRatings) {
            
            populateRatings(allParentalRatings, page);

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
        });

        allCulturesPromise.done(function (allCultures) {
            
            populateLanguages($('#selectAudioLanguage', page), allCultures);
            populateLanguages($('#selectSubtitleLanguage', page), allCultures);

            $('#selectAudioLanguage', page).val(user.Configuration.AudioLanguagePreference || "").selectmenu("refresh");
            $('#selectSubtitleLanguage', page).val(user.Configuration.SubtitleLanguagePreference || "").selectmenu("refresh");
        });

        $('#chkForcedSubtitlesOnly', page).checked(user.Configuration.UseForcedSubtitlesOnly || false).checkboxradio("refresh");
        $('#chkIsAdmin', page).checked(user.Configuration.IsAdministrator || false).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function onSaveComplete(page) {

        Dashboard.hideLoadingMsg();

        var userId = getParameterByName("userId");

        Dashboard.validateCurrentUser(page);

        if (userId) {
            Dashboard.alert("Settings saved.");
        } else {
            Dashboard.navigate("userprofiles.html");
        }
    }

    function saveUser(user, page) {

        user.Name = $('#txtUserName', page).val();
        user.Configuration.MaxParentalRating = $('#selectMaxParentalRating', page).val() || null;

        user.Configuration.IsAdministrator = $('#chkIsAdmin', page).checked();

        user.Configuration.AudioLanguagePreference = $('#selectAudioLanguage', page).val();
        user.Configuration.SubtitleLanguagePreference = $('#selectSubtitleLanguage', page).val();
        user.Configuration.UseForcedSubtitlesOnly = $('#chkForcedSubtitlesOnly', page).checked();

        var userId = getParameterByName("userId");

        if (userId) {
            ApiClient.updateUser(user).done(function () {
                onSaveComplete(page);
            });
        } else {
            ApiClient.createUser(user).done(function () {
                onSaveComplete(page);
            });
        }
    }

    function editUserPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $(this).parents('.page');

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            if (!userId) {
                saveUser({
                    Configuration: {}
                }, page);
            } else {
                ApiClient.getUser(userId).done(function (result) {
                    saveUser(result, page);
                });
            }

            // Disable default form submission
            return false;
        };
    }

    window.EditUserPage = new editUserPage();

    $(document).on('pageshow', "#editUserPage", function () {

        var page = this;
        
        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");

        if (userId) {
            $('#userProfileNavigation', page).show();
        } else {
            $('#userProfileNavigation', page).hide();
        }

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

        var parentalRatingsPromise = ApiClient.getParentalRatings();
        
        var allCulturesPromise = ApiClient.getCultures();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadUser(page, response1[0] || response1, response2[0], parentalRatingsPromise, allCulturesPromise);

        });

        $("#editUserProfileForm input:first").focus();
    });

})(jQuery, window, document);