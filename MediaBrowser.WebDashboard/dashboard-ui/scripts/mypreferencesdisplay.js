(function ($, window, document) {

    function loadForm(page, user) {

        $('#chkDisplayMissingEpisodes', page).checked(user.Configuration.DisplayMissingEpisodes || false).checkboxradio("refresh");
        $('#chkDisplayUnairedEpisodes', page).checked(user.Configuration.DisplayUnairedEpisodes || false).checkboxradio("refresh");

        $('#chkGroupMoviesIntoCollections', page).checked(user.Configuration.GroupMoviesIntoBoxSets || false).checkboxradio("refresh");
        $('#chkDisplayCollectionView', page).checked(user.Configuration.DisplayCollectionsView || false).checkboxradio("refresh");

        ApiClient.getItems(user.Id, {}).done(function (result) {

            var folderHtml = '';

            folderHtml += '<div data-role="controlgroup">';
            folderHtml += result.Items.map(function (i) {

                var currentHtml = '';

                var id = 'chkGroupFolder' + i.Id;

                currentHtml += '<label for="' + id + '">' + i.Name + '</label>';

                var isChecked = user.Configuration.ExcludeFoldersFromGrouping.indexOf(i.Id) == -1;
                var checkedHtml = isChecked ? ' checked="checked"' : '';

                currentHtml += '<input class="chkGroupFolder" data-folderid="' + i.Id + '" type="checkbox" id="' + id + '"' + checkedHtml + ' />';

                return currentHtml;

            }).join('');

            folderHtml += '</div>';

            $('.folderGroupList', page).html(folderHtml).trigger('create');

            Dashboard.hideLoadingMsg();
        });

        ApiClient.getJSON(ApiClient.getUrl("Channels", {

            UserId: user.Id

        })).done(function (result) {

            var folderHtml = '';

            folderHtml += '<div data-role="controlgroup">';
            folderHtml += result.Items.map(function (i) {

                var currentHtml = '';

                var id = 'chkGroupChannel' + i.Id;

                currentHtml += '<label for="' + id + '">' + i.Name + '</label>';

                var isChecked = user.Configuration.DisplayChannelsWithinViews.indexOf(i.Id) != -1;
                var checkedHtml = isChecked ? ' checked="checked"' : '';

                currentHtml += '<input class="chkGroupChannel" data-channelid="' + i.Id + '" type="checkbox" id="' + id + '"' + checkedHtml + ' />';

                return currentHtml;

            }).join('');

            folderHtml += '</div>';

            $('.channelGroupList', page).html(folderHtml).trigger('create');
        });

    }

    function saveUser(page, user) {

        user.Configuration.DisplayMissingEpisodes = $('#chkDisplayMissingEpisodes', page).checked();
        user.Configuration.DisplayUnairedEpisodes = $('#chkDisplayUnairedEpisodes', page).checked();
        user.Configuration.GroupMoviesIntoBoxSets = $('#chkGroupMoviesIntoCollections', page).checked();

        user.Configuration.DisplayCollectionsView = $('#chkDisplayCollectionView', page).checked();

        user.Configuration.ExcludeFoldersFromGrouping = $(".chkGroupFolder:not(:checked)", page).get().map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.DisplayChannelsWithinViews = $(".chkGroupChannel:checked", page).get().map(function (i) {

            return i.getAttribute('data-channelid');
        });

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

    $(document).on('pageinit', "#displayPreferencesPage", function () {

        var page = this;

    }).on('pageshow', "#displayPreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            loadForm(page, user);

        });

    }).on('pageshow', ".userPreferencesPage", function () {

        var page = this;

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        $('.lnkDisplayPreferences', page).attr('href', 'mypreferencesdisplay.html?userId=' + userId);
        $('.lnkLanguagePreferences', page).attr('href', 'mypreferenceslanguages.html?userId=' + userId);
        $('.lnkWebClientPreferences', page).attr('href', 'mypreferenceswebclient.html?userId=' + userId);
    });

    window.DisplayPreferencesPage = {
        onSubmit: onSubmit
    };

})(jQuery, window, document);