define(['jQuery'], function ($) {

    function loadMediaFolders(page, mediaFolders) {

        var html = '';

        html += '<div class="paperListLabel">' + Globalize.translate('HeaderLibraries') + '</div>';

        html += '<div class="paperCheckboxList paperList" style="padding:.5em 1em;">';

        for (var i = 0, length = mediaFolders.length; i < length; i++) {

            var folder = mediaFolders[i];

            var checkedAttribute = ' checked="checked"';

            html += '<paper-checkbox class="chkFolder" data-id="' + folder.Id + '"' + checkedAttribute + '>' + folder.Name + '</paper-checkbox>';
        }

        html += '</div>';

        $('.folderAccess', page).html(html).trigger('create');

        $('#chkEnableAllFolders', page).checked(true).trigger('change');
    }

    function loadChannels(page, channels) {

        var html = '';

        html += '<div class="paperListLabel">' + Globalize.translate('HeaderChannels') + '</div>';

        html += '<div class="paperCheckboxList paperList" style="padding:.5em 1em;">';

        for (var i = 0, length = channels.length; i < length; i++) {

            var folder = channels[i];

            var checkedAttribute = ' checked="checked"';

            html += '<paper-checkbox class="chkChannel" data-id="' + folder.Id + '"' + checkedAttribute + '>' + folder.Name + '</paper-checkbox>';
        }

        html += '</div>';

        $('.channelAccess', page).show().html(html).trigger('create');

        if (channels.length) {
            $('.channelAccessContainer', page).show();
        } else {
            $('.channelAccessContainer', page).hide();
        }

        $('#chkEnableAllChannels', page).checked(true).trigger('change');
    }

    function loadUser(page) {

        $('#txtUserName', page).val('');

        Dashboard.showLoadingMsg();

        var promise4 = ApiClient.getJSON(ApiClient.getUrl("Library/MediaFolders", { IsHidden: false }));

        var promise5 = ApiClient.getJSON(ApiClient.getUrl("Channels"));

        Promise.all([promise4, promise5]).then(function (responses) {

            loadMediaFolders(page, responses[0].Items);
            loadChannels(page, responses[1].Items);

            Dashboard.hideLoadingMsg();
        });
    }

    function saveUser(page) {

        var name = $('#txtUserName', page).val();

        ApiClient.createUser(name).then(function (user) {

            user.Policy.EnableAllFolders = $('#chkEnableAllFolders', page).checked();
            user.Policy.EnabledFolders = user.Policy.EnableAllFolders ?
                [] :
                $('.chkFolder', page).get().filter(function (i) {
                    return i.checked;
                }).map(function (i) {
                    return i.getAttribute('data-id');
                });

            user.Policy.EnableAllChannels = $('#chkEnableAllChannels', page).checked();
            user.Policy.EnabledChannels = user.Policy.EnableAllChannels ?
                [] :
                $('.chkChannel', page).get().filter(function (i) {
                    return i.checked;
                }).map(function (i) {
                    return i.getAttribute('data-id');
                });

            ApiClient.updateUserPolicy(user.Id, user.Policy).then(function () {
                Dashboard.navigate("useredit.html?userId=" + user.Id);
            });

        }, function (response) {

            if (response.status == 400) {

                Dashboard.alert({
                    message: page.querySelector('.labelNewUserNameHelp').innerHTML
                });

            } else {
                require(['toast'], function (toast) {
                    toast(Globalize.translate('DefaultErrorMessage'));
                });
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function onSubmit() {
        var page = $(this).parents('.page')[0];

        Dashboard.showLoadingMsg();

        saveUser(page);

        // Disable default form submission
        return false;
    }

    function loadData(page) {

        loadUser(page);
    }

    $(document).on('pageinit', "#newUserPage", function () {

        var page = this;

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

        $('.newUserProfileForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#newUserPage", function () {

        var page = this;

        loadData(page);

    });

});