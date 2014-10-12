(function () {

    function load(page, devices, config) {

        if (devices.length) {
            $('.noDevices', page).hide();
            $('.devicesUploadForm', page).show();
        } else {
            $('.noDevices', page).show();
            $('.devicesUploadForm', page).hide();
        }

        $('#txtUploadPath', page).val(config.CameraUploadPath || '');

        $('#chkSubfolder', page).checked(config.EnableCameraUploadSubfolders).checkboxradio('refresh');

        loadDeviceList(page, devices, config);
    }

    function loadDeviceList(page, devices, config) {

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>';
        html += Globalize.translate('LabelEnableCameraUploadFor');
        html += '</legend>';

        var index = 0;
        html += devices.map(function (d) {

            var deviceHtml = '';

            var id = "chk" + index;

            deviceHtml += '<label for="' + id + '">';
            deviceHtml += d.Name;

            if (d.AppName) {
                deviceHtml += '<br/><span>' + d.AppName + '</span>';
            }

            deviceHtml += '</label>';

            var isChecked = config.EnabledCameraUploadDevices.indexOf(d.Id) != -1;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            deviceHtml += '<input type="checkbox" id="' + id + '" class="chkDevice" data-id="' + d.Id + '"' + checkedHtml + ' />';

            index++;

            return deviceHtml;

        }).join('');

        html += '</fieldset>';

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

        $.when(promise1, promise2).done(function (response1, response2) {


            load(page, response2[0], response1[0]);

            Dashboard.hideLoadingMsg();
        });
    }

    function save(page) {

        ApiClient.getNamedConfiguration("devices").done(function (config) {

            config.CameraUploadPath = $('#txtUploadPath', page).val();

            config.EnabledCameraUploadDevices = $('.chkDevice:checked', page).get().map(function (c) {

                return c.getAttribute('data-id');

            });

            config.EnableCameraUploadSubfolders = $('#chkSubfolder', page).checked();

            ApiClient.updateNamedConfiguration("devices", config).done(Dashboard.processServerConfigurationUpdateResult);
        });

    }

    $(document).on('pageinit', "#devicesUploadPage", function () {

        var page = this;

        $('#btnSelectUploadPath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

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


    }).on('pageshow', "#devicesUploadPage", function () {

        var page = this;

        loadData(page);

    });

    window.DevicesUploadPage = {

        onSubmit: function () {

            var form = this;
            var page = $(form).parents('.page');

            save(page);

            return false;
        }
    };

})();