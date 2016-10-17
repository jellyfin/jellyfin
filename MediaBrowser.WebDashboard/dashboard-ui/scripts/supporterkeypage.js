define(['fetchHelper', 'jQuery', 'registrationServices'], function (fetchHelper, $, registrationServices) {

    function load(page) {
        Dashboard.showLoadingMsg();

        ApiClient.getPluginSecurityInfo().then(function (info) {

            $('#txtSupporterKey', page).val(info.SupporterKey);

            if (info.SupporterKey && !info.IsMBSupporter) {
                page.querySelector('#txtSupporterKey').classList.add('invalidEntry');
                $('.notSupporter', page).show();
            } else {
                page.querySelector('#txtSupporterKey').classList.remove('invalidEntry');
                $('.notSupporter', page).hide();
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function loadUserInfo(page) {

        Dashboard.getPluginSecurityInfo().then(function (info) {

            if (info.IsMBSupporter) {
                $('.supporterContainer', page).addClass('hide');
            } else {
                $('.supporterContainer', page).removeClass('hide');
            }
        });
    }

    function retrieveSupporterKey() {
        Dashboard.showLoadingMsg();
        var form = this;

        var email = $('#txtEmail', form).val();

        var url = "https://mb3admin.com/admin/service/supporter/retrievekey?email=" + email;
        console.log(url);
        fetchHelper.ajax({

            url: url,
            type: 'POST',
            dataType: 'json'

        }).then(function (result) {

            Dashboard.hideLoadingMsg();
            if (result.Success) {
                require(['toast'], function (toast) {
                    toast(Globalize.translate('MessageKeyEmailedTo').replace("{0}", email));
                });
            } else {
                require(['toast'], function (toast) {
                    toast(result.ErrorMessage);
                });
            }
            console.log(result);

        });

        return false;
    }

    var SupporterKeyPage = {

        updateSupporterKey: function () {

            Dashboard.showLoadingMsg();
            var form = this;

            var key = $('#txtSupporterKey', form).val();

            var info = {
                SupporterKey: key
            };

            ApiClient.updatePluginSecurityInfo(info).then(function () {

                Dashboard.resetPluginSecurityInfo();
                Dashboard.hideLoadingMsg();

                if (key) {

                    Dashboard.alert({
                        message: Globalize.translate('MessageKeyUpdated'),
                        title: Globalize.translate('HeaderConfirmation')
                    });

                } else {
                    Dashboard.alert({
                        message: Globalize.translate('MessageKeyRemoved'),
                        title: Globalize.translate('HeaderConfirmation')
                    });
                }

                var page = $(form).parents('.page')[0];

                load(page);
            });

            return false;
        },

        linkSupporterKeys: function () {

            Dashboard.showLoadingMsg();
            var form = this;

            var email = $('#txtNewEmail', form).val();
            var newkey = $('#txtNewKey', form).val();
            var oldkey = $('#txtOldKey', form).val();

            var url = "https://mb3admin.com/admin/service/supporter/linkKeys";
            console.log(url);
            fetchHelper.ajax({

                url: url,
                type: 'POST',
                dataType: 'json',
                query: {
                    email: email,
                    newkey: newkey,
                    oldkey: oldkey
                }

            }).then(function (result) {

                Dashboard.hideLoadingMsg();
                if (result.Success) {
                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageKeysLinked'));
                    });
                } else {
                    require(['toast'], function (toast) {
                        toast(result.ErrorMessage);
                    });
                }
                console.log(result);

            });

            return false;
        }
    };

    function onSupporterLinkClick(e) {

        registrationServices.showPremiereInfo();
        e.preventDefault();
        e.stopPropagation();
    }

    $(document).on('pageinit', "#supporterKeyPage", function () {

        var page = this;
        $('#supporterKeyForm', this).on('submit', SupporterKeyPage.updateSupporterKey);
        $('#lostKeyForm', this).on('submit', retrieveSupporterKey);
        $('#linkKeysForm', this).on('submit', SupporterKeyPage.linkSupporterKeys);

        page.querySelector('.benefits').innerHTML = Globalize.translate('HeaderSupporterBenefit', '<a class="lnkPremiere" href="http://emby.media/premiere" target="_blank">', '</a>');

        page.querySelector('.lnkPremiere').addEventListener('click', onSupporterLinkClick);

    }).on('pageshow', "#supporterKeyPage", function () {

        var page = this;
        loadUserInfo(page);
        load(page);
    });

});