define(['jQuery'], function ($) {

    function loadPage(page, config) {

        $('#txtRemoteClientBitrateLimit', page).val((config.RemoteClientBitrateLimit / 1000000) || '');

        ApiClient.getNamedConfiguration("channels").then(function (channelConfig) {

            $('#selectChannelResolution', page).val(channelConfig.PreferredStreamingWidth || '');

        });

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().then(function (config) {

            config.RemoteClientBitrateLimit = parseInt(parseFloat(($('#txtRemoteClientBitrateLimit', form).val() || '0')) * 1000000);

            ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        ApiClient.getNamedConfiguration("channels").then(function (config) {

            // This should be null if empty
            config.PreferredStreamingWidth = $('#selectChannelResolution', form).val() || null;

            ApiClient.updateNamedConfiguration("channels", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    function getTabs() {
        return [
        {
            href: 'cinemamodeconfiguration.html',
            name: Globalize.translate('TabCinemaMode')
        },
         {
             href: 'playbackconfiguration.html',
             name: Globalize.translate('TabResumeSettings')
         },
         {
             href: 'streamingsettings.html',
             name: Globalize.translate('TabStreaming')
         }];
    }

    $(document).on('pageinit', "#streamingSettingsPage", function () {

        var page = this;

        $('#btnSelectTranscodingTempPath', page).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    callback: function (path) {

                        if (path) {
                            $('#txtTranscodingTempPath', page).val(path);
                        }
                        picker.close();
                    },

                    header: Globalize.translate('HeaderSelectTranscodingPath'),

                    instruction: Globalize.translate('HeaderSelectTranscodingPathHelp')
                });
            });
        });

        $('.streamingSettingsForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#streamingSettingsPage", function () {

        Dashboard.showLoadingMsg();

        LibraryMenu.setTabs('playback', 2, getTabs);
        var page = this;

        ApiClient.getServerConfiguration().then(function (config) {

            loadPage(page, config);

        });
    });

});
