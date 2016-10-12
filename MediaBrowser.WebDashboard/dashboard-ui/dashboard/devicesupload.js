define(['jQuery', 'fnchecked'], function ($) {

    function load(page, config) {

        $('#txtUploadPath', page).val(config.CameraUploadPath || '');

        $('#chkSubfolder', page).checked(config.EnableCameraUploadSubfolders);
    }

    function loadData(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getNamedConfiguration("devices").then(function (config) {
            load(page, config);

            Dashboard.hideLoadingMsg();
        });
    }

    function save(page) {

        ApiClient.getNamedConfiguration("devices").then(function (config) {

            config.CameraUploadPath = $('#txtUploadPath', page).val();

            config.EnableCameraUploadSubfolders = $('#chkSubfolder', page).checked();

            ApiClient.updateNamedConfiguration("devices", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

    }

    function onSubmit() {
        var form = this;
        var page = $(form).parents('.page');

        save(page);

        return false;
    }

    function getTabs() {
        return [
        {
            href: 'syncactivity.html',
            name: Globalize.translate('TabSyncJobs')
        },
         {
             href: 'devicesupload.html',
             name: Globalize.translate('TabCameraUpload')
         },
        {
            href: 'appservices.html?context=sync',
            name: Globalize.translate('TabServices')
        },
         {
             href: 'syncsettings.html',
             name: Globalize.translate('TabSettings')
         }];
    }

    $(document).on('pageinit', "#devicesUploadPage", function () {

        var page = this;

        $('#btnSelectUploadPath', page).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    callback: function (path) {

                        if (path) {
                            $('#txtUploadPath', page).val(path);
                        }
                        picker.close();
                    },

                    header: Globalize.translate('HeaderSelectUploadPath')
                });
            });
        });

        $('.devicesUploadForm').off('submit', onSubmit).on('submit', onSubmit);


    }).on('pageshow', "#devicesUploadPage", function () {

        LibraryMenu.setTabs('syncadmin', 1, getTabs);
        var page = this;

        loadData(page);

    });

});