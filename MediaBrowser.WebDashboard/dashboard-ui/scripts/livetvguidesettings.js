(function ($, document, window) {

    function loadPage(page) {

        ApiClient.getNamedConfiguration("livetv").done(function (config) {

            renderProviders(page, config.ListingProviders);
            Dashboard.hideLoadingMsg();
        });
    }

    function renderProviders(page, providers) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

        for (var i = 0, length = providers.length; i < length; i++) {

            var provider = providers[i];
            html += '<li>';
            html += '<a href="#">';

            html += '<h3>';
            html += provider.Name;
            html += '</h3>';

            html += '</a>';
            html += '<a href="#" class="btnDelete">';
            html += '</a>';
            html += '</li>';
        }

        html += '</ul>';

        var elem = $('.providerList', page).html(html).trigger('create');

        $('.btnDelete', elem).on('click', function () {

            var id = this.getAttribute('data-id');

            deleteProvider(page, id);
        });
    }

    function deleteProvider(page, id) {

        var message = Globalize.translate('MessageConfirmDeleteGuideProvider');

        Dashboard.confirm(message, Globalize.translate('HeaderDeleteProvider'), function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl('LiveTv/TunerHosts', {
                        Id: id
                    })

                }).done(function () {

                    loadPage(page);
                });
            }
        });
    }

    function submitAddProviderForm(page) {

        page.querySelector('.dlgAddProvider').close();
        Dashboard.showLoadingMsg();

        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl('LiveTv/TunerHosts'),
            data: JSON.stringify({
                Type: $('#selectTunerDeviceType', page).val(),
                Url: $('#txtDevicePath', page).val()
            }),
            contentType: "application/json"

        }).done(function () {

            loadPage(page);
        });

    }

    $(document).on('pageinitdepends', "#liveTvGuideSettingsPage", function () {

        var page = this;

        $('.btnAddProvider', page).on('click', function () {
            page.querySelector('.dlgAddProvider').open();
        });

        $('.formAddProvider', page).on('submit', function () {
            submitAddProviderForm(page);
            return false;
        });

    }).on('pageshowready', "#liveTvGuideSettingsPage", function () {

        var page = this;

        loadPage(page);
    });

})(jQuery, document, window);
