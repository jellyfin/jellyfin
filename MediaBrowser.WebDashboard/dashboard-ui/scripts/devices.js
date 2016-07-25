define(['jQuery'], function ($) {

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
            deviceHtml += '<paper-icon-item>';

            deviceHtml += '<paper-fab mini style="background:#999;" icon="tablet-android" item-icon></paper-fab>';

            deviceHtml += '<paper-item-body three-line>';
            deviceHtml += '<a class="clearLink" href="device.html?id=' + d.Id + '">';

            deviceHtml += '<div>';
            deviceHtml += d.Name;
            deviceHtml += '</div>';

            if (d.AppName) {
                deviceHtml += '<div secondary>';
                deviceHtml += d.AppName;
                deviceHtml += '</div>';
            }

            if (d.LastUserName) {
                deviceHtml += '<div secondary>';
                deviceHtml += Globalize.translate('DeviceLastUsedByUserName', d.LastUserName);
                deviceHtml += '</div>';
            }

            deviceHtml += '</a>';
            deviceHtml += '</paper-item-body>';

            deviceHtml += '<button type="button" is="paper-icon-button-light" class="btnDeleteDevice" data-id="' + d.Id + '" title="' + Globalize.translate('ButtonDelete') + '"><iron-icon icon="delete"></iron-icon></button>';

            deviceHtml += '</paper-icon-item>';

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

            require(['paper-fab', 'paper-item-body', 'paper-icon-item'], function () {
                load(page, result.Items);
            });

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageshow', "#devicesPage", function () {

        var page = this;

        loadData(page);

    });

});