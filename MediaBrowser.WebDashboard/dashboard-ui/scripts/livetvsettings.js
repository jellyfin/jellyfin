(function ($, document, window) {

    function loadPage(page, config, liveTvInfo) {

        if (liveTvInfo.IsEnabled) {

            $('.liveTvSettingsForm', page).show();
            $('.noLiveTvServices', page).hide();

        } else {
            $('.liveTvSettingsForm', page).hide();
            $('.noLiveTvServices', page).show();
        }

        $('#selectGuideDays', page).val(config.GuideDays || '').selectmenu('refresh');

        var serviceOptions = liveTvInfo.Services.map(function (s) {
            return '<option value="' + s.Name + '">' + s.Name + '</option>';
        });

        $('#selectActiveService', page).html(serviceOptions).val(config.ActiveService || '').selectmenu('refresh');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#liveTvSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getNamedConfiguration("livetv");

        var promise2 = ApiClient.getLiveTvInfo();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

    });

    window.LiveTvSettingsPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getNamedConfiguration("livetv").done(function (config) {


                config.GuideDays = $('#selectGuideDays', form).val() || null;
                config.ActiveService = $('#selectActiveService', form).val() || null;

                ApiClient.updateNamedConfiguration("livetv", config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
