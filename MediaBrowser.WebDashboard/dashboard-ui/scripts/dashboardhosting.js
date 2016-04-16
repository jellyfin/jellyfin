define(['jQuery'], function ($) {

    function loadPage(page, config) {

        $('#txtPortNumber', page).val(config.HttpServerPortNumber);
        $('#txtPublicPort', page).val(config.PublicPort);
        $('#txtPublicHttpsPort', page).val(config.PublicHttpsPort);

        $('#chkEnableHttps', page).checked(config.EnableHttps);
        $('#txtHttpsPort', page).val(config.HttpsPortNumber);

        $('#txtDdns', page).val(config.WanDdns || '');
        $('#txtCertificatePath', page).val(config.CertificatePath || '');

        $('#chkEnableUpnp', page).checked(config.EnableUPnP);

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().then(function (config) {

            config.HttpServerPortNumber = $('#txtPortNumber', form).val();
            config.PublicPort = $('#txtPublicPort', form).val();
            config.PublicHttpsPort = $('#txtPublicHttpsPort', form).val();
            config.EnableHttps = $('#chkEnableHttps', form).checked();
            config.HttpsPortNumber = $('#txtHttpsPort', form).val();
            config.EnableUPnP = $('#chkEnableUpnp', form).checked();
            config.WanDdns = $('#txtDdns', form).val();
            config.CertificatePath = $('#txtCertificatePath', form).val();

            ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    function getTabs() {
        return [
        {
            href: 'dashboardhosting.html',
            name: Globalize.translate('TabHosting')
        },
         {
             href: 'serversecurity.html',
             name: Globalize.translate('TabSecurity')
         }];
    }

    $(document).on('pageshow', "#dashboardHostingPage", function () {

        LibraryMenu.setTabs('adminadvanced', 0, getTabs);
        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().then(function (config) {

            loadPage(page, config);

        });

    }).on('pageinit', "#dashboardHostingPage", function () {

        var page = this;

        $('#btnSelectCertPath', page).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    includeFiles: true,
                    includeDirectories: true,

                    callback: function (path) {

                        if (path) {
                            $('#txtCertificatePath', page).val(path);
                        }
                        picker.close();
                    },

                    header: Globalize.translate('HeaderSelectCertificatePath')
                });
            });
        });

        $('.dashboardHostingForm').off('submit', onSubmit).on('submit', onSubmit);
    });

});
