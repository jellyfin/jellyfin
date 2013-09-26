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
            if ((info.LegacyKey || info.SupporterKey) && !info.IsMBSupporter) {
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

                SupporterKeyPage.load();
            });

        return false;
    },
    
    linkSupporterKeys: function () {

        Dashboard.showLoadingMsg();
        var page = $.mobile.activePage;
        
        var email = $('#txtNewEmail', page).val();
        var newkey = $('#txtNewKey', page).val();
        var oldkey = $('#txtOldKey', page).val();

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
