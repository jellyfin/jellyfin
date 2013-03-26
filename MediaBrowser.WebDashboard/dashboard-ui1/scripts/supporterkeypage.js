var SupporterKeyPage = {

    onPageShow: function () {
        SupporterKeyPage.load();
    },

    onPageHide: function () {

    },

    load: function() {
        Dashboard.showLoadingMsg();
        var page = $.mobile.activePage;

        ApiClient.getPluginSecurityInfo().done(function (info) {
            $('#txtSupporterKey', page).val(info.SupporterKey);
            $('#txtLegacyKey', page).val(info.LegacyKey);
            if (info.IsMBSupporter) {
                $('.supporterOnly', page).show();
            } else {
                $('.supporterOnly', page).hide();
            }
            Dashboard.hideLoadingMsg();
        });
    },

    updateSupporterKey: function () {

        Dashboard.showLoadingMsg();
        var page = $.mobile.activePage;
        
        var key = $('#txtSupporterKey', page).val();
        var legacyKey = $('#txtLegacyKey', page).val();

            var info = {
                SupporterKey: key,
                LegacyKey: legacyKey
            };

            ApiClient.updatePluginSecurityInfo(info).done(function () {
                Dashboard.resetPluginSecurityInfo();
                Dashboard.hideLoadingMsg();
                SupporterPage.load();

            });

        return false;
    },
    
    retrieveSupporterKey: function () {

        Dashboard.showLoadingMsg();
        var page = $.mobile.activePage;

        var email = $('#txtEmail', page).val();

        var url = "http://mb3admin.com/admin/service/supporter/retrievekey?email="+email;
        console.log(url);
        $.post(url).done(function (res) {
            var result = JSON.parse(res);
            Dashboard.hideLoadingMsg();
            if (result.Success) {
                Dashboard.alert("Key emailed to "+email);
            } else {
                Dashboard.showError(result.ErrorMessage);
            }
            console.log(result);

        });

        return false;
    }

};

$(document).on('pageshow', "#supporterKeyPage", SupporterKeyPage.onPageShow)
    .on('pagehide', "#supporterKeyPage", SupporterKeyPage.onPageHide);
