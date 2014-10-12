(function () {

    function deleteDevice(page, id) {

        var msg = Globalize.translate('DeleteDeviceConfirmation');

        Dashboard.confirm(msg, Globalize.translate('HeaderDeleteDevice'), function (result) {

            if (result) {
                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl('Devices', {
                        Id: id
                    })

                }).done(function () {

                    loadData(page);
                });
            }
        });
    }

    function load(page, devices) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="minus">';

        html += devices.map(function (d) {

            var deviceHtml = '';
            deviceHtml += '<li>';

            deviceHtml += '<a href="device.html?id=' + d.Id + '">';

            deviceHtml += '<h3>';
            deviceHtml += d.Name;
            deviceHtml += '</h3>';

            if (d.AppName) {
                deviceHtml += '<p style="color:blue;">';
                deviceHtml += d.AppName;
                deviceHtml += '</p>';
            }

            if (d.LastUserName) {
                deviceHtml += '<p style="color:green;">';
                deviceHtml += Globalize.translate('DeviceLastUsedByUserName', d.LastUserName);
                deviceHtml += '</p>';
            }

            deviceHtml += '</a>';

            deviceHtml += '<a href="#" data-icon="minus" class="btnDeleteDevice" data-id="' + d.Id + '">';
            deviceHtml += Globalize.translate('Delete');
            deviceHtml += '</a>';


            deviceHtml += '</li>';
            return deviceHtml;

        }).join('');

        html += '</ul>';

        var elem = $('.devicesList', page).html(html).trigger('create');

        $('.btnDeleteDevice', elem).on('click', function () {

            deleteDevice(page, this.getAttribute('data-id'));
        });
    }

    function loadData(page) {
        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl('Devices')).done(function (devices) {

            load(page, devices);

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageshow', "#devicesPage", function () {

        var page = this;

        loadData(page);

    });

})();