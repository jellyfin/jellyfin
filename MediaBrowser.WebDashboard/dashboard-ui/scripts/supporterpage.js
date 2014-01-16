var SupporterPage = {

    onPageShow: function () {
        SupporterPage.load();
    },

    onPageHide: function () {

    },

    load: function() {
        Dashboard.showLoadingMsg();
        var page = $.mobile.activePage;

        ApiClient.getPluginSecurityInfo().done(function (info) {
            $('#txtSupporterKey', page).val(info.SupporterKey);
            if (info.IsMBSupporter) {
                $('.supporterOnly', page).show();
            } else {
                $('.supporterOnly', page).hide();
            }
            $('#paypalReturnUrl', page).val(ApiClient.getUrl("supporterkey.html"));
            Dashboard.hideLoadingMsg();
        });
    }
};

$(document).on('pageshow', "#supporterPage", SupporterPage.onPageShow)
    .on('pagehide', "#supporterPage", SupporterPage.onPageHide);
