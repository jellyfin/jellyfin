(function ($, document, window) {

    function loadPage(page, config, users) {

        $('#chkEnableServer', page).checked(config.DlnaOptions.EnableServer).checkboxradio("refresh");
        $('#chkBlastAliveMessages', page).checked(config.DlnaOptions.BlastAliveMessages).checkboxradio("refresh");
        $('#txtBlastInterval', page).val(config.DlnaOptions.BlastAliveMessageIntervalSeconds);

        var usersHtml = users.map(function (u) {
            return '<option value="' + u.Id + '">' + u.Name + '</option>';
        }).join('');

        $('#selectUser', page).html(usersHtml).val(config.DlnaOptions.DefaultUserId || '').selectmenu("refresh");

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#dlnaServerSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();
        var promise2 = ApiClient.getUsers();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

    });

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().done(function (config) {

            config.DlnaOptions.EnableServer = $('#chkEnableServer', form).checked();
            config.DlnaOptions.BlastAliveMessages = $('#chkBlastAliveMessages', form).checked();
            config.DlnaOptions.BlastAliveMessageIntervalSeconds = $('#txtBlastInterval', form).val();
            config.DlnaOptions.DefaultUserId = $('#selectUser', form).val();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    window.DlnaServerSettingsPage = {
        onSubmit: onSubmit
    };

})(jQuery, document, window);
