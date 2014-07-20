var AdvancedMetadataConfigurationPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (configuration) {

            AdvancedMetadataConfigurationPage.load(page, configuration);

        });
    },

    onPageInit: function () {

        var page = this;

        $('#btnSelectMetadataPath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtMetadataPath', page).val(path);
                    }
                    picker.close();
                },

                header: Globalize.translate('HeaderSelectMetadataPath'),

                instruction: Globalize.translate('HeaderSelectMetadataPathHelp')
            });
        });
    },

    load: function (page, config) {

        $('#chkEnableTmdbPersonUpdates', page).checked(config.EnableTmdbUpdates).checkboxradio("refresh");
        $('#chkEnableTvdbUpdates', page).checked(config.EnableTvDbUpdates).checkboxradio("refresh");
        $('#chkEnableFanartUpdates', page).checked(config.EnableFanArtUpdates).checkboxradio("refresh");
        $('#txtMetadataPath', page).val(config.MetadataPath || '');

        Dashboard.hideLoadingMsg();
    },

    onSubmit: function () {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.EnableTvDbUpdates = $('#chkEnableTvdbUpdates', form).checked();
            config.EnableTmdbUpdates = $('#chkEnableTmdbPersonUpdates', form).checked();
            config.EnableFanArtUpdates = $('#chkEnableFanartUpdates', form).checked();
            config.MetadataPath = $('#txtMetadataPath', form).val();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageinit', "#advancedMetadataConfigurationPage", AdvancedMetadataConfigurationPage.onPageInit).on('pageshow', "#advancedMetadataConfigurationPage", AdvancedMetadataConfigurationPage.onPageShow);