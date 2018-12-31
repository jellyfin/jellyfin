define(['loading', 'events', 'dialogHelper', 'dom', 'layoutManager', 'scrollHelper', 'globalize', 'require', 'material-icons', 'emby-button', 'paper-icon-button-light', 'emby-input', 'formDialogStyle', 'flexStyles'], function (loading, events, dialogHelper, dom, layoutManager, scrollHelper, globalize, require) {
    'use strict';

    var currentApiClient;
    var currentDlg;
    var currentInstance;

    function reloadPageWhenServerAvailable(retryCount) {

        var apiClient = currentApiClient;

        if (!apiClient) {
            return;
        }

        // Don't use apiclient method because we don't want it reporting authentication under the old version
        apiClient.getJSON(apiClient.getUrl("System/Info")).then(function (info) {

            // If this is back to false, the restart completed
            if (!info.IsShuttingDown) {
                currentInstance.restarted = true;
                dialogHelper.close(currentDlg);
            } else {
                retryReload(retryCount);
            }

        }, function () {
            retryReload(retryCount);
        });
    }

    function retryReload(retryCount) {
        setTimeout(function () {

            retryCount = retryCount || 0;
            retryCount++;

            if (retryCount < 150) {
                reloadPageWhenServerAvailable(retryCount);
            }
        }, 500);
    }

    function startRestart(instance, apiClient, dlg) {

        currentApiClient = apiClient;
        currentDlg = dlg;
        currentInstance = instance;

        apiClient.restartServer().then(function () {

            setTimeout(reloadPageWhenServerAvailable, 250);

        });
    }

    function showDialog(instance, options, template) {

        var dialogOptions = {
            removeOnClose: true,
            scrollY: false
        };

        var enableTvLayout = layoutManager.tv;

        if (enableTvLayout) {
            dialogOptions.size = 'fullscreen';
        }

        var dlg = dialogHelper.createDialog(dialogOptions);

        var configuredButtons = [];

        dlg.classList.add('formDialog');

        dlg.innerHTML = globalize.translateHtml(template, 'sharedcomponents');

        dlg.classList.add('align-items-center');
        dlg.classList.add('justify-items-center');

        var formDialogContent = dlg.querySelector('.formDialogContent');
        formDialogContent.style['flex-grow'] = 'initial';

        if (enableTvLayout) {
            formDialogContent.style['max-width'] = '50%';
            formDialogContent.style['max-height'] = '60%';
            scrollHelper.centerFocus.on(formDialogContent, false);
        } else {
            formDialogContent.style.maxWidth = (Math.min((configuredButtons.length * 150) + 200, dom.getWindowSize().innerWidth - 50)) + 'px';
            dlg.classList.add('dialog-fullscreen-lowres');
        }

        //dlg.querySelector('.btnCancel').addEventListener('click', function (e) {
        //    dialogHelper.close(dlg);
        //});

        dlg.querySelector('.formDialogHeaderTitle').innerHTML = globalize.translate('sharedcomponents#HeaderRestartingEmbyServer');

        dlg.querySelector('.text').innerHTML = globalize.translate('sharedcomponents#RestartPleaseWaitMessage');

        var i, length;
        var html = '';
        for (i = 0, length = configuredButtons.length; i < length; i++) {

            var item = configuredButtons[i];
            var autoFocus = i === 0 ? ' autofocus' : '';

            var buttonClass = 'btnOption raised formDialogFooterItem formDialogFooterItem-autosize';

            if (item.type) {
                buttonClass += ' button-' + item.type;
            }

            html += '<button is="emby-button" type="button" class="' + buttonClass + '" data-id="' + item.id + '"' + autoFocus + '>' + item.name + '</button>';
        }

        dlg.querySelector('.formDialogFooter').innerHTML = html;

        function onButtonClick() {
            dialogHelper.close(dlg);
        }

        var buttons = dlg.querySelectorAll('.btnOption');
        for (i = 0, length = buttons.length; i < length; i++) {
            buttons[i].addEventListener('click', onButtonClick);
        }

        var dlgPromise = dialogHelper.open(dlg);

        startRestart(instance, options.apiClient, dlg);

        return dlgPromise.then(function () {

            if (enableTvLayout) {
                scrollHelper.centerFocus.off(dlg.querySelector('.formDialogContent'), false);
            }

            instance.destroy();
            loading.hide();

            if (instance.restarted) {
                events.trigger(instance, 'restarted');
            }
        });
    }

    function ServerRestartDialog(options) {

        this.options = options;
    }

    ServerRestartDialog.prototype.show = function () {

        var instance = this;
        loading.show();

        return new Promise(function (resolve, reject) {
            require(['text!./../dialog/dialog.template.html'], function (template) {
                showDialog(instance, instance.options, template).then(resolve, reject);
            });
        });
    };

    ServerRestartDialog.prototype.destroy = function () {

        currentApiClient = null;
        currentDlg = null;
        currentInstance = null;
        this.options = null;
    };

    return ServerRestartDialog;
});