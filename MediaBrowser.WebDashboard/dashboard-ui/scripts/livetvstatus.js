(function ($, document, window) {

    function loadPage(page, liveTvInfo) {

        if (liveTvInfo.IsEnabled) {

            $('.liveTvStatusContent', page).show();
            $('.noLiveTvServices', page).hide();

        } else {
            $('.liveTvStatusContent', page).hide();
            $('.noLiveTvServices', page).show();
        }

        var service = liveTvInfo.Services.filter(function (s) {
            return s.Name == liveTvInfo.ActiveServiceName;

        })[0] || {};

        var serviceUrl = service.HomePageUrl || '#';

        $('#activeServiceName', page).html('<a href="' + serviceUrl + '" target="_blank">' + liveTvInfo.ActiveServiceName + '</a>').trigger('create');

        var versionHtml = service.Version || 'Unknown';

        if (service.HasUpdateAvailable) {
            versionHtml += ' <a style="margin-left: .25em;" href="' + serviceUrl + '" target="_blank">(Update available)</a>';
        }
        else {
            versionHtml += '<img src="css/images/checkmarkgreen.png" style="height: 17px; margin-left: 10px; margin-right: 0; position: relative; top: 4px;" /> Up to date!';
        }
        
        $('#activeServiceVersion', page).html(versionHtml);

        var status = liveTvInfo.Status;

        if (liveTvInfo.Status == 'Ok') {

            status = '<span style="color:green;">' + status + '</span>';
        } else {

            if (liveTvInfo.StatusMessage) {
                status += ' (' + liveTvInfo.StatusMessage + ')';
            }

            status = '<span style="color:red;">' + status + '</span>';
        }

        $('#activeServiceStatus', page).html(status);

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#liveTvStatusPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getLiveTvInfo().done(function (liveTvInfo) {

            loadPage(page, liveTvInfo);

        });

    });

})(jQuery, document, window);
