(function ($, window, document) {

    function loadMediaFolders(page, mediaFolders) {

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>' + Globalize.translate('HeaderLibraries') + '</legend>';

        for (var i = 0, length = mediaFolders.length; i < length; i++) {

            var folder = mediaFolders[i];

            var id = 'mediaFolder' + i;

            var checkedAttribute = ' checked="checked"';

            html += '<input class="chkFolder" data-id="' + folder.Id + '" type="checkbox" id="' + id + '"' + checkedAttribute + ' />';
            html += '<label for="' + id + '">' + folder.Name + '</label>';
        }

        html += '</fieldset>';

        $('.folderAccess', page).html(html).trigger('create');

        $('#chkEnableAllFolders', page).checked(true).checkboxradio('refresh').trigger('change');
    }

    function loadChannels(page, channels) {

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>' + Globalize.translate('HeaderChannels') + '</legend>';

        for (var i = 0, length = channels.length; i < length; i++) {

            var folder = channels[i];

            var id = 'channels' + i;

            var checkedAttribute = ' checked="checked"';

            html += '<input class="chkChannel" data-id="' + folder.Id + '" type="checkbox" id="' + id + '"' + checkedAttribute + ' />';
            html += '<label for="' + id + '">' + folder.Name + '</label>';
        }

        html += '</fieldset>';

        $('.channelAccess', page).show().html(html).trigger('create');

        if (channels.length) {
            $('.channelAccessContainer', page).show();
        } else {
            $('.channelAccessContainer', page).hide();
        }

        $('#chkEnableAllChannels', page).checked(true).checkboxradio('refresh').trigger('change');
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
                $('.chkFolder:checked', page).map(function () {

                    return this.getAttribute('data-id');

                }).get();

            user.Policy.EnableAllChannels = $('#chkEnableAllChannels', page).checked();
            user.Policy.EnabledChannels = user.Policy.EnableAllChannels ?
                [] :
                $('.chkChannel:checked', page).map(function () {

                    return this.getAttribute('data-id');

                }).get();

            ApiClient.updateUserPolicy(user.Id, user.Policy).then(function () {
                Dashboard.navigate("useredit.html?userId=" + user.Id);
            });

        }, function () {

            Dashboard.alert(Globalize.translate('DefaultErrorMessage'));
            Dashboard.hideLoadingMsg();
        });
    }

    function onSubmit() {
        var page = $(this).parents('.page');

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

})(jQuery, window, document);