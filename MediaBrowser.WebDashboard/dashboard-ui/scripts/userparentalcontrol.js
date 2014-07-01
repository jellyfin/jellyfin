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

    function loadMediaFolders(page, user, mediaFolders) {

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>Media Folders</legend>';

        for (var i = 0, length = mediaFolders.length; i < length; i++) {

            var folder = mediaFolders[i];

            var id = 'mediaFolder' + i;

            var checkedAttribute = user.Configuration.BlockedMediaFolders.indexOf(folder.Id) == -1 && user.Configuration.BlockedMediaFolders.indexOf(folder.Name) == -1 ? ' checked="checked"' : '';

            html += '<input class="chkMediaFolder" data-foldername="' + folder.Id + '" type="checkbox" data-mini="true" id="' + id + '"' + checkedAttribute + ' />';
            html += '<label for="' + id + '">' + folder.Name + '</label>';
        }

        html += '</fieldset>';

        $('.mediaFolderAccess', page).html(html).trigger('create');
    }

    function loadChannels(page, user, channels) {

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>Channels</legend>';

        for (var i = 0, length = channels.length; i < length; i++) {

            var folder = channels[i];

            var id = 'channels' + i;

            var checkedAttribute = user.Configuration.BlockedChannels.indexOf(folder.Id) == -1 ? ' checked="checked"' : '';

            html += '<input class="chkChannel" data-foldername="' + folder.Id + '" type="checkbox" data-mini="true" id="' + id + '"' + checkedAttribute + ' />';
            html += '<label for="' + id + '">' + folder.Name + '</label>';
        }

        html += '</fieldset>';

        $('.channelAccess', page).show().html(html).trigger('create');

        if (channels.length) {
            $('.channelAccessContainer', page).show();
        } else {
            $('.channelAccessContainer', page).hide();
        }
    }

    function loadUnratedItems(page, user) {

        var items = [
            { name: 'Books', value: 'Book' },
            { name: 'Games', value: 'Game' },
            { name: 'Internet Channel Content', value: 'ChannelContent' },
            { name: 'Live TV Channels', value: 'LiveTvChannel' },
            { name: 'Live TV Programs', value: 'LiveTvProgram' },
            { name: 'Movies', value: 'Movie' },
            { name: 'Music', value: 'Music' },
            { name: 'Trailers', value: 'Trailer' },
            { name: 'TV Shows', value: 'Series' },
            { name: 'Others', value: 'Other' }
        ];

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>Block items with no rating information:</legend>';

        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var id = 'unratedItem' + i;

            var checkedAttribute = user.Configuration.BlockUnratedItems.indexOf(item.value) != -1 ? ' checked="checked"' : '';

            html += '<input class="chkUnratedItem" data-itemtype="' + item.value + '" type="checkbox" data-mini="true" id="' + id + '"' + checkedAttribute + ' />';
            html += '<label for="' + id + '">' + item.name + '</label>';
        }

        html += '</fieldset>';

        $('.blockUnratedItems', page).html(html).trigger('create');
    }

    function loadUser(page, user, loggedInUser, allParentalRatings, mediaFolders, channels) {

        Dashboard.setPageTitle(user.Name);

        loadChannels(page, user, channels);
        loadMediaFolders(page, user, mediaFolders);
        loadUnratedItems(page, user);

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

        Dashboard.hideLoadingMsg();
    }

    function onSaveComplete(page) {

        Dashboard.hideLoadingMsg();

        Dashboard.alert(Globalize.translate('SettingsSaved'));
    }

    function saveUser(user, page) {

        user.Configuration.MaxParentalRating = $('#selectMaxParentalRating', page).val() || null;

        user.Configuration.BlockedMediaFolders = $('.chkMediaFolder:not(:checked)', page).map(function () {

            return this.getAttribute('data-foldername');

        }).get();

        user.Configuration.BlockedChannels = $('.chkChannel:not(:checked)', page).map(function () {

            return this.getAttribute('data-foldername');

        }).get();

        user.Configuration.BlockUnratedItems = $('.chkUnratedItem:checked', page).map(function () {

            return this.getAttribute('data-itemtype');

        }).get();

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

        var promise4 = $.getJSON(ApiClient.getUrl("Library/MediaFolders", {IsHidden: false}));

        var promise5 = $.getJSON(ApiClient.getUrl("Channels"));

        $.when(promise1, promise2, promise3, promise4, promise5).done(function (response1, response2, response3, response4, response5) {

            loadUser(page, response1[0] || response1, response2[0], response3[0], response4[0].Items, response5[0].Items);

        });
    });

})(jQuery, window, document);