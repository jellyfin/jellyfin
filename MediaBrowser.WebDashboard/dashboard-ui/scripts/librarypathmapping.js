(function ($, document, window) {

    var currentConfig;

    function remove(page, index) {

        Dashboard.confirm("Are you sure you wish to delete this path substitution?", "Confirm Deletion", function (result) {

            if (result) {

                ApiClient.getServerConfiguration().done(function (config) {

                    config.PathSubstitutions.splice(index, 1);

                    ApiClient.updateServerConfiguration(config).done(function () {

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

            mapHtml += '<td>';
            mapHtml += '<button class="btnDeletePath" data-index="' + index + '" data-mini="true" data-inline="true" data-icon="delete" data-iconpos="notext" type="button" style="margin:0 .5em 0 0;">Delete</button>';
            mapHtml += '</td>';

            mapHtml += '<td style="vertical-align:middle;">';
            mapHtml += map.From;
            mapHtml += '</td>';

            mapHtml += '<td style="vertical-align:middle;">';
            mapHtml += map.To;
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

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });
    }

    $(document).on('pageshow', "#libraryPathMappingPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    }).on('pagehide', "#libraryPathMappingPage", function () {

        currentConfig = null;

    });

    window.LibraryPathMappingPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;
            var page = $(form).parents('.page');

            ApiClient.getServerConfiguration().done(function (config) {

                addSubstitution(page, config);
                ApiClient.updateServerConfiguration(config).done(function () {

                    reload(page);
                });
            });

            // Disable default form submission
            return false;

        }

    };

})(jQuery, document, window);
