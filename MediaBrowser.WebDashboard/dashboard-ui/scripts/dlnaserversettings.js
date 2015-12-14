(function ($, document, window) {

    function loadPage(page, config, users) {

        $('#chkEnableServer', page).checked(config.EnableServer).checkboxradio("refresh");
        $('#chkBlastAliveMessages', page).checked(config.BlastAliveMessages).checkboxradio("refresh");
        $('#txtBlastInterval', page).val(config.BlastAliveMessageIntervalSeconds);

        $('#chkEnableMovieFolders', page).checked(config.EnableMovieFolders).checkboxradio("refresh");

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

    $(document).on('pageinit', "#dlnaServerSettingsPage", function () {

        $('.dlnaServerSettingsForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#dlnaServerSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getNamedConfiguration("dlna");
        var promise2 = ApiClient.getUsers();

        Promise.all([promise1, promise2]).then(function (responses) {

            loadPage(page, responses[0], responses[1]);

        });

    });

})(jQuery, document, window);
