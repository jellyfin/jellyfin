(function ($, window, document) {

    function loadMediaFolders(page, user, mediaFolders) {

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>' + Globalize.translate('HeaderMediaFolders') + '</legend>';

        for (var i = 0, length = mediaFolders.length; i < length; i++) {

            var folder = mediaFolders[i];

            var id = 'mediaFolder' + i;

            var checkedAttribute = user.Configuration.BlockedMediaFolders.indexOf(folder.Id) == -1 && user.Configuration.BlockedMediaFolders.indexOf(folder.Name) == -1 ? ' checked="checked"' : '';

            html += '<input class="chkMediaFolder" data-foldername="' + folder.Id + '" type="checkbox" id="' + id + '"' + checkedAttribute + ' />';
            html += '<label for="' + id + '">' + folder.Name + '</label>';
        }

        html += '</fieldset>';

        $('.mediaFolderAccess', page).html(html).trigger('create');
    }

    function loadChannels(page, user, channels) {

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>' + Globalize.translate('HeaderChannels') + '</legend>';

        for (var i = 0, length = channels.length; i < length; i++) {

            var folder = channels[i];

            var id = 'channels' + i;

            var checkedAttribute = user.Configuration.BlockedChannels.indexOf(folder.Id) == -1 ? ' checked="checked"' : '';

            html += '<input class="chkChannel" data-foldername="' + folder.Id + '" type="checkbox" id="' + id + '"' + checkedAttribute + ' />';
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

    function loadUser(page, user, loggedInUser, mediaFolders, channels) {

        $(page).trigger('userloaded', [user]);

        Dashboard.setPageTitle(user.Name);

        loadChannels(page, user, channels);
        loadMediaFolders(page, user, mediaFolders);

        Dashboard.hideLoadingMsg();
    }

    function onSaveComplete(page) {

        Dashboard.hideLoadingMsg();

        Dashboard.alert(Globalize.translate('SettingsSaved'));
    }

    function saveUser(user, page) {

        user.Configuration.BlockedMediaFolders = $('.chkMediaFolder:not(:checked)', page).map(function () {

            return this.getAttribute('data-foldername');

        }).get();

        user.Configuration.BlockedChannels = $('.chkChannel:not(:checked)', page).map(function () {

            return this.getAttribute('data-foldername');

        }).get();

        ApiClient.updateUser(user).done(function () {
            onSaveComplete(page);
        });
    }

    window.LibraryAccessPage = {

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

    $(document).on('pageshow', "#userLibraryAccessPage", function () {

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

        var promise4 = ApiClient.getJSON(ApiClient.getUrl("Library/MediaFolders", { IsHidden: false }));

        var promise5 = ApiClient.getJSON(ApiClient.getUrl("Channels"));

        $.when(promise1, promise2, promise4, promise5).done(function (response1, response2, response4, response5) {

            loadUser(page, response1[0] || response1, response2[0], response4[0].Items, response5[0].Items);

        });
    });

})(jQuery, window, document);