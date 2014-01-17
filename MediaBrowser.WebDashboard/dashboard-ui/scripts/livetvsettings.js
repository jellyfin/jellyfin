(function ($, document, window) {

    function loadPage(page, config, liveTvInfo) {

        if (liveTvInfo.IsEnabled) {

            $('.liveTvSettingsForm', page).show();
            $('.noLiveTvServices', page).hide();

        } else {
            $('.liveTvSettingsForm', page).hide();
            $('.noLiveTvServices', page).show();
        }

        $('#selectGuideDays', page).val(config.LiveTvOptions.GuideDays || '').selectmenu('refresh');

        var serviceOptions = liveTvInfo.Services.map(function (s) {
            return '<option value="' + s.Name + '">' + s.Name + '</option>';
        });

        $('#selectActiveService', page).html(serviceOptions).selectmenu('refresh');

        var service = liveTvInfo.Services.filter(function (s) {
            return s.Name == liveTvInfo.ActiveServiceName;
            
        })[0] || {};

        if (service.HomePageUrl) {
            $('#activeServiceName', page).html('<a href="' + service.HomePageUrl + '" target="_blank">' + liveTvInfo.ActiveServiceName + '</a>').trigger('create');
        } else {
            $('#activeServiceName', page).html(liveTvInfo.ActiveServiceName);
        }

        var versionHtml = service.Version || 'Unknown'; 
        
        if (service.HasUpdateAvailable) {
            versionHtml += ' <span style="color:green; margin-left: .25em;">(Update available)</span>';
        } else {
            versionHtml += '<img src="css/images/checkmarkgreen.png" style="height: 17px; margin-left: 10px; margin-right: 0; position: relative; top: 4px;" /> Up to date!';
        }
        $('#activeServiceVersion', page).html(versionHtml);

        var status = liveTvInfo.Status;

        if (status != 'Ok') {

            if (liveTvInfo.StatusMessage) {
                status += ' (' + liveTvInfo.StatusMessage + ')';
            }
            status = '<span style="color:red;">' + status + '</span>';
        }
        $('#activeServiceStatus', page).html(status);

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#liveTvSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();

        var promise2 = ApiClient.getLiveTvInfo();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

    });

    window.LiveTvSettingsPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {


                config.LiveTvOptions.GuideDays = $('#selectGuideDays', form).val() || null;

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
