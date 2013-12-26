(function ($, window, document) {

    function populateRatings(allParentalRatings, page) {

        var html = "";

        html += "<option value=''></option>";

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

    function loadUser(page, user, loggedInUser, allParentalRatings) {

        Dashboard.setPageTitle(user.Name);

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

        $('#chkBlockNotRated', page).checked(user.Configuration.BlockNotRated || false).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function onSaveComplete(page) {

        Dashboard.hideLoadingMsg();

        Dashboard.validateCurrentUser(page);

        Dashboard.alert("Settings saved.");
    }

    function saveUser(user, page) {

        user.Configuration.MaxParentalRating = $('#selectMaxParentalRating', page).val() || null;

        user.Configuration.BlockNotRated = $('#chkBlockNotRated', page).checked();

        ApiClient.updateUser(user).done(function () {
            onSaveComplete(page);
        });
    }

    window.UserParentalControlPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            ApiClient.getUser(userId).done(function (result) {
                saveUser(result, page);
            });

            // Disable default form submission
            return false;
        }
    };

    $(document).on('pageshow', "#userParentalControlPage", function () {

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

        var promise3 = ApiClient.getParentalRatings();

        $.when(promise1, promise2, promise3).done(function (response1, response2, response3) {

            loadUser(page, response1[0] || response1, response2[0], response3[0]);

        });

        $("form input:first", page).focus();
    });

})(jQuery, window, document);