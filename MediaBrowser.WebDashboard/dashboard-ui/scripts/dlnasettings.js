define(['jQuery'], function ($) {

    function loadPage(page, config, users) {

        page.querySelector('#chkEnablePlayTo').checked = config.EnablePlayTo;
        page.querySelector('#chkEnableDlnaDebugLogging').checked = config.EnableDebugLog;

        $('#txtClientDiscoveryInterval', page).val(config.ClientDiscoveryIntervalSeconds);

        $('#chkEnableServer', page).checked(config.EnableServer);
        $('#chkBlastAliveMessages', page).checked(config.BlastAliveMessages);
        $('#txtBlastInterval', page).val(config.BlastAliveMessageIntervalSeconds);

        $('#chkEnableMovieFolders', page).checked(config.EnableMovieFolders);

        var usersHtml = users.map(function (u) {
            return '<option value="' + u.Id + '">' + u.Name + '</option>';
        }).join('');

        $('#selectUser', page).html(usersHtml).val(config.DefaultUserId || '');

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("dlna").then(function (config) {

            config.EnablePlayTo = form.querySelector('#chkEnablePlayTo').checked;
            config.EnableDebugLog = form.querySelector('#chkEnableDlnaDebugLogging').checked;

            config.ClientDiscoveryIntervalSeconds = $('#txtClientDiscoveryInterval', form).val();

            config.EnableServer = $('#chkEnableServer', form).checked();
            config.BlastAliveMessages = $('#chkBlastAliveMessages', form).checked();
            config.BlastAliveMessageIntervalSeconds = $('#txtBlastInterval', form).val();
            config.DefaultUserId = $('#selectUser', form).val();

            config.EnableMovieFolders = $('#chkEnableMovieFolders', form).checked();

            ApiClient.updateNamedConfiguration("dlna", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    function getTabs() {
        return [
        {
            href: 'dlnasettings.html',
            name: Globalize.translate('TabSettings')
        },
         {
             href: 'dlnaprofiles.html',
             name: Globalize.translate('TabProfiles')
         }];
    }

    $(document).on('pageinit', "#dlnaSettingsPage", function () {

        $('.dlnaSettingsForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#dlnaSettingsPage", function () {

        LibraryMenu.setTabs('dlna', 0, getTabs);
        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getNamedConfiguration("dlna");
        var promise2 = ApiClient.getUsers();

        Promise.all([promise1, promise2]).then(function (responses) {

            loadPage(page, responses[0], responses[1]);

        });

    });

});
