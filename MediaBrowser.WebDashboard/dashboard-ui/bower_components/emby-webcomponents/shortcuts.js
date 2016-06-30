define(['playbackManager', 'inputManager', 'connectionManager', 'embyRouter', 'globalize', 'loading'], function (playbackManager, inputManager, connectionManager, embyRouter, globalize, loading) {

    function playAllFromHere(card, serverId) {
        var cards = card.parentNode.querySelectorAll('.itemAction[data-id]');
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
        playbackManager.play({
            ids: ids,
            serverId: serverId
        });
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

    function executeAction(card, action) {
        var id = card.getAttribute('data-id');
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

        else if (action == 'setplaylistindex') {

        }

        else if (action == 'record') {
            onRecordCommand(serverId, id, type, card.getAttribute('data-timerid'), card.getAttribute('data-seriestimerid'));
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
            var action = card.getAttribute('data-action');

            if (action) {
                executeAction(card, action);
            }
        }
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
                executeAction(card, cmd);
            }
        }
    }

    function on(context) {
        context.addEventListener('click', onClick);
        inputManager.on(context, onCommand);
    }

    function off(context) {
        context.removeEventListener('click', onClick);
        inputManager.off(context, onCommand);
    }

    return {
        on: on,
        off: off
    };

});