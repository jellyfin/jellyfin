(function ($, document, window) {

    var providerId;
    var listingsId;

    function reload(page) {

        ApiClient.getNamedConfiguration("livetv").done(function (config) {

            var info = config.ListingProviders.filter(function (i) {
                return i.Id == providerId;
            })[0];

            listingsId = info.ListingsId;
            $('#selectListing', page).val(info.ListingsId || '').selectmenu('refresh');
            $('#txtZipCode', page).val(info.ZipCode || '').trigger('change');
            $('#txtUser', page).val(info.Username || '');

        });
    }

    function submitLoginForm(page) {

        Dashboard.showLoadingMsg();

        var info = {
            Type: 'SchedulesDirect',
            Username: $('#txtUser', page).val(),
            Password: CryptoJS.SHA1($('#txtPass', page).val()).toString()
        };

        var id = providerId;

        if (id) {
            info.Id = id;
        }

        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl('LiveTv/ListingProviders'),
            data: JSON.stringify(info),
            contentType: "application/json"

        }).done(function (result) {

            Dashboard.processServerConfigurationUpdateResult();

        }).fail(function () {
            Dashboard.alert({
                message: Globalize.translate('ErrorSavingTvProvider')
            });
        });

    }

    function submitListingsForm(page) {

        Dashboard.showLoadingMsg();

        var id = providerId;

        ApiClient.getNamedConfiguration("livetv").done(function (config) {

            var info = config.ListingProviders.filter(function (i) {
                return i.Id == id;
            })[0];

            info.ZipCode = $('#txtZipCode', page).val();
            info.ListingsId = $('#selectListing', page).val();

            ApiClient.ajax({
                type: "POST",
                url: ApiClient.getUrl('LiveTv/ListingProviders'),
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
                Id: providerId,
                Location: value
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

            //Dashboard.alert({
            //    message: Globalize.translate('ErrorGettingTvLineups')
            //});
            //refreshListings(page, '');
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

        $('#txtZipCode', page).on('change', function () {
            refreshListings(page, this.value);
        });


    }).on('pageshowready', "#liveTvGuideProviderScdPage", function () {

        providerId = getParameterByName('id');

        var page = this;
        reload(page);
    });

})(jQuery, document, window);
