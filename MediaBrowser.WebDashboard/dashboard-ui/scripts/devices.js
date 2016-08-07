define(['jQuery', 'listViewStyle'], function ($) {

    function deleteDevice(page, id) {

        var msg = Globalize.translate('DeleteDeviceConfirmation');

        require(['confirm'], function (confirm) {

            confirm(msg, Globalize.translate('HeaderDeleteDevice')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl('Devices', {
                        Id: id
                    })

                }).then(function () {

                    loadData(page);
                });
            });

        });
    }

    function load(page, devices) {

        var html = '';

        if (devices.length) {
            html += '<div class="paperList">';
        }

        html += devices.map(function (d) {

            var deviceHtml = '';
            deviceHtml += '<div class="listItem">';

            deviceHtml += '<i class="listItemIcon md-icon" style="background:#999;">tablet_android</i>';

            if (d.AppName && d.LastUserName) {
                deviceHtml += '<div class="listItemBody three-line">';
            } else {
                deviceHtml += '<div class="listItemBody two-line">';
            }
            deviceHtml += '<a class="clearLink" href="device.html?id=' + d.Id + '">';

            deviceHtml += '<div class="listItemBodyText">';
            deviceHtml += d.Name;
            deviceHtml += '</div>';

            if (d.AppName) {
                deviceHtml += '<div class="listItemBodyText secondary">';
                deviceHtml += d.AppName;
                deviceHtml += '</div>';
            }

            if (d.LastUserName) {
                deviceHtml += '<div class="listItemBodyText secondary">';
                deviceHtml += Globalize.translate('DeviceLastUsedByUserName', d.LastUserName);
                deviceHtml += '</div>';
            }

            deviceHtml += '</a>';
            deviceHtml += '</div>';

            deviceHtml += '<button type="button" is="paper-icon-button-light" class="btnDeleteDevice" data-id="' + d.Id + '" title="' + Globalize.translate('ButtonDelete') + '"><i class="md-icon">delete</i></button>';

            deviceHtml += '</div>';

            return deviceHtml;

        }).join('');

        if (devices.length) {
            html += '</div>';
        }

        var elem = $('.devicesList', page).html(html).trigger('create');

        $('.btnDeleteDevice', elem).on('click', function () {

            deleteDevice(page, this.getAttribute('data-id'));
        });
    }

    function loadData(page) {
        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl('Devices', {
            
            SupportsPersistentIdentifier: true

        })).then(function (result) {

            load(page, result.Items);

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageshow', "#devicesPage", function () {

        var page = this;

        loadData(page);

    });

});