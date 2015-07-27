(function ($, window, document) {

    function renderViews(page, user, result) {

        var folderHtml = '';

        folderHtml += '<div data-role="controlgroup">';
        folderHtml += result.Items.map(function (i) {

            var currentHtml = '';

            var id = 'chkGroupFolder' + i.Id;

            currentHtml += '<label for="' + id + '">' + i.Name + '</label>';

            var isChecked = (user.Configuration.ExcludeFoldersFromGrouping != null && user.Configuration.ExcludeFoldersFromGrouping.indexOf(i.Id) == -1) ||
                user.Configuration.GroupedFolders.indexOf(i.Id) != -1;

            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<input class="chkGroupFolder" data-folderid="' + i.Id + '" type="checkbox" id="' + id + '"' + checkedHtml + ' />';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        $('.folderGroupList', page).html(folderHtml).trigger('create');
    }

    function renderViewStyles(page, user, result) {

        var folderHtml = '';

        folderHtml += '<div data-role="controlgroup">';
        folderHtml += result.map(function (i) {

            var currentHtml = '';

            var id = 'chkPlainFolder' + i.Id;

            currentHtml += '<label for="' + id + '">' + i.Name + '</label>';

            var isChecked = user.Configuration.PlainFolderViews.indexOf(i.Id) == -1;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<input class="chkPlainFolder" data-folderid="' + i.Id + '" type="checkbox" id="' + id + '"' + checkedHtml + ' />';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        $('.viewStylesList', page).html(folderHtml).trigger('create');

        if (result.length) {
            $('.viewStylesSection', page).show();
        } else {
            $('.viewStylesSection', page).hide();
        }
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

    function loadForm(page, user, displayPreferences) {

        $('#chkDisplayCollectionView', page).checked(user.Configuration.DisplayCollectionsView || false).checkboxradio("refresh");

        $('#chkHidePlayedFromLatest', page).checked(user.Configuration.HidePlayedInLatest || false).checkboxradio("refresh");

        $('#selectHomeSection1', page).val(displayPreferences.CustomPrefs.home0 || '').selectmenu("refresh");
        $('#selectHomeSection2', page).val(displayPreferences.CustomPrefs.home1 || '').selectmenu("refresh");
        $('#selectHomeSection3', page).val(displayPreferences.CustomPrefs.home2 || '').selectmenu("refresh");
        $('#selectHomeSection4', page).val(displayPreferences.CustomPrefs.home3 || '').selectmenu("refresh");

        $('#chkEnableLibraryTileNames', page).checked(displayPreferences.CustomPrefs.enableLibraryTileNames != '0').checkboxradio("refresh");

        var promise1 = ApiClient.getItems(user.Id, {
            sortBy: "SortName"
        });
        var promise2 = ApiClient.getJSON(ApiClient.getUrl("Channels", {
            UserId: user.Id
        }));
        var promise3 = ApiClient.getUserViews(user.Id);
        var promise4 = ApiClient.getJSON(ApiClient.getUrl("Users/" + user.Id + "/SpecialViewOptions"));

        $.when(promise1, promise2, promise3, promise4).done(function (r1, r2, r3, r4) {

            renderViews(page, user, r1[0]);
            renderLatestItems(page, user, r1[0]);
            renderChannels(page, user, r2[0]);
            renderViewOrder(page, user, r3[0]);
            renderViewStyles(page, user, r4[0]);

            Dashboard.hideLoadingMsg();
        });
    }

    function saveUser(page, user, displayPreferences) {

        user.Configuration.DisplayCollectionsView = $('#chkDisplayCollectionView', page).checked();

        user.Configuration.HidePlayedInLatest = $('#chkHidePlayedFromLatest', page).checked();

        user.Configuration.LatestItemsExcludes = $(".chkIncludeInLatest:not(:checked)", page).get().map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.ExcludeFoldersFromGrouping = null;

        user.Configuration.GroupedFolders = $(".chkGroupFolder:checked", page).get().map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.PlainFolderViews = $(".chkPlainFolder:not(:checked)", page).get().map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.DisplayChannelsWithinViews = $(".chkGroupChannel:checked", page).get().map(function (i) {

            return i.getAttribute('data-channelid');
        });

        user.Configuration.OrderedViews = $(".viewItem", page).get().map(function (i) {

            return i.getAttribute('data-viewid');
        });

        displayPreferences.CustomPrefs.home0 = $('#selectHomeSection1', page).val();
        displayPreferences.CustomPrefs.home1 = $('#selectHomeSection2', page).val();
        displayPreferences.CustomPrefs.home2 = $('#selectHomeSection3', page).val();
        displayPreferences.CustomPrefs.home3 = $('#selectHomeSection4', page).val();
        displayPreferences.CustomPrefs.enableLibraryTileNames = $('#chkEnableLibraryTileNames', page).checked() ? '1' : '0';

        ApiClient.updateDisplayPreferences('home', displayPreferences, user.Id, AppSettings.displayPreferencesKey()).done(function() {
            
            ApiClient.updateUserConfiguration(user.Id, user.Configuration).done(function () {
                Dashboard.alert(Globalize.translate('SettingsSaved'));

                loadForm(page, user, displayPreferences);
            });
        });
    }

    function onSubmit() {

        var page = $(this).parents('.page');

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            ApiClient.getDisplayPreferences('home', user.Id, AppSettings.displayPreferencesKey()).done(function (displayPreferences) {

                saveUser(page, user, displayPreferences);

            });

        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinitdepends', "#homeScreenPreferencesPage", function () {

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

        $('.homeScreenPreferencesForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshowready', "#homeScreenPreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            ApiClient.getDisplayPreferences('home', user.Id, AppSettings.displayPreferencesKey()).done(function (result) {

                loadForm(page, user, result);

            });
        });
    });

})(jQuery, window, document);