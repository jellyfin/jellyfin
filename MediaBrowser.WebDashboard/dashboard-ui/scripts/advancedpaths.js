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

                header: "Select Server Cache Path",

                instruction: "Browse or enter the path to use for Media Browser Server cache. The folder must be writeable. The location of this folder will directly impact server performance and should ideally be placed on a solid state drive."
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

                header: "Select Transcoding Temporary Path",

                instruction: "Browse or enter the path to use for transcoding temporary files. The folder must be writeable."
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

                header: "Select Images By Name Path",

                instruction: "Browse or enter the path to your items by name folder. The folder must be writeable."
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

                header: "Select Metadata Path",

                instruction: "Browse or enter the path you'd like to store metadata within. The folder must be writeable."
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
