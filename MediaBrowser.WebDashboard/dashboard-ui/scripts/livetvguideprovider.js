(function ($, document, window) {

    var listingsId;

    function reload(page, providerId) {

        Dashboard.showLoadingMsg();

        ApiClient.getNamedConfiguration("livetv").done(function (config) {

            var info = config.ListingProviders.filter(function (i) {
                return i.Id == providerId;
            })[0];

            info = info || {};

            listingsId = info.ListingsId;
            $('#selectListing', page).val(info.ListingsId || '').selectmenu('refresh');

            page.querySelector('.txtZipCode').value = info.ZipCode || '';

            setCountry(page, info);
        });
    }

    function setCountry(page, info) {

        $('#selectCountry', page).val(info.Country || '').selectmenu('refresh');

        $(page.querySelector('.txtZipCode')).trigger('change');

        Dashboard.hideLoadingMsg();
    }

    function submitListingsForm(page) {

        var selectedListingsId = $('#selectListing', page).val();

        if (!selectedListingsId) {
            Dashboard.alert({
                message: Globalize.translate('ErrorPleaseSelectLineup')
            });
            return;
        }

        Dashboard.showLoadingMsg();

        var providerId = getParameterByName('id');

        ApiClient.getNamedConfiguration("livetv").done(function (config) {

            var info = config.ListingProviders.filter(function (i) {
                return i.Id == providerId;

            })[0] || {};

            info.ZipCode = page.querySelector('.txtZipCode').value;
            info.Country = $('#selectCountry', page).val();
            info.ListingsId = selectedListingsId;
            info.Type = 'emby';

            ApiClient.ajax({
                type: "POST",
                url: ApiClient.getUrl('LiveTv/ListingProviders', {
                }),
                data: JSON.stringify(info),
                contentType: "application/json"

            }).done(function (result) {

                Dashboard.processServerConfigurationUpdateResult();

            }).fail(function () {
                Dashboard.alert({
                    message: Globalize.translate('ErrorSavingTvProvider')
                });
            });

        });
    }

    function refreshListings(page, value) {

        if (!value) {
            $('#selectListing', page).html('').selectmenu('refresh');
            return;
        }

        Dashboard.showModalLoadingMsg();

        ApiClient.ajax({
            type: "GET",
            url: ApiClient.getUrl('LiveTv/ListingProviders/Lineups', {
                Type: 'emby',
                Location: value,
                Country: $('#selectCountry', page).val()
            }),
            dataType: 'json'

        }).done(function (result) {

            $('#selectListing', page).html(result.map(function (o) {

                return '<option value="' + o.Id + '">' + o.Name + '</option>';

            })).selectmenu('refresh');

            if (listingsId) {
                $('#selectListing', page).val(listingsId).selectmenu('refresh');
            }

            Dashboard.hideModalLoadingMsg();

        }).fail(function (result) {

            Dashboard.alert({
                message: Globalize.translate('ErrorGettingTvLineups')
            });
            refreshListings(page, '');
            Dashboard.hideModalLoadingMsg();

        });

    }

    $(document).on('pageinitdepends', "#liveTvGuideProviderPage", function () {

        var page = this;

        $('.formListings', page).on('submit', function () {
            submitListingsForm(page);
            return false;
        });

        $('.txtZipCode', page).on('change', function () {
            refreshListings(page, this.value);
        });

    }).on('pageshowready', "#liveTvGuideProviderPage", function () {

        var providerId = getParameterByName('id');
        var page = this;
        reload(page, providerId);
    });

})(jQuery, document, window);
