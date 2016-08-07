define(['jQuery', 'listViewStyle'], function ($) {

    var currentConfig;

    function remove(page, index) {

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmPathSubstitutionDeletion'), Globalize.translate('HeaderConfirmDeletion')).then(function () {

                ApiClient.getServerConfiguration().then(function (config) {

                    config.PathSubstitutions.splice(index, 1);

                    ApiClient.updateServerConfiguration(config).then(function () {

                        reload(page);
                    });
                });
            });
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

            var mapHtml = '';
            mapHtml += '<div class="listItem">';

            mapHtml += '<i class="listItemIcon md-icon">folder</i>';

            mapHtml += '<div class="listItemBody three-line">';

            mapHtml += "<h3 class='listItemBodyText'>" + map.From + "</h3>";
            mapHtml += "<div class='listItemBodyText secondary'>" + Globalize.translate('HeaderTo') + "</div>";
            mapHtml += "<div class='listItemBodyText secondary'>" + map.To + "</div>";

            mapHtml += '</div>';

            mapHtml += '<button type="button" is="paper-icon-button-light" data-index="' + index + '" class="btnDeletePath"><i class="md-icon">delete</i></button>';

            mapHtml += '</div>';

            index++;

            return mapHtml;

        }).join('');

        if (config.PathSubstitutions.length) {
            html = '<div class="paperList">' + html + '</div>';
        } 

        var elem = $('.pathSubstitutions', page).html(html);

        $('.btnDeletePath', elem).on('click', function () {

            remove(page, parseInt(this.getAttribute('data-index')));
        });
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

    function getTabs() {
        return [
        {
            href: 'library.html',
            name: Globalize.translate('TabFolders')
        },
         {
             href: 'librarydisplay.html',
             name: Globalize.translate('TabDisplay')
         },
         {
             href: 'librarypathmapping.html',
             name: Globalize.translate('TabPathSubstitution')
         },
         {
             href: 'librarysettings.html',
             name: Globalize.translate('TabAdvanced')
         }];
    }


    $(document).on('pageinit', "#libraryPathMappingPage", function () {

        var page = this;

        $('.libraryPathMappingForm').off('submit', onSubmit).on('submit', onSubmit);

        page.querySelector('.labelFromHelp').innerHTML = Globalize.translate('LabelFromHelp', 'D:\\Movies');

    }).on('pageshow', "#libraryPathMappingPage", function () {

        LibraryMenu.setTabs('librarysetup', 2, getTabs);
        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().then(function (config) {

            loadPage(page, config);

        });

    }).on('pagebeforehide', "#libraryPathMappingPage", function () {

        currentConfig = null;

    });

});
