define(['itemHelper', 'globalize', 'apphost', 'connectionManager', 'events', 'emby-checkbox'], function (itemHelper, globalize, appHost, connectionManager, events) {
    'use strict';

    function updateSyncStatus(container, item) {

        container.querySelector('.chkOffline').checked = item.SyncPercent != null;
    }

    function syncToggle(options) {

        var self = this;

        self.options = options;

        function resetSyncStatus() {
            updateSyncStatus(options.container, options.item);
        }

        function onSyncLocalClick() {

            if (this.checked) {
                require(['syncDialog'], function (syncDialog) {
                    syncDialog.showMenu({
                        items: [options.item],
                        isLocalSync: true,
                        serverId: options.item.ServerId

                    }).then(function () {
                        events.trigger(self, 'sync');
                    }, resetSyncStatus);
                });
            } else {

                require(['confirm'], function (confirm) {

                    confirm(globalize.translate('sharedcomponents#ConfirmRemoveDownload')).then(function () {
                        connectionManager.getApiClient(options.item.ServerId).cancelSyncItems([options.item.Id]);
                    }, resetSyncStatus);
                });
            }
        }

        var container = options.container;
        var user = options.user;
        var item = options.item;

        var html = '';
        html += '<label class="checkboxContainer" style="margin: 0;">';
        html += '<input type="checkbox" is="emby-checkbox" class="chkOffline" />';
        html += '<span>' + globalize.translate('sharedcomponents#MakeAvailableOffline') + '</span>';
        html += '</label>';

        if (itemHelper.canSync(user, item)) {
            if (appHost.supports('sync')) {
                container.classList.remove('hide');
            } else {
                container.classList.add('hide');
            }

            container.innerHTML = html;

            container.querySelector('.chkOffline').addEventListener('change', onSyncLocalClick);
            updateSyncStatus(container, item);

        } else {
            container.classList.add('hide');
        }
    }

    syncToggle.prototype.refresh = function(item) {

        this.options.item = item;
        updateSyncStatus(this.options.container, item);
    };

    syncToggle.prototype.destroy = function () {

        var options = this.options;

        if (options) {
            options.container.innerHTML = '';
            this.options = null;
        }
    };

    return syncToggle;
});