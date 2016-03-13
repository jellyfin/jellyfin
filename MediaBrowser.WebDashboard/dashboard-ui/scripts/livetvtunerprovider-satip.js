(function ($, document, window) {

    function reload(page, providerId) {

        page.querySelector('.txtDevicePath').value = '';

        if (providerId) {
            ApiClient.getNamedConfiguration("livetv").then(function (config) {

                var info = config.TunerHosts.filter(function (i) {
                    return i.Id == providerId;
                })[0];

                page.querySelector('.txtDevicePath').value = info.Url || '';
            });
        }
    }

    function submitForm(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        if (id) {

            ApiClient.getNamedConfiguration("livetv").then(function (config) {

                var info = config.TunerHosts.filter(function (i) {
                    return i.Id == id;
                })[0];

                info.Url = page.querySelector('.txtDevicePath').value;

                submitTunerInfo(page, info);
            });

        } else {
            var info = {
                Type: 'satip',
                Url: page.querySelector('.txtDevicePath').value
            };

            submitTunerInfo(page, info);
        }
    }

    function submitTunerInfo(page, info) {
        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl('LiveTv/TunerHosts'),
            data: JSON.stringify(info),
            contentType: "application/json"

        }).then(function () {

            Dashboard.processServerConfigurationUpdateResult();
            Dashboard.navigate('livetvstatus.html');

        }, function () {
            Dashboard.hideLoadingMsg();
            Dashboard.alert({
                message: Globalize.translate('ErrorSavingTvProvider')
            });
        });
    }

    $(document).on('pageinit', "#liveTvTunerProviderSatPage", function () {

        var page = this;

        $('form', page).on('submit', function () {
            submitForm(page);
            return false;
        });

    }).on('pageshow', "#liveTvTunerProviderSatPage", function () {

        var providerId = getParameterByName('id');
        var page = this;
        reload(page, providerId);
    });

})(jQuery, document, window);
