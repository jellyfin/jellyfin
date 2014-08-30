(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtSeasonZeroName', page).val(config.SeasonZeroDisplayName);

        $('#chkEnableRealtimeMonitor', page).checked(config.EnableRealtimeMonitor).checkboxradio("refresh");

        $('#txtItemsByNamePath', page).val(config.ItemsByNamePath || '');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#librarySettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    }).on('pageinit', "#librarySettingsPage", function () {

        var page = this;

        $('#btnSelectIBNPath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtItemsByNamePath', page).val(path);
                    }
                    picker.close();
                },

                header: Globalize.translate('HeaderSelectImagesByNamePath'),

                instruction: Globalize.translate('HeaderSelectImagesByNamePathHelp')
            });
        });
    });

    function librarySettingsPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.ItemsByNamePath = $('#txtItemsByNamePath', form).val();

                config.SeasonZeroDisplayName = $('#txtSeasonZeroName', form).val();

                config.EnableRealtimeMonitor = $('#chkEnableRealtimeMonitor', form).checked();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.LibrarySettingsPage = new librarySettingsPage();

})(jQuery, document, window);
