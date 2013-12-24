(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtItemsByNamePath', page).val(config.ItemsByNamePath || '');

        var customIbn = config.ItemsByNamePath ? true : false;
        $('#chkEnableCustomIBNPath', page).checked(customIbn).checkboxradio("refresh");

        if (customIbn) {
            $('#fieldEnterIBNPath', page).show();
            $('#txtItemsByNamePath', page).attr("required", "required");
        } else {
            $('#fieldEnterIBNPath', page).hide();
            $('#txtItemsByNamePath', page).removeAttr("required");
        }

        $('#txtSeasonZeroName', page).val(config.SeasonZeroDisplayName);

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#librarySettingsPage", function () {

        Dashboard.showLoadingMsg();

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

                header: "Select Images By Name Path",

                instruction: "Browse or enter the path to your items by name folder. The folder must be writeable."
            });
        });

        $('#chkEnableCustomIBNPath', page).on("change.showIBNText", function () {

            if (this.checked) {
                $('#fieldEnterIBNPath', page).show();
                $('#txtItemsByNamePath', page).attr("required", "required");
            } else {
                $('#fieldEnterIBNPath', page).hide();
                $('#txtItemsByNamePath', page).removeAttr("required");
            }

        });

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    }).on('pagehide', "#librarySettingsPage", function () {

        var page = this;

        $('#chkEnableCustomIBNPath', page).off("change.showIBNText");
        $('#btnSelectIBNPath', page).off("click.selectDirectory");
    });

    function librarySettingsPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                if ($('#chkEnableCustomIBNPath', form).checked()) {
                    config.ItemsByNamePath = $('#txtItemsByNamePath', form).val();
                } else {
                    config.ItemsByNamePath = '';
                }

                config.SeasonZeroDisplayName = $('#txtSeasonZeroName', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.LibrarySettingsPage = new librarySettingsPage();

})(jQuery, document, window);
