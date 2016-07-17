define(['playbackManager', 'inputManager', 'connectionManager', 'embyRouter', 'globalize', 'loading'], function (playbackManager, inputManager, connectionManager, embyRouter, globalize, loading) {

    function playAllFromHere(card, serverId, queue) {

        var parent = card.parentNode;
        var className = card.classList.length ? ('.' + card.classList[0]) : '';
        var cards = parent.querySelectorAll(className + '[data-id]');

        var ids = [];

        var foundCard = false;
        for (var i = 0, length = cards.length; i < length; i++) {
            if (cards[i] == card) {
                foundCard = true;
            }
            if (foundCard) {
                ids.push(cards[i].getAttribute('data-id'));
            }
        }

        if (!ids.length) {
            return;
        }

        if (queue) {
            playbackManager.queue({
                ids: ids,
                serverId: serverId
            });
        } else {
            playbackManager.play({
                ids: ids,
                serverId: serverId
            });
        }
    }

    function showSlideshow(startItemId, serverId) {

        var apiClient = connectionManager.getApiClient(serverId);
        var userId = apiClient.getCurrentUserId();

        return apiClient.getItem(userId, startItemId).then(function (item) {

            return apiClient.getItems(userId, {

                MediaTypes: 'Photo',
                Filters: 'IsNotFolder',
                ParentId: item.ParentId

            }).then(function (result) {

                var items = result.Items;

                var index = items.map(function (i) {
                    return i.Id;

                }).indexOf(startItemId);

                if (index == -1) {
                    index = 0;
                }

                require(['slideshow'], function (slideshow) {

                    var newSlideShow = new slideshow({
                        showTitle: false,
                        cover: false,
                        items: items,
                        startIndex: index,
                        interval: 8000,
                        interactive: true
                    });

                    newSlideShow.show();
                });

            });
        });
    }

    function showItem(options) {

        if (options.Type == 'Photo') {

            showSlideshow(options.Id, options.ServerId);
            return;
        }

        embyRouter.showItem(options);
    }

    function getItem(button) {

        button = parentWithAttribute(button, 'data-id');
        var serverId = button.getAttribute('data-serverid');
        var id = button.getAttribute('data-id');
        var type = button.getAttribute('data-type');

        var apiClient = connectionManager.getApiClient(serverId);

        return apiClient.getItem(apiClient.getCurrentUserId(), id);
    }

    function showContextMenu(card, options) {

        getItem(card).then(function (item) {

            var itemsContainer = options.itemsContainer || parentWithAttribute(card, 'is', 'emby-itemscontainer');

            var playlistId = itemsContainer ? itemsContainer.getAttribute('data-playlistid') : null;
            var collectionId = itemsContainer ? itemsContainer.getAttribute('data-collectionid') : null;

            if (playlistId) {
                var elem = parentWithAttribute(card, 'data-playlistitemid');
                item.PlaylistItemId = elem ? elem.getAttribute('data-playlistitemid') : null;
            }

            require(['itemContextMenu'], function (itemContextMenu) {

                itemContextMenu.show(Object.assign({
                    item: item,
                    play: true,
                    queue: true,
                    playAllFromHere: !item.IsFolder,
                    queueAllFromHere: !item.IsFolder,
                    identify: false,
                    playlistId: playlistId,
                    collectionId: collectionId

                }, options || {})).then(function (result) {

                    if (result.command == 'playallfromhere' || result.command == 'queueallfromhere') {
                        executeAction(card, options.positionTo, result.command);
                    }
                    else if (result.command == 'removefromplaylist' || result.command == 'removefromcollection') {

                        if (itemsContainer) {
                            itemsContainer.dispatchEvent(new CustomEvent('needsrefresh', {
                                detail: {},
                                cancelable: false,
                                bubbles: true
                            }));
                        }
                    }
                });
            });
        });
    }

    function showPlayMenu(card, target) {

        getItem(card).then(function (item) {

            require(['playMenu'], function (playMenu) {

                playMenu.show({

                    item: item,
                    positionTo: target
                });
            });
        });
    }

    function executeAction(card, target, action) {

        var id = card.getAttribute('data-id');

        if (!id) {
            card = parentWithAttribute(card, 'data-id');
            id = card.getAttribute('data-id');
        }

        var serverId = card.getAttribute('data-serverid');
        var type = card.getAttribute('data-type');
        var isfolder = card.getAttribute('data-isfolder') == 'true';

        if (action == 'link') {
            showItem({
                Id: id,
                Type: type,
                IsFolder: isfolder,
                ServerId: serverId
            });
        }

        else if (action == 'instantmix') {
            playbackManager.instantMix(id, serverId);
        }

        else if (action == 'play') {

            var startPositionTicks = parseInt(card.getAttribute('data-startpositionticks') || '0');

            playbackManager.play({
                ids: [id],
                startPositionTicks: startPositionTicks,
                serverId: serverId
            });
        }

        else if (action == 'playallfromhere') {
            playAllFromHere(card, serverId);
        }

        else if (action == 'queueallfromhere') {
            playAllFromHere(card, serverId, true);
        }

        else if (action == 'setplaylistindex') {
            playbackManager.currentPlaylistIndex(parseInt(card.getAttribute('data-index')));
        }

        else if (action == 'record') {
            onRecordCommand(serverId, id, type, card.getAttribute('data-timerid'), card.getAttribute('data-seriestimerid'));
        }

        else if (action == 'menu') {
            showContextMenu(card, {
                identify: false,
                positionTo: target || card
            });
        }

        else if (action == 'playmenu') {
            showPlayMenu(card, target || card);
        }
    }

    function onRecordCommand(serverId, id, type, timerId, seriesTimerId) {

        var apiClient = connectionManager.getApiClient(serverId);

        if (seriesTimerId && timerId) {

            // cancel 
            cancelTimer(apiClient, timerId, true);

        } else if (timerId) {

            // change to series recording, if possible
            // otherwise cancel individual recording
            changeRecordingToSeries(apiClient, timerId, id);

        } else if (type == 'Program') {
            // schedule recording
            createRecording(apiClient, id);
        }
    }

    function changeRecordingToSeries(apiClient, timerId, programId) {

        loading.show();

        apiClient.getItem(apiClient.getCurrentUserId(), programId).then(function (item) {

            if (item.IsSeries) {
                // cancel, then create series
                cancelTimer(apiClient, timerId, false).then(function () {
                    apiClient.getNewLiveTvTimerDefaults({ programId: programId }).then(function (timerDefaults) {

                        apiClient.createLiveTvSeriesTimer(timerDefaults).then(function () {

                            loading.hide();
                            sendToast(globalize.translate('sharedcomponents#SeriesRecordingScheduled'));
                        });
                    });
                });
            } else {
                // cancel 
                cancelTimer(apiClient, timerId, true);
            }
        });
    }

    function cancelTimer(apiClient, timerId, hideLoading) {
        loading.show();
        return apiClient.cancelLiveTvTimer(timerId).then(function () {

            if (hideLoading) {
                loading.hide();
                sendToast(globalize.translate('sharedcomponents#RecordingCancelled'));
            }
        });
    }

    function createRecording(apiClient, programId) {

        loading.show();
        apiClient.getNewLiveTvTimerDefaults({ programId: programId }).then(function (item) {

            apiClient.createLiveTvTimer(item).then(function () {

                loading.hide();
                sendToast(globalize.translate('sharedcomponents#RecordingScheduled'));
            });
        });
    }

    function sendToast(msg) {
        require(['toast'], function (toast) {
            toast(msg);
        });
    }

    function onClick(e) {
        var card = parentWithClass(e.target, 'itemAction');

        if (card) {

            var actionElement = card;
            var action = actionElement.getAttribute('data-action');

            if (!action) {
                actionElement = parentWithAttribute(actionElement, 'data-action');
                action = actionElement.getAttribute('data-action');
            }

            if (action) {
                executeAction(card, e.target, action);

                e.preventDefault();
                e.stopPropagation();
                return false;
            }
        }
    }

    function parentWithAttribute(elem, name, value) {

        while ((value ? elem.getAttribute(name) != value : !elem.getAttribute(name))) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function onCommand(e) {
        var cmd = e.detail.command;

        if (cmd == 'play' || cmd == 'record') {
            var card = parentWithClass(e.target, 'itemAction');

            if (card) {
                executeAction(card, card, cmd);
            }
        }
    }

    function on(context, options) {

        options = options || {};

        if (options.click !== false) {
            context.addEventListener('click', onClick);
        }

        if (options.command !== false) {
            inputManager.on(context, onCommand);
        }
    }

    function off(context, options) {
        options = options || {};

        context.removeEventListener('click', onClick);

        if (options.command !== false) {
            inputManager.off(context, onCommand);
        }
    }

    return {
        on: on,
        off: off,
        onClick: onClick,
        showContextMenu: showContextMenu
    };

});