define(['thirdparty/social-share-kit-1.0.4/dist/js/social-share-kit.min', 'css!thirdparty/social-share-kit-1.0.4/dist/css/social-share-kit.css', 'fade-in-animation', 'fade-out-animation', 'paper-dialog'], function () {

    function showMenu(options, successCallback, cancelCallback) {

        var id = 'dlg' + new Date().getTime();
        var html = '';

        html += '<paper-dialog id="' + id + '" entry-animation="fade-in-animation" exit-animation="fade-out-animation" with-backdrop>';

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

        html += '</paper-dialog>';

        $(document.body).append(html);

        var isShared = false;

        setTimeout(function () {

            var dlg = document.getElementById(id);

            dlg.open();

            var shareInfo = options.share;

            SocialShareKit.init({
                selector: '#' + id + ' .ssk',
                url: shareInfo.Url,
                title: shareInfo.Name,
                text: shareInfo.Overview,
                image: shareInfo.ImageUrl,
                via: 'Emby'
            });

            // Has to be assigned a z-index after the call to .open() 
            $(dlg).on('iron-overlay-closed', function () {
                $(this).remove();

                if (isShared) {
                    successCallback(options);
                } else {
                    cancelCallback(options);
                }
            });

            // Has to be assigned a z-index after the call to .open() 
            $('.ssk', dlg).on('click', function () {
                isShared = true;
                dlg.close();
            });

        }, 100);

    }

    return {
        showMenu: showMenu
    };
});