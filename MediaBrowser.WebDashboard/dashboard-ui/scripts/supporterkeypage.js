var SupporterKeyPage = {

    onPageShow: function () {
        SupporterKeyPage.load(this);
    },

    load: function (page) {

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
    },

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

            SupporterKeyPage.load(page);
        });

        return false;
    },

    linkSupporterKeys: function () {

        Dashboard.showLoadingMsg();
        var form = this;

        var email = $('#txtNewEmail', form).val();
        var newkey = $('#txtNewKey', form).val();
        var oldkey = $('#txtOldKey', form).val();

        var info = {
            email: email,
            newkey: newkey,
            oldkey: oldkey
        };

        var url = "https://mb3admin.com/admin/service/supporter/linkKeys";
        console.log(url);
        $.post(url, info).then(function (res) {
            var result = JSON.parse(res);
            Dashboard.hideLoadingMsg();
            if (result.Success) {
                Dashboard.alert(Globalize.translate('MessageKeysLinked'));
            } else {
                Dashboard.alert(result.ErrorMessage);
            }
            console.log(result);

        });

        return false;
    },

    retrieveSupporterKey: function () {

        Dashboard.showLoadingMsg();
        var form = this;

        var email = $('#txtEmail', form).val();

        var url = "https://mb3admin.com/admin/service/supporter/retrievekey?email=" + email;
        console.log(url);
        $.post(url).then(function (res) {
            var result = JSON.parse(res);
            Dashboard.hideLoadingMsg();
            if (result.Success) {
                Dashboard.alert(Globalize.translate('MessageKeyEmailedTo').replace("{0}", email));
            } else {
                Dashboard.alert(result.ErrorMessage);
            }
            console.log(result);

        });

        return false;
    }

};

$(document).on('pageshow', "#supporterKeyPage", SupporterKeyPage.onPageShow);