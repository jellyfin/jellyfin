(function ($, document, window) {

    var currentConfig;

    function remove(page, index) {

        Dashboard.confirm(Globalize.translate('MessageConfirmPathSubstitutionDeletion'), Globalize.translate('HeaderConfirmDeletion'), function (result) {

            if (result) {

                ApiClient.getServerConfiguration().then(function (config) {

                    config.PathSubstitutions.splice(index, 1);

                    ApiClient.updateServerConfiguration(config).then(function () {

                        reload(page);
                    });
                });
            }

        });


    }

    function addSubstitution(page, config) {

        config.PathSubstitutions.push({
            From: $('#txtFrom', page).val(),
            To: $('#txtTo', page).val()
        });

    }

    function reloadPathMappings(page, config) {

        var index = 0;

        var html = config.PathSubstitutions.map(function (map) {

            var mapHtml = '<tr>';

            mapHtml += '<td style="vertical-align:middle;">';
            mapHtml += map.From;
            mapHtml += '</td>';

            mapHtml += '<td style="vertical-align:middle;">';
            mapHtml += map.To;
            mapHtml += '</td>';

            mapHtml += '<td>';
            mapHtml += '<paper-icon-button data-index="' + index + '" icon="delete" class="btnDeletePath"></paper-icon-button>';
            mapHtml += '</td>';

            mapHtml += '</tr>';

            index++;

            return mapHtml;
        });

        var elem = $('.tbodyPathSubstitutions', page).html(html.join('')).parents('table').table('refresh').trigger('create');

        $('.btnDeletePath', elem).on('click', function () {

            remove(page, parseInt(this.getAttribute('data-index')));
        });

        if (config.PathSubstitutions.length) {
            $('#tblPaths', page).show();
        } else {
            $('#tblPaths', page).hide();
        }
    }

    function loadPage(page, config) {

        currentConfig = config;

        reloadPathMappings(page, config);
        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        $('#txtFrom', page).val('');
        $('#txtTo', page).val('');

        ApiClient.getServerConfiguration().then(function (config) {

            loadPage(page, config);

        });
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;
        var page = $(form).parents('.page');

        ApiClient.getServerConfiguration().then(function (config) {

            addSubstitution(page, config);
            ApiClient.updateServerConfiguration(config).then(function () {

                reload(page);
            });
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#libraryPathMappingPage", function () {

        $('.libraryPathMappingForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#libraryPathMappingPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().then(function (config) {

            loadPage(page, config);

        });

    }).on('pagebeforehide', "#libraryPathMappingPage", function () {

        currentConfig = null;

    });

})(jQuery, document, window);
