define(['dialogHelper', 'layoutManager', 'globalize', './social-share-kit-1.0.10/dist/js/social-share-kit.min', 'css!./social-share-kit-1.0.10/dist/css/social-share-kit.css', 'emby-button'], function (dialogHelper, layoutManager, globalize) {
    'use strict';

    function showMenu(options) {

        var dlg = dialogHelper.createDialog({
            removeOnClose: true,
            autoFocus: layoutManager.tv,
            modal: false
        });

        dlg.id = 'dlg' + new Date().getTime();
        var html = '';

        html += '<h2>' + Globalize.translate('Share') + '</h2>';

        html += '<div class="ssk-group ssk-round ssk-lg">';

        // We can only do facebook if we can guarantee that the current page is available over the internet, since FB will try to probe it.
        html += '<a href="#" class="ssk ssk-facebook" style="color:#fff;"></a>';

        html += '<a href="#" class="ssk ssk-twitter" style="color:#fff;"></a>';
        html += '<a href="#" class="ssk ssk-google-plus" style="color:#fff;"></a>';
        html += '<a href="#" class="ssk ssk-pinterest" style="color:#fff;"></a>';
        html += '<a href="#" class="ssk ssk-tumblr" style="color:#fff;"></a>';
        html += '</div>';

        dlg.style.padding = '.5em 1.5em 1.5em';

        dlg.innerHTML = html;

        var isShared = false;

        var shareInfo = options.share;

        function onSskButtonClick(e) {
            isShared = true;
            dialogHelper.close(dlg);
        }

        // Has to be assigned a z-index after the call to .open() 
        var sskButtons = dlg.querySelectorAll('.ssk');
        for (var i = 0, length = sskButtons.length; i < length; i++) {
            sskButtons[i].addEventListener('click', onSskButtonClick);
        }

        dlg.addEventListener('open', function() {
            SocialShareKit.init({
                selector: '#' + dlg.id + ' .ssk',
                url: shareInfo.Url,
                title: shareInfo.Name,
                text: shareInfo.Overview,
                image: shareInfo.ImageUrl,
                via: 'Emby'
            });
        });

        return dialogHelper.open(dlg).then(function() {
            if (isShared) {
                return Promise.resolve();
            } else {
                return Promise.reject();
            }
        });
    }

    return {
        showMenu: showMenu
    };
});