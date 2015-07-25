(function ($, document, window) {

    var listingsId;

    function reload(page, providerId) {

        ApiClient.getNamedConfiguration("livetv").done(function (config) {

            var info = config.ListingProviders.filter(function (i) {
                return i.Id == providerId;
            })[0];

            listingsId = info.ListingsId;
            $('#selectListing', page).val(info.ListingsId || '').selectmenu('refresh');
            $('#selectCountry', page).val(info.Country || '').selectmenu('refresh');
            page.querySelector('.txtZipCode').value = info.ZipCode || '';
            $(page.querySelector('.txtZipCode')).trigger('change');
            page.querySelector('.txtUser').value = info.Username || '';
            page.querySelector('.txtPass').value = info.Username || '';

        });
    }

    function submitLoginForm(page) {

        Dashboard.showLoadingMsg();

        var info = {
            Type: 'SchedulesDirect',
            Username: page.querySelector('.txtUser').value,
            Password: CryptoJS.SHA1(page.querySelector('.txtPass').value).toString()
        };

        var providerId = getParameterByName('id');
        var id = providerId;

        if (id) {
            info.Id = id;
        }

        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl('LiveTv/ListingProviders', {
                ValidateLogin: true
            }),
            data: JSON.stringify(info),
            contentType: "application/json"

        }).done(function (result) {

            Dashboard.processServerConfigurationUpdateResult();
            Dashboard.navigate('livetvguideprovider-scd.html?id=' + result.Id);

        }).fail(function () {
            Dashboard.alert({
                message: Globalize.translate('ErrorSavingTvProvider')
            });
        });

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
        var id = providerId;

        ApiClient.getNamedConfiguration("livetv").done(function (config) {

            var info = config.ListingProviders.filter(function (i) {
                return i.Id == id;
            })[0];

            info.ZipCode = page.querySelector('.txtZipCode').value;
            info.Country = $('#selectCountry', page).val();
            info.ListingsId = selectedListingsId;

            ApiClient.ajax({
                type: "POST",
                url: ApiClient.getUrl('LiveTv/ListingProviders', {
                    ValidateListings: true
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

        var providerId = getParameterByName('id');

        Dashboard.showModalLoadingMsg();

        ApiClient.ajax({
            type: "GET",
            url: ApiClient.getUrl('LiveTv/ListingProviders/Lineups', {
                Id: providerId,
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

    $(document).on('pageinitdepends', "#liveTvGuideProviderScdPage", function () {

        var page = this;

        $('.formLogin', page).on('submit', function () {
            submitLoginForm(page);
            return false;
        });

        $('.formListings', page).on('submit', function () {
            submitListingsForm(page);
            return false;
        });

        $('.txtZipCode', page).on('change', function () {
            refreshListings(page, this.value);
        });

        $('.createAccountHelp', page).html(Globalize.translate('MessageCreateAccountAt', '<a href="http://www.schedulesdirect.org" target="_blank">http://www.schedulesdirect.org</a>'));

    }).on('pageshowready', "#liveTvGuideProviderScdPage", function () {

        var providerId = getParameterByName('id');
        var page = this;
        reload(page, providerId);
    });

})(jQuery, document, window);
