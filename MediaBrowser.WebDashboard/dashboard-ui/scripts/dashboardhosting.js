define(['jQuery'], function ($) {

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

    return function (view, params) {

        var self = this;

        function loadPage(page, config) {

            $('#txtPortNumber', page).val(config.HttpServerPortNumber);
            $('#txtPublicPort', page).val(config.PublicPort);
            $('#txtPublicHttpsPort', page).val(config.PublicHttpsPort);

            var chkEnableHttps = page.querySelector('#chkEnableHttps');
            chkEnableHttps.checked = config.EnableHttps;

            $('#txtHttpsPort', page).val(config.HttpsPortNumber);

            $('#txtDdns', page).val(config.WanDdns || '');

            var txtCertificatePath = page.querySelector('#txtCertificatePath');
            txtCertificatePath.value = config.CertificatePath || '';

            $('#chkEnableUpnp', page).checked(config.EnableUPnP);

            onCertPathChange.call(txtCertificatePath);

            Dashboard.hideLoadingMsg();
        }

        function onCertPathChange() {

            if (this.value) {
                view.querySelector('#txtDdns').setAttribute('required', 'required');
            } else {
                view.querySelector('#txtDdns').removeAttribute('required');
            }
        }

        $('#btnSelectCertPath', view).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    includeFiles: true,
                    includeDirectories: true,

                    callback: function (path) {

                        if (path) {
                            $('#txtCertificatePath', view).val(path);
                        }
                        picker.close();
                    },

                    header: Globalize.translate('HeaderSelectCertificatePath')
                });
            });
        });

        $('.dashboardHostingForm').off('submit', onSubmit).on('submit', onSubmit);

        view.querySelector('#txtCertificatePath').addEventListener('change', onCertPathChange);

        view.addEventListener('viewshow', function (e) {
            LibraryMenu.setTabs('adminadvanced', 0, getTabs);
            Dashboard.showLoadingMsg();

            ApiClient.getServerConfiguration().then(function (config) {

                loadPage(view, config);

            });
        });
    };
});
