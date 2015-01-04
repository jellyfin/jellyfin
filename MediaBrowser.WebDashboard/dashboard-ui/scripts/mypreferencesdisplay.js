(function ($, window, document) {

    function renderViews(page, user, result) {
        
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
    }

    function renderLatestItems(page, user, result) {

        var folderHtml = '';

        folderHtml += '<div data-role="controlgroup">';
        folderHtml += result.Items.map(function (i) {

            var currentHtml = '';

            var id = 'chkIncludeInLatest' + i.Id;

            currentHtml += '<label for="' + id + '">' + i.Name + '</label>';

            var isChecked = user.Configuration.LatestItemsExcludes.indexOf(i.Id) == -1;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<input class="chkIncludeInLatest" data-folderid="' + i.Id + '" type="checkbox" id="' + id + '"' + checkedHtml + ' />';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        $('.latestItemsList', page).html(folderHtml).trigger('create');
    }

    function renderChannels(page, user, result) {

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
    }

    function renderViewOrder(page, user, result) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-mini="true">';
        var index = 0;

        html += result.Items.map(function (view) {

            var currentHtml = '';

            currentHtml += '<li data-mini="true" class="viewItem" data-viewid="' + view.Id + '">';

            if (index > 0) {
                currentHtml += '<a href="#">' + view.Name + '</a>';

                currentHtml += '<a class="btnViewItemUp btnViewItemMove" href="#" data-icon="arrow-u">' + Globalize.translate('ButtonUp') + '</a>';
            }
            else if (result.Items.length > 1) {

                currentHtml += '<a href="#">' + view.Name + '</a>';

                currentHtml += '<a class="btnViewItemDown btnViewItemMove" href="#" data-icon="arrow-d">' + Globalize.translate('ButtonDown') + '</a>';
            }
            else {

                currentHtml += view.Name;

            }
            html += '</li>';

            index++;
            return currentHtml;

        }).join('');

        html += '</ul>';

        $('.viewOrderList', page).html(html).trigger('create');
    }

    function loadForm(page, user, hideMsg) {

        $('#chkDisplayMissingEpisodes', page).checked(user.Configuration.DisplayMissingEpisodes || false).checkboxradio("refresh");
        $('#chkDisplayUnairedEpisodes', page).checked(user.Configuration.DisplayUnairedEpisodes || false).checkboxradio("refresh");

        $('#chkDisplayTrailersWithinMovieSuggestions', page).checked(user.Configuration.IncludeTrailersInSuggestions || false).checkboxradio("refresh");

        $('#chkGroupMoviesIntoCollections', page).checked(user.Configuration.GroupMoviesIntoBoxSets || false).checkboxradio("refresh");
        $('#chkDisplayCollectionView', page).checked(user.Configuration.DisplayCollectionsView || false).checkboxradio("refresh");
        $('#chkDisplayFolderView', page).checked(user.Configuration.DisplayFoldersView || false).checkboxradio("refresh");

        var promise1 = ApiClient.getItems(user.Id, {
            sortBy: "SortName"
        });
        var promise2 = ApiClient.getJSON(ApiClient.getUrl("Channels", {
            UserId: user.Id
        }));
        var promise3 = ApiClient.getUserViews(user.Id);

        $.when(promise1, promise2, promise3).done(function (r1, r2, r3) {

            renderViews(page, user, r1[0]);
            renderLatestItems(page, user, r1[0]);
            renderChannels(page, user, r2[0]);
            renderViewOrder(page, user, r3[0]);

            if (hideMsg !== false) {
                Dashboard.hideLoadingMsg();
            }
        });
    }

    function saveUser(page, user) {

        user.Configuration.DisplayMissingEpisodes = $('#chkDisplayMissingEpisodes', page).checked();
        user.Configuration.DisplayUnairedEpisodes = $('#chkDisplayUnairedEpisodes', page).checked();
        user.Configuration.GroupMoviesIntoBoxSets = $('#chkGroupMoviesIntoCollections', page).checked();

        user.Configuration.DisplayCollectionsView = $('#chkDisplayCollectionView', page).checked();
        user.Configuration.DisplayFoldersView = $('#chkDisplayFolderView', page).checked();

        user.Configuration.IncludeTrailersInSuggestions = $('#chkDisplayTrailersWithinMovieSuggestions', page).checked();

        user.Configuration.LatestItemsExcludes = $(".chkIncludeInLatest:not(:checked)", page).get().map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.ExcludeFoldersFromGrouping = $(".chkGroupFolder:not(:checked)", page).get().map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.DisplayChannelsWithinViews = $(".chkGroupChannel:checked", page).get().map(function (i) {

            return i.getAttribute('data-channelid');
        });

        user.Configuration.OrderedViews = $(".viewItem", page).get().map(function (i) {

            return i.getAttribute('data-viewid');
        });

        ApiClient.updateUserConfiguration(user.Id, user.Configuration).done(function () {
            Dashboard.alert(Globalize.translate('SettingsSaved'));

            loadForm(page, user, false);

        });
    }

    function onSubmit() {

        var page = $(this).parents('.page');

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            saveUser(page, user);

        });

        // Disable default form submission
        return false;

    }

    $(document).on('pageinit', "#displayPreferencesPage", function () {

        var page = this;

        $('.viewOrderList', page).on('click', '.btnViewItemMove', function () {

            var li = $(this).parents('.viewItem');
            var ul = li.parents('ul');

            if ($(this).hasClass('btnViewItemDown')) {

                var next = li.next();

                li.remove().insertAfter(next);

            } else {

                var prev = li.prev();

                li.remove().insertBefore(prev);
            }

            $('.viewItem', ul).each(function () {

                if ($(this).prev('.viewItem').length) {
                    $('.btnViewItemMove', this).addClass('btnViewItemUp').removeClass('btnViewItemDown').attr('data-icon', 'arrow-u').removeClass('ui-icon-arrow-d').addClass('ui-icon-arrow-u');
                } else {
                    $('.btnViewItemMove', this).addClass('btnViewItemDown').removeClass('btnViewItemUp').attr('data-icon', 'arrow-d').removeClass('ui-icon-arrow-u').addClass('ui-icon-arrow-d');
                }

            });

            ul.listview('destroy').listview({});
        });

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
        $('.lnkMyProfile', page).attr('href', 'myprofile.html?userId=' + userId);
    });

    window.DisplayPreferencesPage = {
        onSubmit: onSubmit
    };

})(jQuery, window, document);