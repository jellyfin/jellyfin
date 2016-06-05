define(['dialogHelper', 'layoutManager', 'globalize', './social-share-kit-1.0.4/dist/js/social-share-kit.min', 'css!./social-share-kit-1.0.4/dist/css/social-share-kit.css', 'emby-button'], function (dialogHelper, layoutManager, globalize) {

    function showMenu(options) {

        var dlg = dialogHelper.createDialog({
            removeOnClose: true,
            autoFocus: layoutManager.tv
        });

        dlg.id = 'dlg' + new Date().getTime();
        var html = '';

        html += '<h2>' + Globalize.translate('Share') + '</h2>';

        html += '<div>';
        html += '<div class="ssk-group ssk-round ssk-lg">';

        // We can only do facebook if we can guarantee that the current page is available over the internet, since FB will try to probe it.
        html += '<a href="" class="ssk ssk-facebook"></a>';

        html += '<a href="" class="ssk ssk-twitter"></a><a href="" class="ssk ssk-google-plus"></a><a href="" class="ssk ssk-pinterest"></a><a href="" class="ssk ssk-tumblr"></a></div>';
        html += '</div>';

        html += '<div class="buttons">';
        html += '<button is="emby-button" type="button" class="btnCancel">' + globalize.translate('sharedcomponents#ButtonCancel') + '</button>';
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

        function onSskButtonClick(e) {
            isShared = true;
            dialogHelper.close(dlg);
        }

        // Has to be assigned a z-index after the call to .open() 
        var sskButtons = dlg.querySelectorAll('.ssk');
        for (var i = 0, length = sskButtons.length; i < length; i++) {
            sskButtons[i].addEventListener('click', onSskButtonClick);
        }

        // Has to be assigned a z-index after the call to .open() 
        dlg.querySelector('.btnCancel').addEventListener('click', function () {
            dialogHelper.close(dlg);
        });

        var promise = new Promise(function (resolve, reject) {

            dlg.addEventListener('close', function () {
                if (isShared) {
                    resolve();
                } else {
                    reject();
                }
            });
        });

        dialogHelper.open(dlg);

        return promise;
    }

    return {
        showMenu: showMenu
    };
});