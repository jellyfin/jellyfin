(function ($, window, document) {

    function populateRatings(allParentalRatings, page) {

        var html = "";

        html += "<option value=''>None</option>";

        var ratings = [];
        var i, length, rating;

        for (i = 0, length = allParentalRatings.length; i < length; i++) {

            rating = allParentalRatings[i];

            if (ratings.length) {

                var lastRating = ratings[ratings.length - 1];

                if (lastRating.Value === rating.Value) {

                    lastRating.Name += "/" + rating.Name;
                    continue;
                }

            }

            ratings.push({ Name: rating.Name, Value: rating.Value });
        }

        for (i = 0, length = ratings.length; i < length; i++) {

            rating = ratings[i];

            html += "<option value='" + rating.Value + "'>" + rating.Name + "</option>";
        }

        $('#selectMaxParentalRating', page).html(html).selectmenu("refresh");
    }

    function loadUser(page, user, loggedInUser, parentalRatingsPromise) {

        if (!loggedInUser.Configuration.IsAdministrator) {
            $('#parentalControlDiv', page).hide();
            $('#fldIsAdmin', page).hide();
            $('#accessControlDiv', page).hide();
        } else {
            $('#parentalControlDiv', page).show();
            $('#accessControlDiv', page).show();
            $('#fldIsAdmin', page).show();
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

        $('#chkIsAdmin', page).checked(user.Configuration.IsAdministrator || false).checkboxradio("refresh");
        $('#chkBlockNotRated', page).checked(user.Configuration.BlockNotRated || false).checkboxradio("refresh");

        $('#chkDisabled', page).checked(user.Configuration.IsDisabled || false).checkboxradio("refresh");
        $('#chkIsHidden', page).checked(user.Configuration.IsHidden || false).checkboxradio("refresh");

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

        user.Configuration.BlockNotRated = $('#chkBlockNotRated', page).checked();

        user.Configuration.IsHidden = $('#chkIsHidden', page).checked();
        user.Configuration.IsDisabled = $('#chkDisabled', page).checked();

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

    $(document).on('pagebeforeshow', "#editUserPage", function () {

        var page = this;

        var userId = getParameterByName("userId");

        if (userId) {
            $('#userProfileNavigation', page).show();
        } else {
            $('#userProfileNavigation', page).hide();
        }

        Dashboard.getCurrentUser().done(function (loggedInUser) {

            if (loggedInUser.Configuration.IsAdministrator) {
                $('.lnkMediaLibrary', page).show().prev().removeClass('ui-last-child');
            } else {
                $('.lnkMediaLibrary', page).hide().prev().addClass('ui-last-child');
            }
        });

    }).on('pageshow', "#editUserPage", function () {

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

        var parentalRatingsPromise = ApiClient.getParentalRatings();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadUser(page, response1[0] || response1, response2[0], parentalRatingsPromise);

        });

        $("#editUserProfileForm input:first").focus();
    });

})(jQuery, window, document);