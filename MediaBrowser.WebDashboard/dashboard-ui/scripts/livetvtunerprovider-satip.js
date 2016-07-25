define([], function () {

    function reload(page, providerId) {

        page.querySelector('.txtDevicePath').value = '';

        if (providerId) {
            ApiClient.getNamedConfiguration("livetv").then(function (config) {

                var info = config.TunerHosts.filter(function (i) {
                    return i.Id == providerId;
                })[0];

                page.querySelector('.txtDevicePath').value = info.Url || '';
                page.querySelector('.txtM3uUrl').value = info.M3UUrl || '';
                page.querySelector('.chkEnabled').checked = info.IsEnabled;
            });
        }
    }

    function fillInfoFromPage(page, info) {
        info.Url = page.querySelector('.txtDevicePath').value;
        info.M3UUrl = page.querySelector('.txtM3uUrl').value;
        info.IsEnabled = page.querySelector('.chkEnabled').checked;
        info.DiseqC = page.querySelector('.selectDiseqC').value;
        info.SourceA = page.querySelector('.selectSourceA').value;
        info.SourceB = page.querySelector('.selectSourceB').value;
        info.SourceC = page.querySelector('.selectSourceC').value;
        info.SourceD = page.querySelector('.selectSourceD').value;
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

    function populateMappings(view) {

        ApiClient.getJSON(ApiClient.getUrl('LiveTv/TunerHosts/Satip/IniMappings')).then(function (mappings) {

            var optionsHtml = mappings.map(function (m) {
                return '<option value="' + m.Value + '">' + m.Name + '</option>';
            }).join('');

            optionsHtml = '<option value="">' + Globalize.translate('OptionNone') + '</option>' + optionsHtml;

            view.querySelector('.selectSourceA').innerHTML = optionsHtml;
            view.querySelector('.selectSourceB').innerHTML = optionsHtml;
            view.querySelector('.selectSourceC').innerHTML = optionsHtml;
            view.querySelector('.selectSourceD').innerHTML = optionsHtml;
        });
    }

    return function (view, params) {

        function submitForm() {

            Dashboard.showLoadingMsg();

            var id = params.id;

            if (id) {

                ApiClient.getNamedConfiguration("livetv").then(function (config) {

                    var info = config.TunerHosts.filter(function (i) {
                        return i.Id == id;
                    })[0];

                    fillInfoFromPage(view, info);
                    submitTunerInfo(view, info);
                });

            } else {
                var info = {
                    Type: 'satip'
                };

                fillInfoFromPage(view, info);
                submitTunerInfo(view, info);
            }
        }

        function onSelectDiseqCChange(e) {

            var select = e.target;
            var value = select.value;

            if (value) {
                view.querySelector('.fldSourceB').classList.remove('hide');
            } else {
                view.querySelector('.fldSourceB').classList.add('hide');
            }

            if (value == 'diseqc1') {

                view.querySelector('.fldSourceC').classList.remove('hide');
                view.querySelector('.fldSourceD').classList.remove('hide');
            } else {
                view.querySelector('.fldSourceC').classList.add('hide');
                view.querySelector('.fldSourceD').classList.add('hide');
            }
        }

        view.querySelector('form').addEventListener('submit', function (e) {
            submitForm();
            e.preventDefault();
            return false;
        });

        view.querySelector('.selectDiseqC').addEventListener('change', onSelectDiseqCChange);

        populateMappings(view);

        view.addEventListener('viewshow', function (e) {
            var providerId = params.id;
            reload(view, providerId);
        });
    }
});