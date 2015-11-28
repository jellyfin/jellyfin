(function ($, window, document) {

    function renderViews(page, user, result) {

        var folderHtml = '';

        folderHtml += '<div class="paperCheckboxList">';
        folderHtml += result.map(function (i) {

            var currentHtml = '';

            var id = 'chkGroupFolder' + i.Id;

            var isChecked = (user.Configuration.ExcludeFoldersFromGrouping != null && user.Configuration.ExcludeFoldersFromGrouping.indexOf(i.Id) == -1) ||
                user.Configuration.GroupedFolders.indexOf(i.Id) != -1;

            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<paper-checkbox class="chkGroupFolder" data-folderid="' + i.Id + '" id="' + id + '"' + checkedHtml + '>' + i.Name + '</paper-checkbox>';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        $('.folderGroupList', page).html(folderHtml);
    }

    function renderViewStyles(page, user, result) {

        var folderHtml = '';

        folderHtml += '<div class="paperCheckboxList">';
        folderHtml += result.map(function (i) {

            var currentHtml = '';

            var id = 'chkPlainFolder' + i.Id;

            var isChecked = user.Configuration.PlainFolderViews.indexOf(i.Id) == -1;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<paper-checkbox class="chkPlainFolder" data-folderid="' + i.Id + '" id="' + id + '"' + checkedHtml + '>' + i.Name + '</paper-checkbox>';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        $('.viewStylesList', page).html(folderHtml);

        if (result.length) {
            $('.viewStylesSection', page).show();
        } else {
            $('.viewStylesSection', page).hide();
        }
    }

    function renderLatestItems(page, user, result) {

        var folderHtml = '';

        folderHtml += '<div class="paperCheckboxList">';
        folderHtml += result.Items.map(function (i) {

            var currentHtml = '';

            var id = 'chkIncludeInLatest' + i.Id;

            var isChecked = user.Configuration.LatestItemsExcludes.indexOf(i.Id) == -1;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<paper-checkbox class="chkIncludeInLatest" data-folderid="' + i.Id + '" id="' + id + '"' + checkedHtml + '>' + i.Name + '</paper-checkbox>';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        $('.latestItemsList', page).html(folderHtml);
    }

    function renderViewOrder(page, user, result) {

        var html = '';

        var index = 0;

        html += result.Items.map(function (view) {

            var currentHtml = '';

            currentHtml += '<paper-icon-item class="viewItem" data-viewid="' + view.Id + '">';

            currentHtml += '<paper-fab mini style="background-color:#444;" icon="folder-open" item-icon></paper-fab>';

            currentHtml += '<paper-item-body>';

            currentHtml += '<div>';
            currentHtml += view.Name;
            currentHtml += '</div>';

            currentHtml += '</paper-item-body>';

            if (index > 0) {

                currentHtml += '<paper-icon-button icon="keyboard-arrow-up" class="btnViewItemUp btnViewItemMove" title="' + Globalize.translate('ButtonUp') + '"></paper-icon-button>';
            }
            else if (result.Items.length > 1) {

                currentHtml += '<paper-icon-button icon="keyboard-arrow-down" class="btnViewItemDown btnViewItemMove" title="' + Globalize.translate('ButtonDown') + '"></paper-icon-button>';
            }


            currentHtml += '</paper-icon-item>';

            index++;
            return currentHtml;

        }).join('');

        $('.viewOrderList', page).html(html);
    }

    function loadForm(page, user, displayPreferences) {

        page.querySelector('.chkDisplayCollectionView').checked = user.Configuration.DisplayCollectionsView || false;
        page.querySelector('.chkHidePlayedFromLatest').checked = user.Configuration.HidePlayedInLatest || false;
        page.querySelector('.chkDisplayChannelsInline').checked = user.Configuration.DisplayChannelsInline || false;

        $('#selectHomeSection1', page).val(displayPreferences.CustomPrefs.home0 || '');
        $('#selectHomeSection2', page).val(displayPreferences.CustomPrefs.home1 || '');
        $('#selectHomeSection3', page).val(displayPreferences.CustomPrefs.home2 || '');
        $('#selectHomeSection4', page).val(displayPreferences.CustomPrefs.home3 || '');

        var promise1 = ApiClient.getItems(user.Id, {
            sortBy: "SortName"
        });
        var promise2 = ApiClient.getUserViews({}, user.Id);
        var promise3 = ApiClient.getJSON(ApiClient.getUrl("Users/" + user.Id + "/SpecialViewOptions"));
        var promise4 = ApiClient.getJSON(ApiClient.getUrl("Users/" + user.Id + "/GroupingOptions"));

        Promise.all([promise1, promise2, promise3, promise4]).then(function (responses) {

            renderViews(page, user, responses[3]);
            renderLatestItems(page, user, responses[0]);
            renderViewOrder(page, user, responses[1]);
            renderViewStyles(page, user, responses[2]);

            Dashboard.hideLoadingMsg();
        });
    }

    function saveUser(page, user, displayPreferences) {

        user.Configuration.DisplayCollectionsView = page.querySelector('.chkDisplayCollectionView').checked;
        user.Configuration.HidePlayedInLatest = page.querySelector('.chkHidePlayedFromLatest').checked;

        user.Configuration.DisplayChannelsInline = page.querySelector('.chkDisplayChannelsInline').checked;

        user.Configuration.LatestItemsExcludes = $(".chkIncludeInLatest", page).get().filter(function (i) {

            return !i.checked;

        }).map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.ExcludeFoldersFromGrouping = null;

        user.Configuration.GroupedFolders = $(".chkGroupFolder", page).get().filter(function (i) {

            return i.checked;

        }).map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.PlainFolderViews = $(".chkPlainFolder", page).get().filter(function (i) {

            return !i.checked;

        }).map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.OrderedViews = $(".viewItem", page).get().map(function (i) {

            return i.getAttribute('data-viewid');
        });

        displayPreferences.CustomPrefs.home0 = $('#selectHomeSection1', page).val();
        displayPreferences.CustomPrefs.home1 = $('#selectHomeSection2', page).val();
        displayPreferences.CustomPrefs.home2 = $('#selectHomeSection3', page).val();
        displayPreferences.CustomPrefs.home3 = $('#selectHomeSection4', page).val();

        ApiClient.updateDisplayPreferences('home', displayPreferences, user.Id, AppSettings.displayPreferencesKey()).then(function () {

            ApiClient.updateUserConfiguration(user.Id, user.Configuration).then(function () {
                Dashboard.alert(Globalize.translate('SettingsSaved'));

                loadForm(page, user, displayPreferences);
            });
        });
    }

    function onSubmit() {

        var page = $(this).parents('.page')[0];

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).then(function (user) {

            ApiClient.getDisplayPreferences('home', user.Id, AppSettings.displayPreferencesKey()).then(function (displayPreferences) {

                saveUser(page, user, displayPreferences);

            });

        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#homeScreenPreferencesPage", function () {

        var page = this;

        $('.viewOrderList', page).on('click', '.btnViewItemMove', function () {

            var li = $(this).parents('.viewItem');
            var ul = li.parents('.paperList');

            if ($(this).hasClass('btnViewItemDown')) {

                var next = li.next();

                li.remove().insertAfter(next);

            } else {

                var prev = li.prev();

                li.remove().insertBefore(prev);
            }

            $('.viewItem', ul).each(function () {

                var btn = $('.btnViewItemMove', this)[0];

                if ($(this).prev('.viewItem').length) {

                    btn.classList.add('btnViewItemUp');
                    btn.classList.remove('btnViewItemDown');
                    btn.icon = 'keyboard-arrow-up';
                } else {

                    btn.classList.remove('btnViewItemUp');
                    btn.classList.add('btnViewItemDown');
                    btn.icon = 'keyboard-arrow-down';
                }

            });
        });

        $('.homeScreenPreferencesForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#homeScreenPreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).then(function (user) {

            ApiClient.getDisplayPreferences('home', user.Id, AppSettings.displayPreferencesKey()).then(function (result) {

                loadForm(page, user, result);

            });
        });
    });

})(jQuery, window, document);