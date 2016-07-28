define(['serverNotifications', 'playbackManager', 'events', 'globalize', 'require'], function (serverNotifications, playbackManager, events, globalize, require) {

    function onOneDocumentClick() {

        document.removeEventListener('click', onOneDocumentClick);
        document.removeEventListener('keydown', onOneDocumentClick);

        if (window.Notification) {
            Notification.requestPermission();
        }
    }
    document.addEventListener('click', onOneDocumentClick);
    document.addEventListener('keydown', onOneDocumentClick);

    var serviceWorkerRegistration;

    function closeAfter(notification, timeoutMs) {

        setTimeout(function () {

            if (notification.close) {
                notification.close();
            }
            else if (notification.cancel) {
                notification.cancel();
            }
        }, timeoutMs);
    }

    function resetRegistration() {
        navigator.serviceWorker.ready.then(function (registration) {
            serviceWorkerRegistration = registration;
        });
    }

    resetRegistration();

    function show(title, options, timeoutMs) {

        resetRegistration();

        if (serviceWorkerRegistration && !timeoutMs) {
            serviceWorkerRegistration.showNotification(title, options);
            return;
        }

        try {
            var notif = new Notification(title, options);

            if (notif.show) {
                notif.show();
            }

            if (timeoutMs) {
                closeAfter(notif, timeoutMs);
            }
        } catch (err) {
            if (options.actions) {
                options.actions = [];
                show(title, options, timeoutMs);
            } else {
                throw err;
            }
        }
    }

    function showNewItemNotification(item, apiClient) {

        var notification = {
            title: "New " + item.Type,
            body: item.Name,
            vibrate: true,
            tag: "newItem" + item.Id,
            data: {
                //options: {
                //    url: LibraryBrowser.getHref(item)
                //}
            }
        };

        var imageTags = item.ImageTags || {};

        if (imageTags.Primary) {

            notification.icon = apiClient.getScaledImageUrl(item.Id, {
                width: 80,
                tag: imageTags.Primary,
                type: "Primary"
            });
        }

        show(notification.title, notification, 15000);
    }

    function onLibraryChanged(data, apiClient) {

        var newItems = data.ItemsAdded;

        if (!newItems.length || !window.Notification || Notification.permission !== "granted") {
            return;
        }

        if (playbackManager.isPlayingVideo()) {
            return;
        }

        apiClient.getItems(apiClient.getCurrentUserId(), {

            Recursive: true,
            Limit: 3,
            IsFolder: false,
            SortBy: "DateCreated",
            SortOrder: "Descending",
            ImageTypes: "Primary",
            Ids: newItems.join(',')

        }).then(function (result) {

            var items = result.Items;

            for (var i = 0, length = items.length ; i < length; i++) {

                showNewItemNotification(items[i], apiClient);
            }
        });
    }

    function getIconUrl(name) {
        return require.toUrl('.').split('?')[0] + '/' + name;
    }

    function showPackageInstallNotification(apiClient, installation, status) {

        apiClient.getCurrentUser().then(function (user) {

            if (!user.Policy.IsAdministrator) {
                return;
            }

            var notification = {
                tag: "install" + installation.Id,
                data: {},
                icon: getIconUrl('/notificationicon.png')
            };

            if (status == 'completed') {
                notification.title = globalize.translate('sharedcomponents#PackageInstallCompleted').replace('{0}', installation.Name + ' ' + installation.Version);
                notification.vibrate = true;
            }
            else if (status == 'cancelled') {
                notification.title = globalize.translate('sharedcomponents#PackageInstallCancelled').replace('{0}', installation.Name + ' ' + installation.Version);
            }
            else if (status == 'failed') {
                notification.title = globalize.translate('sharedcomponents#PackageInstallFailed').replace('{0}', installation.Name + ' ' + installation.Version);
                notification.vibrate = true;
            }
            else if (status == 'progress') {
                notification.title = globalize.translate('sharedcomponents#InstallingPackage').replace('{0}', installation.Name + ' ' + installation.Version);

                //notification.actions =
                //[
                //    { action: 'cancel', title: globalize.translate('sharedcomponents#ButtonCancel')/*, icon: 'https://example/like.png'*/ }
                //];
            }

            if (status == 'progress') {

                var percentComplete = Math.round(installation.PercentComplete || 0);

                notification.body = percentComplete + '% complete.';
            }

            var timeout = status == 'cancelled' ? 5000 : 0;

            show(notification.title, notification, timeout);
        });
    }

    events.on(serverNotifications, 'LibraryChanged', function (e, apiClient, data) {
        onLibraryChanged(data, apiClient);
    });

    events.on(serverNotifications, 'PackageInstallationCompleted', function (e, apiClient, data) {
        showPackageInstallNotification(apiClient, data, "completed");
    });

    events.on(serverNotifications, 'PackageInstallationFailed', function (e, apiClient, data) {
        showPackageInstallNotification(apiClient, data, "failed");
    });

    events.on(serverNotifications, 'PackageInstallationCancelled', function (e, apiClient, data) {
        showPackageInstallNotification(apiClient, data, "cancelled");
    });

    events.on(serverNotifications, 'PackageInstalling', function (e, apiClient, data) {
        showPackageInstallNotification(apiClient, data, "progress");
    });

});