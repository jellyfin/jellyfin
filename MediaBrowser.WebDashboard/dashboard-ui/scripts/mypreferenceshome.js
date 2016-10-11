define(['listViewStyle'], function () {

    function renderViews(page, user, result) {

        var folderHtml = '';

        folderHtml += '<div class="checkboxList">';
        folderHtml += result.map(function (i) {

            var currentHtml = '';

            var id = 'chkGroupFolder' + i.Id;

            var isChecked = (user.Configuration.ExcludeFoldersFromGrouping != null && user.Configuration.ExcludeFoldersFromGrouping.indexOf(i.Id) == -1) ||
                user.Configuration.GroupedFolders.indexOf(i.Id) != -1;

            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<label>';
            currentHtml += '<input type="checkbox" is="emby-checkbox" class="chkGroupFolder" data-folderid="' + i.Id + '" id="' + id + '"' + checkedHtml + '/>';
            currentHtml += '<span>' + i.Name + '</span>';
            currentHtml += '</label>';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        page.querySelector('.folderGroupList').innerHTML = folderHtml;
    }

    function renderLatestItems(page, user, result) {

        var folderHtml = '';

        folderHtml += '<div class="checkboxList">';
        folderHtml += result.Items.map(function (i) {

            var currentHtml = '';

            var id = 'chkIncludeInLatest' + i.Id;

            var isChecked = user.Configuration.LatestItemsExcludes.indexOf(i.Id) == -1;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<label>';
            currentHtml += '<input type="checkbox" is="emby-checkbox" class="chkIncludeInLatest" data-folderid="' + i.Id + '" id="' + id + '"' + checkedHtml + '/>';
            currentHtml += '<span>' + i.Name + '</span>';
            currentHtml += '</label>';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        page.querySelector('.latestItemsList').innerHTML = folderHtml;
    }

    function renderViewOrder(page, user, result) {

        var html = '';

        var index = 0;

        html += result.Items.map(function (view) {

            var currentHtml = '';

            currentHtml += '<div class="listItem viewItem" data-viewid="' + view.Id + '">';

            currentHtml += '<button type="button" is="emby-button" class="fab mini autoSize" item-icon><i class="md-icon">folder_open</i></button>';

            currentHtml += '<div class="listItemBody">';

            currentHtml += '<div>';
            currentHtml += view.Name;
            currentHtml += '</div>';

            currentHtml += '</div>';

            if (index > 0) {

                currentHtml += '<button type="button" is="paper-icon-button-light" class="btnViewItemUp btnViewItemMove autoSize" title="' + Globalize.translate('ButtonUp') + '"><i class="md-icon">keyboard_arrow_up</i></button>';
            }
            else if (result.Items.length > 1) {

                currentHtml += '<button type="button" is="paper-icon-button-light" class="btnViewItemDown btnViewItemMove autoSize" title="' + Globalize.translate('ButtonDown') + '"><i class="md-icon">keyboard_arrow_down</i></button>';
            }


            currentHtml += '</div>';

            index++;
            return currentHtml;

        }).join('');

        page.querySelector('.viewOrderList').innerHTML = html;
    }

    function loadForm(page, user, displayPreferences) {

        page.querySelector('.chkHidePlayedFromLatest').checked = user.Configuration.HidePlayedInLatest || false;

        page.querySelector('#selectHomeSection1').value = displayPreferences.CustomPrefs.home0 || '';
        page.querySelector('#selectHomeSection2').value = displayPreferences.CustomPrefs.home1 || '';
        page.querySelector('#selectHomeSection3').value = displayPreferences.CustomPrefs.home2 || '';
        page.querySelector('#selectHomeSection4').value = displayPreferences.CustomPrefs.home3 || '';

        var promise1 = ApiClient.getItems(user.Id, {
            sortBy: "SortName"
        });
        var promise2 = ApiClient.getUserViews({}, user.Id);
        var promise3 = ApiClient.getJSON(ApiClient.getUrl("Users/" + user.Id + "/GroupingOptions"));

        Promise.all([promise1, promise2, promise3]).then(function (responses) {

            renderViews(page, user, responses[2]);
            renderLatestItems(page, user, responses[0]);
            renderViewOrder(page, user, responses[1]);

            Dashboard.hideLoadingMsg();
        });
    }

    function displayPreferencesKey() {
        if (AppInfo.isNativeApp) {
            return 'Emby Mobile';
        }

        return 'webclient';
    }

    function getCheckboxItems(selector, page, isChecked) {

        var inputs = page.querySelectorAll(selector);
        var list = [];

        for (var i = 0, length = inputs.length; i < length; i++) {

            if (inputs[i].checked == isChecked) {
                list.push(inputs[i]);
            }

        }

        return list;
    }

    function saveUser(page, user, displayPreferences) {

        user.Configuration.HidePlayedInLatest = page.querySelector('.chkHidePlayedFromLatest').checked;

        user.Configuration.LatestItemsExcludes = getCheckboxItems(".chkIncludeInLatest", page, false).map(function (i) {

            return i.getAttribute('data-folderid');
        });

        user.Configuration.ExcludeFoldersFromGrouping = null;

        user.Configuration.GroupedFolders = getCheckboxItems(".chkGroupFolder", page, true).map(function (i) {

            return i.getAttribute('data-folderid');
        });

        var viewItems = page.querySelectorAll('.viewItem');
        var orderedViews = [];
        for (var i = 0, length = viewItems.length; i < length; i++) {
            orderedViews.push(viewItems[i].getAttribute('data-viewid'));
        }

        user.Configuration.OrderedViews = orderedViews;

        displayPreferences.CustomPrefs.home0 = page.querySelector('#selectHomeSection1').value;
        displayPreferences.CustomPrefs.home1 = page.querySelector('#selectHomeSection2').value;
        displayPreferences.CustomPrefs.home2 = page.querySelector('#selectHomeSection3').value;
        displayPreferences.CustomPrefs.home3 = page.querySelector('#selectHomeSection4').value;

        return ApiClient.updateDisplayPreferences('home', displayPreferences, user.Id, displayPreferencesKey()).then(function () {

            return ApiClient.updateUserConfiguration(user.Id, user.Configuration);
        });
    }

    function save(page, userId) {

        Dashboard.showLoadingMsg();

        if (!AppInfo.enableAutoSave) {
            Dashboard.showLoadingMsg();
        }

        ApiClient.getUser(userId).then(function (user) {

            ApiClient.getDisplayPreferences('home', user.Id, displayPreferencesKey()).then(function (displayPreferences) {

                saveUser(page, user, displayPreferences).then(function () {

                    Dashboard.hideLoadingMsg();
                    if (!AppInfo.enableAutoSave) {
                        require(['toast'], function (toast) {
                            toast(Globalize.translate('SettingsSaved'));
                        });
                    }

                }, function () {
                    Dashboard.hideLoadingMsg();
                });

            });
        });
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function getSibling(elem, type, className) {

        var sibling = elem[type];

        while (sibling != null) {
            if (sibling.classList.contains(className)) {
                break;
            }
        }

        if (sibling != null) {
            if (!sibling.classList.contains(className)) {
                sibling = null;
            }
        }

        return sibling;
    }

    return function (view, params) {

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        function onSubmit(e) {

            save(view, userId);

            // Disable default form submission
            e.preventDefault();
            return false;
        }

        view.querySelector('.viewOrderList').addEventListener('click', function (e) {

            var target = parentWithClass(e.target, 'btnViewItemMove');

            var li = parentWithClass(target, 'viewItem');
            var ul = parentWithClass(li, 'paperList');

            if (target.classList.contains('btnViewItemDown')) {

                var next = li.nextSibling;

                li.parentNode.removeChild(li);
                next.parentNode.insertBefore(li, next.nextSibling);

            } else {

                var prev = li.previousSibling;

                li.parentNode.removeChild(li);
                prev.parentNode.insertBefore(li, prev);
            }

            var viewItems = ul.querySelectorAll('.viewItem');
            for (var i = 0, length = viewItems.length; i < length; i++) {
                var viewItem = viewItems[i];

                var btn = viewItem.querySelector('.btnViewItemMove');

                var prevViewItem = getSibling(viewItem, 'previousSibling', 'viewItem');

                if (prevViewItem) {

                    btn.classList.add('btnViewItemUp');
                    btn.classList.remove('btnViewItemDown');
                    btn.icon = 'keyboard-arrow-up';
                } else {

                    btn.classList.remove('btnViewItemUp');
                    btn.classList.add('btnViewItemDown');
                    btn.icon = 'keyboard-arrow-down';
                }
            }
        });

        view.querySelector('.homeScreenPreferencesForm').addEventListener('submit', onSubmit);

        if (AppInfo.enableAutoSave) {
            view.querySelector('.btnSave').classList.add('hide');
        } else {
            view.querySelector('.btnSave').classList.remove('hide');
        }

        view.addEventListener('viewshow', function () {
            var page = this;

            Dashboard.showLoadingMsg();

            var userId = params.userId || Dashboard.getCurrentUserId();

            ApiClient.getUser(userId).then(function (user) {

                ApiClient.getDisplayPreferences('home', user.Id, displayPreferencesKey()).then(function (result) {

                    loadForm(page, user, result);

                });
            });
        });

        view.addEventListener('viewbeforehide', function () {
            var page = this;

            if (AppInfo.enableAutoSave) {
                save(page, userId);
            }
        });
    };
});