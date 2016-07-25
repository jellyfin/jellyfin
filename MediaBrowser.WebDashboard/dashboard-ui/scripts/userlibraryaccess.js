define(['jQuery'], function ($) {

    function loadMediaFolders(page, user, mediaFolders) {

        var html = '';

        html += '<div class="paperListLabel">' + Globalize.translate('HeaderLibraries') + '</div>';

        html += '<div class="paperCheckboxList paperList">';

        for (var i = 0, length = mediaFolders.length; i < length; i++) {

            var folder = mediaFolders[i];

            var isChecked = user.Policy.EnableAllFolders || user.Policy.EnabledFolders.indexOf(folder.Id) != -1;
            var checkedAttribute = isChecked ? ' checked="checked"' : '';

            html += '<paper-checkbox class="chkFolder" data-id="' + folder.Id + '" type="checkbox"' + checkedAttribute + '>' + folder.Name + '</paper-checkbox>';
        }

        html += '</div>';

        $('.folderAccess', page).html(html).trigger('create');

        $('#chkEnableAllFolders', page).checked(user.Policy.EnableAllFolders).trigger('change');
    }

    function loadChannels(page, user, channels) {

        var html = '';

        html += '<div class="paperListLabel">' + Globalize.translate('HeaderChannels') + '</div>';

        html += '<div class="paperCheckboxList paperList">';

        for (var i = 0, length = channels.length; i < length; i++) {

            var folder = channels[i];

            var isChecked = user.Policy.EnableAllChannels || user.Policy.EnabledChannels.indexOf(folder.Id) != -1;
            var checkedAttribute = isChecked ? ' checked="checked"' : '';

            html += '<paper-checkbox class="chkChannel" data-id="' + folder.Id + '" type="checkbox"' + checkedAttribute + '>' + folder.Name + '</paper-checkbox>';
        }

        html += '</div>';

        $('.channelAccess', page).show().html(html).trigger('create');

        if (channels.length) {
            $('.channelAccessContainer', page).show();
        } else {
            $('.channelAccessContainer', page).hide();
        }

        $('#chkEnableAllChannels', page).checked(user.Policy.EnableAllChannels).trigger('change');
    }

    function loadDevices(page, user, devices) {

        var html = '';

        html += '<div class="paperListLabel">' + Globalize.translate('HeaderDevices') + '</div>';

        html += '<div class="paperCheckboxList paperList">';

        for (var i = 0, length = devices.length; i < length; i++) {

            var device = devices[i];

            var checkedAttribute = user.Policy.EnableAllDevices || user.Policy.EnabledDevices.indexOf(device.Id) != -1 ? ' checked="checked"' : '';

            html += '<paper-checkbox class="chkDevice" data-id="' + device.Id + '" type="checkbox"' + checkedAttribute + '>' + device.Name + ' - ' + device.AppName + '</paper-checkbox>';
        }

        html += '</div>';

        $('.deviceAccess', page).show().html(html).trigger('create');

        $('#chkEnableAllDevices', page).checked(user.Policy.EnableAllDevices).trigger('change');

        if (user.Policy.IsAdministrator) {
            page.querySelector('.deviceAccessContainer').classList.add('hide');
        } else {
            page.querySelector('.deviceAccessContainer').classList.remove('hide');
        }
    }

    function loadUser(page, user, loggedInUser, mediaFolders, channels, devices) {

        $(page).trigger('userloaded', [user]);

        Dashboard.setPageTitle(user.Name);

        loadChannels(page, user, channels);
        loadMediaFolders(page, user, mediaFolders);
        loadDevices(page, user, devices);

        Dashboard.hideLoadingMsg();
    }

    function onSaveComplete(page) {

        Dashboard.hideLoadingMsg();

        require(['toast'], function (toast) {
            toast(Globalize.translate('SettingsSaved'));
        });
    }

    function saveUser(user, page) {

        user.Policy.EnableAllFolders = $('#chkEnableAllFolders', page).checked();
        user.Policy.EnabledFolders = user.Policy.EnableAllFolders ?
            [] :
            $('.chkFolder', page).get().filter(function (c) {
                return c.checked;
            }).map(function (c) {
                return c.getAttribute('data-id');
            });

        user.Policy.EnableAllChannels = $('#chkEnableAllChannels', page).checked();
        user.Policy.EnabledChannels = user.Policy.EnableAllChannels ?
            [] :
            $('.chkChannel', page).get().filter(function (c) {
                return c.checked;
            }).map(function (c) {
                return c.getAttribute('data-id');
            });

        user.Policy.EnableAllDevices = $('#chkEnableAllDevices', page).checked();
        user.Policy.EnabledDevices = user.Policy.EnableAllDevices ?
            [] :
            $('.chkDevice', page).get().filter(function (c) {
                return c.checked;
            }).map(function (c) {
                return c.getAttribute('data-id');
            });

        // Legacy
        user.Policy.BlockedChannels = null;
        user.Policy.BlockedMediaFolders = null;

        ApiClient.updateUserPolicy(user.Id, user.Policy).then(function () {
            onSaveComplete(page);
        });
    }

    function onSubmit() {
        var page = $(this).parents('.page');

        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");

        ApiClient.getUser(userId).then(function (result) {
            saveUser(result, page);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#userLibraryAccessPage", function () {

        var page = this;

        $('#chkEnableAllDevices', page).on('change', function () {

            if (this.checked) {
                $('.deviceAccessListContainer', page).hide();
            } else {
                $('.deviceAccessListContainer', page).show();
            }

        });

        $('#chkEnableAllChannels', page).on('change', function () {

            if (this.checked) {
                $('.channelAccessListContainer', page).hide();
            } else {
                $('.channelAccessListContainer', page).show();
            }

        });

        $('#chkEnableAllFolders', page).on('change', function () {

            if (this.checked) {
                $('.folderAccessListContainer', page).hide();
            } else {
                $('.folderAccessListContainer', page).show();
            }

        });

        $('.userLibraryAccessForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#userLibraryAccessPage", function () {

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
        var promise6 = ApiClient.getJSON(ApiClient.getUrl('Devices', {
            SupportsPersistentIdentifier: true
        }));

        Promise.all([promise1, promise2, promise4, promise5, promise6]).then(function (responses) {

            loadUser(page, responses[0], responses[1], responses[2].Items, responses[3].Items, responses[4].Items);

        });
    });

});