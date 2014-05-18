var SupporterKeyPage = {

    onPageShow: function () {
        SupporterKeyPage.load(this);
    },

    load: function (page) {

        Dashboard.showLoadingMsg();

        ApiClient.getPluginSecurityInfo().done(function (info) {

            $('#txtSupporterKey', page).val(info.SupporterKey);

            if (info.SupporterKey && !info.IsMBSupporter) {
                $('#txtSupporterKey', page).addClass("invalidEntry");
                $('.notSupporter', page).show();
            } else {
                $('#txtSupporterKey', page).removeClass("invalidEntry");
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

        ApiClient.updatePluginSecurityInfo(info).done(function () {

            Dashboard.resetPluginSecurityInfo();
            Dashboard.hideLoadingMsg();

            if (key) {

                Dashboard.alert({
                    message: "Thank you. Your supporter key has been updated.",
                    title: "Confirmation"
                });

            } else {
                Dashboard.alert({
                    message: "Thank you. Your supporter key has been removed.",
                    title: "Confirmation"
                });
            }

            var page = $(form).parents('.page');

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

        var url = "http://mb3admin.com/admin/service/supporter/linkKeys";
        console.log(url);
        $.post(url, info).done(function (res) {
            var result = JSON.parse(res);
            Dashboard.hideLoadingMsg();
            if (result.Success) {
                Dashboard.alert("Keys Linked.");
            } else {
                Dashboard.showError(result.ErrorMessage);
            }
            console.log(result);

        });

        return false;
    },

    retrieveSupporterKey: function () {

        Dashboard.showLoadingMsg();
        var form = this;

        var email = $('#txtEmail', form).val();

        var url = "http://mb3admin.com/admin/service/supporter/retrievekey?email=" + email;
        console.log(url);
        $.post(url).done(function (res) {
            var result = JSON.parse(res);
            Dashboard.hideLoadingMsg();
            if (result.Success) {
                Dashboard.alert("Key emailed to " + email);
            } else {
                Dashboard.showError(result.ErrorMessage);
            }
            console.log(result);

        });

        return false;
    }

};

$(document).on('pageshow', "#supporterKeyPage", SupporterKeyPage.onPageShow);
