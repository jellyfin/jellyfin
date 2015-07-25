(function ($, document, window) {

    function reload(page, providerId) {

        $('#txtDevicePath', page).val('');
        $('#chkFavorite', page).checked(false).checkboxradio('refresh');

        if (providerId) {
            ApiClient.getNamedConfiguration("livetv").done(function (config) {

                var info = config.TunerHosts.filter(function (i) {
                    return i.Id == providerId;
                })[0];

                $('#txtDevicePath', page).val(info.Url || '');
                $('#chkFavorite', page).checked(info.ImportFavoritesOnly).checkboxradio('refresh');

            });
        }
    }

    function submitForm(page) {

        Dashboard.showLoadingMsg();

        var info = {
            Type: 'hdhomerun',
            Url: $('#txtDevicePath', page).val(),
            ImportFavoritesOnly: $('#chkFavorite', page).checked()
        };

        var id = getParameterByName('id');

        if (id) {
            info.Id = id;
        }

        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl('LiveTv/TunerHosts'),
            data: JSON.stringify(info),
            contentType: "application/json"

        }).done(function (result) {

            Dashboard.processServerConfigurationUpdateResult();
            Dashboard.navigate('livetvstatus.html');

        }).fail(function () {
            Dashboard.alert({
                message: Globalize.translate('ErrorSavingTvProvider')
            });
        });

    }

    $(document).on('pageinitdepends', "#liveTvTunerProviderHdHomerunPage", function () {

        var page = this;

        $('form', page).on('submit', function () {
            submitForm(page);
            return false;
        });

    }).on('pageshowready', "#liveTvTunerProviderHdHomerunPage", function () {

        var providerId = getParameterByName('id');
        var page = this;
        reload(page, providerId);
    });

})(jQuery, document, window);
