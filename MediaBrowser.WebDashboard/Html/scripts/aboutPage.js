var AboutPage = {

    onPageShow: function () {
        AboutPage.pollForInfo();
    },


    pollForInfo: function () {
        ApiClient.getSystemInfo().done(AboutPage.renderInfo);
    },

    renderInfo: function (info) {
        AboutPage.renderSystemInfo(info);
    },


    renderSystemInfo: function (info) {
        var page = $.mobile.activePage;
        $('#appVersionNumber', page).html(info.Version);
    },

};

$(document).on('pageshow', "#aboutPage", AboutPage.onPageShow);