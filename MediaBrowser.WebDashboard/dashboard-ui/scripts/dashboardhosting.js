(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtPortNumber', page).val(config.HttpServerPortNumber);
        $('#txtPublicPort', page).val(config.PublicPort);
        $('#txtPublicHttpsPort', page).val(config.PublicHttpsPort);

        $('#chkEnableHttps', page).checked(config.EnableHttps).checkboxradio('refresh');
        $('#txtHttpsPort', page).val(config.HttpsPortNumber);
        $('#txtCertificatePath', page).val(config.CertificatePath);

        $('#txtDdns', page).val(config.WanDdns || '');

        $('#chkEnableUpnp', page).checked(config.EnableUPnP).checkboxradio('refresh');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#dashboardHostingPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    }).on('pageinit', "#dashboardHostingPage", function () {

        var page = this;
    });

    window.DashboardHostingPage = {

        onSubmit: function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.HttpServerPortNumber = $('#txtPortNumber', form).val();
                config.PublicPort = $('#txtPublicPort', form).val();
                config.PublicHttpsPort = $('#txtPublicHttpsPort', form).val();
                config.EnableHttps = $('#chkEnableHttps', form).checked();
                config.HttpsPortNumber = $('#txtHttpsPort', form).val();
                config.CertificatePath = $('#txtCertificatePath', form).val();
                config.EnableUPnP = $('#chkEnableUpnp', form).checked();
                config.WanDdns = $('#txtDdns', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }

    };

})(jQuery, document, window);
