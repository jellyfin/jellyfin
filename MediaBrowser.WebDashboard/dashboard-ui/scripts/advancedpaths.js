(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtCachePath', page).val(config.CachePath || '');
        $('#txtTranscodingTempPath', page).val(config.TranscodingTempPath || '');
        $('#txtItemsByNamePath', page).val(config.ItemsByNamePath || '');
        $('#txtMetadataPath', page).val(config.MetadataPath || '');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#advancedPathsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    }).on('pageinit', "#advancedPathsPage", function () {

        var page = this;

        $('#btnSelectCachePath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtCachePath', page).val(path);
                    }
                    picker.close();
                },

                header: Globalize.translate('HeaderSelectServerCachePath'),

                instruction: Globalize.translate('HeaderSelectServerCachePathHelp')
            });
        });

        $('#btnSelectTranscodingTempPath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

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

    });

    window.AdvancedPathsPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.CachePath = $('#txtCachePath', form).val();
                config.TranscodingTempPath = $('#txtTranscodingTempPath', form).val();
                config.ItemsByNamePath = $('#txtItemsByNamePath', form).val();
                config.MetadataPath = $('#txtMetadataPath', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
