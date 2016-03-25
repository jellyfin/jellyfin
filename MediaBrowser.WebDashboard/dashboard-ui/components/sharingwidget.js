define(['dialogHelper', 'jQuery', 'thirdparty/social-share-kit-1.0.4/dist/js/social-share-kit.min', 'css!thirdparty/social-share-kit-1.0.4/dist/css/social-share-kit.css'], function (dialogHelper, $) {

    function showMenu(options, successCallback, cancelCallback) {

        var dlg = dialogHelper.createDialog({
            removeOnClose: true,
            autoFocus: false
        });

        dlg.id = 'dlg' + new Date().getTime();
        var html = '';

        html += '<h2>' + Globalize.translate('HeaderShare') + '</h2>';

        html += '<div>';
        html += '<div class="ssk-group ssk-round ssk-lg">';

        // We can only do facebook if we can guarantee that the current page is available over the internet, since FB will try to probe it.
        if (Dashboard.isConnectMode()) {
            html += '<a href="" class="ssk ssk-facebook"></a>';
        }

        html += '<a href="" class="ssk ssk-twitter"></a><a href="" class="ssk ssk-google-plus"></a><a href="" class="ssk ssk-pinterest"></a><a href="" class="ssk ssk-tumblr"></a></div>';
        html += '</div>';

        html += '<div style="max-width:240px;">';
        html += Globalize.translate('ButtonShareHelp');
        html += '</div>';

        html += '<div class="buttons">';
        html += '<paper-button class="btnCancel" dialog-dismiss>' + Globalize.translate('ButtonCancel') + '</paper-button>';
        html += '</div>';

        dlg.innerHTML = html;

        document.body.appendChild(dlg);

        var isShared = false;

        var shareInfo = options.share;

        SocialShareKit.init({
            selector: '#' + dlg.id + ' .ssk',
            url: shareInfo.Url,
            title: shareInfo.Name,
            text: shareInfo.Overview,
            image: shareInfo.ImageUrl,
            via: 'Emby'
        });

        // Has to be assigned a z-index after the call to .open() 
        dlg.addEventListener('close', function () {

            if (isShared) {
                successCallback(options);
            } else {
                cancelCallback(options);
            }
        });

        // Has to be assigned a z-index after the call to .open() 
        $('.ssk', dlg).on('click', function () {
            isShared = true;
            dialogHelper.close(dlg);
        });

        // Has to be assigned a z-index after the call to .open() 
        dlg.querySelector('.btnCancel').addEventListener('click', function () {
            dialogHelper.close(dlg);
        });

        dialogHelper.open(dlg);
    }

    return {
        showMenu: showMenu
    };
});