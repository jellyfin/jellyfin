(function () {

    function showInfo(url) {

        var html = '';
        html += '<div class="newAppOverlay" style="background-image:url(css/images/splash.jpg);top:0;left:0;right:0;bottom:0;position:fixed;background-position:center center;background-size:100% 100%;background-repeat:no-repeat;z-index:999999;">';
        html += '<div style="background:rgba(10,10,10,.8);width:100%;height:100%;color:#eee;">';


        html += '<div class="inAppPurchaseForm" style="margin: 0 auto;padding: 30px 1em 0;max-width:600px;">';

        html += '<h1 style="color:#fff;">' + Globalize.translate('HeaderEmbyForAndroidHasMoved') + '</h1>';

        html += '<p style="margin:2em 0;">';
        html += Globalize.translate('MessageEmbyForAndroidHasMoved');
        html += '</p>';

        html += '<p>';
        html += '<a class="clearLink" href="' + (url || 'http://emby.media/download') + '" style="display:block;" target="_blank"><paper-button raised class="submit block"><iron-icon icon="check"></iron-icon><span>' + Globalize.translate('ButtonViewNewApp') + '</span></paper-button></a>';
        html += '</p>';

        html += '<p>';
        html += '<paper-button onclick="jQuery(\'.newAppOverlay\').remove();" raised class="cancelDark block"><iron-icon icon="cancel"></iron-icon><span>' + Globalize.translate('ButtonCancel') + '</span></paper-button>';
        html += '</p>';

        html += '</div>';

        html += '</div>';
        html += '</div>';

        $(document.body).append(html);
    }

    function showNewAppIfReady() {

        var platform = (window.device ? device.platform : '') || '';
        platform = platform.toLowerCase();
        var oldApp = platform.indexOf('amazon') == -1 ? 'com.mb.android.google' : 'com.mb.android.amazon';

        HttpClient.send({
            type: "GET",
            url: "https://mb3admin.com/admin/service/appstore/newAppInfo?oldApp=" + oldApp,
            dataType: 'json'

        }).done(function (result) {

            // Overly defensive, but don't want any failures here
            result = result || {};

            if (result.newAppActive) {
                showInfo(result.newAppUrl);
            }

        }).fail(function () {
            Logger.log('showNewAppIfReady failed');
        });
    }

    function showInfoIfNeeded() {

        var key = 'lastNewAppShow';
        var lastShow = parseInt(appStorage.getItem(key) || '0');

        if ((new Date().getTime() - lastShow) > 86400000) {
            showNewAppIfReady();
            appStorage.setItem(key, new Date().getTime().toString());
        } else {
            //showNewAppIfReady();
        }
    }

    setTimeout(showInfoIfNeeded, 1000);
    document.addEventListener("resume", showInfoIfNeeded, false);

})();