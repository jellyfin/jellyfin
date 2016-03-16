define(['jQuery'], function ($) {

    function reload(page, providerId) {

        page.querySelector('.txtDevicePath').value = '';

        if (providerId) {
            ApiClient.getNamedConfiguration("livetv").then(function (config) {

                var info = config.TunerHosts.filter(function (i) {
                    return i.Id == providerId;
                })[0];

                page.querySelector('.txtDevicePath').value = info.Url || '';
                page.querySelector('.chkEnabled').checked = info.IsEnabled;
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

                fillInfoFromPage(page, info);
                submitTunerInfo(page, info);
            });

        } else {
            var info = {
                Type: 'satip'
            };

            fillInfoFromPage(page, info);
            submitTunerInfo(page, info);
        }
    }

    function fillInfoFromPage(page, info) {
        info.Url = page.querySelector('.txtDevicePath').value;
        info.IsEnabled = page.querySelector('.chkEnabled').checked;
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

    function onSelectDiseqCChange(e) {

        var select = e.target;
        var value = select.value;
        var page = $(select).parents('.page')[0];

        if (value) {
            page.querySelector('.fldSourceB').classList.remove('hide');
        } else {
            page.querySelector('.fldSourceB').classList.add('hide');
        }

        if (value == 'diseqc1') {

            page.querySelector('.fldSourceC').classList.remove('hide');
            page.querySelector('.fldSourceD').classList.remove('hide');
        } else {
            page.querySelector('.fldSourceC').classList.add('hide');
            page.querySelector('.fldSourceD').classList.add('hide');
        }
    }

    $(document).on('pageinit', "#liveTvTunerProviderSatPage", function () {

        var page = this;

        $('form', page).on('submit', function () {
            submitForm(page);
            return false;
        });

        page.querySelector('.selectDiseqC').addEventListener('change', onSelectDiseqCChange);

    }).on('pageshow', "#liveTvTunerProviderSatPage", function () {

        var providerId = getParameterByName('id');
        var page = this;
        reload(page, providerId);
    });

});