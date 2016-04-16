define(['jQuery'], function ($) {

    function load(page, devices, config) {

        if (devices.length) {
            $('.noDevices', page).hide();
            $('.devicesUploadForm', page).show();
        } else {
            $('.noDevices', page).show();
            $('.devicesUploadForm', page).hide();
        }

        $('#txtUploadPath', page).val(config.CameraUploadPath || '');

        $('#chkSubfolder', page).checked(config.EnableCameraUploadSubfolders);

        loadDeviceList(page, devices, config);
    }

    function loadDeviceList(page, devices, config) {

        var html = '';

        html += '<div class="paperListLabel">';
        html += Globalize.translate('LabelEnableCameraUploadFor');
        html += '</div>';

        html += '<div class="paperCheckboxList paperList">';

        var index = 0;
        html += devices.map(function (d) {

            var deviceHtml = '';

            var isChecked = config.EnabledCameraUploadDevices.indexOf(d.Id) != -1;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            var label = d.Name;

            if (d.AppName) {
                label += ' - ' + d.AppName;
            }

            deviceHtml += '<paper-checkbox class="chkDevice" data-id="' + d.Id + '"' + checkedHtml + '>' + label + '</paper-checkbox>';

            index++;

            return deviceHtml;

        }).join('');

        html += '</div>';

        html += '<div class="fieldDescription">';
        html += Globalize.translate('LabelEnableCameraUploadForHelp');
        html += '</div>';

        $('.devicesList', page).html(html).trigger('create');
    }

    function loadData(page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getNamedConfiguration("devices");
        var promise2 = ApiClient.getJSON(ApiClient.getUrl('Devices', {

            SupportsContentUploading: true

        }));

        Promise.all([promise1, promise2]).then(function (responses) {


            load(page, responses[1].Items, responses[0]);

            Dashboard.hideLoadingMsg();
        });
    }

    function save(page) {

        ApiClient.getNamedConfiguration("devices").then(function (config) {

            config.CameraUploadPath = $('#txtUploadPath', page).val();

            config.EnabledCameraUploadDevices = $('.chkDevice', page).get().filter(function (c) {

                return c.checked;

            }).map(function (c) {

                return c.getAttribute('data-id');

            });

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