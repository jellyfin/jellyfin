var AboutPage = {

    onPageShow: function () {
        AboutPage.pollForInfo();
    },


    pollForInfo: function () {
        $.getJSON("dashboardInfo").done(AboutPage.renderInfo);
    },

    renderInfo: function (dashboardInfo) {
        AboutPage.renderSystemInfo(dashboardInfo);
    },


    renderSystemInfo: function (dashboardInfo) {
        var page = $.mobile.activePage;
        $('#appVersionNumber', page).html(dashboardInfo.SystemInfo.Version);
    },

};

$(document).on('pageshow', "#aboutPage", AboutPage.onPageShow);