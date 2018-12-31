define(['playbackManager', 'inputManager', 'connectionManager', 'appRouter', 'globalize', 'loading', 'dom', 'recordingHelper'], function (playbackManager, inputManager, connectionManager, appRouter, globalize, loading, dom, recordingHelper) {
    'use strict';

    function playAllFromHere(card, serverId, queue) {

        var parent = card.parentNode;
        var className = card.classList.length ? ('.' + card.classList[0]) : '';
        var cards = parent.querySelectorAll(className + '[data-id]');

        var ids = [];

        var foundCard = false;
        var startIndex;

        for (var i = 0, length = cards.length; i < length; i++) {
            if (cards[i] === card) {
                foundCard = true;
                startIndex = i;
            }
            if (foundCard || !queue) {
                ids.push(cards[i].getAttribute('data-id'));
            }
        }

        var itemsContainer = dom.parentWithClass(card, 'itemsContainer');
        if (itemsContainer && itemsContainer.fetchData) {

            var queryOptions = queue ? { StartIndex: startIndex } : {};

            return itemsContainer.fetchData(queryOptions).then(function (result) {

                if (queue) {
                    return playbackManager.queue({
                        items: result.Items
                    });
                } else {

                    return playbackManager.play({
                        items: result.Items,
                        startIndex: startIndex
                    });
                }
            });
        }

        if (!ids.length) {
            return;
        }

        if (queue) {
            return playbackManager.queue({
                ids: ids,
                serverId: serverId
            });
        } else {

            return playbackManager.play({
                ids: ids,
                serverId: serverId,
                startIndex: startIndex
            });
        }
    }

    function showProgramDialog(item) {

        require(['recordingCreator'], function (recordingCreator) {

            recordingCreator.show(item.Id, item.ServerId);
        });
    }

    function getItem(button) {

        button = dom.parentWithAttribute(button, 'data-id');
        var serverId = button.getAttribute('data-serverid');
        var id = button.getAttribute('data-id');
        var type = button.getAttribute('data-type');

        var apiClient = connectionManager.getApiClient(serverId);

        if (type === 'Timer') {
            return apiClient.getLiveTvTimer(id);
        }
        if (type === 'SeriesTimer') {
            return apiClient.getLiveTvSeriesTimer(id);
        }
        return apiClient.getItem(apiClient.getCurrentUserId(), id);
    }

    function notifyRefreshNeeded(childElement, itemsContainer) {

        itemsContainer = itemsContainer || dom.parentWithAttribute(childElement, 'is', 'emby-itemscontainer');

        if (itemsContainer) {
            itemsContainer.notifyRefreshNeeded(true);
        }
    }

    function showContextMenu(card, options) {

        getItem(card).then(function (item) {

            var playlistId = card.getAttribute('data-playlistid');
            var collectionId = card.getAttribute('data-collectionid');

            if (playlistId) {
                var elem = dom.parentWithAttribute(card, 'data-playlistitemid');
                item.PlaylistItemId = elem ? elem.getAttribute('data-playlistitemid') : null;
            }

            require(['itemContextMenu'], function (itemContextMenu) {

                connectionManager.getApiClient(item.ServerId).getCurrentUser().then(function (user) {
                    itemContextMenu.show(Object.assign({
                        item: item,
                        play: true,
                        queue: true,
                        playAllFromHere: !item.IsFolder,
                        queueAllFromHere: !item.IsFolder,
                        playlistId: playlistId,
                        collectionId: collectionId,
                        user: user

                    }, options || {})).then(function (result) {

                        var itemsContainer;

                        if (result.command === 'playallfromhere' || result.command === 'queueallfromhere') {
                            executeAction(card, options.positionTo, result.command);
                        }
                        else if (result.updated || result.deleted) {
                            notifyRefreshNeeded(card, options.itemsContainer);
                        }
                    });
                });
            });
        });
    }

    function getItemInfoFromCard(card) {

        return {
            Type: card.getAttribute('data-type'),
            Id: card.getAttribute('data-id'),
            TimerId: card.getAttribute('data-timerid'),
            CollectionType: card.getAttribute('data-collectiontype'),
            ChannelId: card.getAttribute('data-channelid'),
            SeriesId: card.getAttribute('data-seriesid'),
            ServerId: card.getAttribute('data-serverid'),
            MediaType: card.getAttribute('data-mediatype'),
            IsFolder: card.getAttribute('data-isfolder') === 'true',
            UserData: {
                PlaybackPositionTicks: parseInt(card.getAttribute('data-positionticks') || '0')
            }
        };
    }

    function showPlayMenu(card, target) {

        var item = getItemInfoFromCard(card);

        require(['playMenu'], function (playMenu) {

            playMenu.show({

                item: item,
                positionTo: target
            });
        });
    }

    function sendToast(text) {
        require(['toast'], function (toast) {
            toast(text);
        });
    }

    function executeAction(card, target, action) {

        target = target || card;

        var id = card.getAttribute('data-id');

        if (!id) {
            card = dom.parentWithAttribute(card, 'data-id');
            id = card.getAttribute('data-id');
        }

        var item = getItemInfoFromCard(card);

        var serverId = item.ServerId;
        var type = item.Type;

        var playableItemId = type === 'Program' ? item.ChannelId : item.Id;

        if (item.MediaType === 'Photo' && action === 'link') {
            action = 'play';
        }

        if (action === 'link') {

            appRouter.showItem(item, {
                context: card.getAttribute('data-context'),
                parentId: card.getAttribute('data-parentid')
            });
        }

        else if (action === 'programdialog') {

            showProgramDialog(item);
        }

        else if (action === 'instantmix') {
            playbackManager.instantMix({
                Id: playableItemId,
                ServerId: serverId
            });
        }

        else if (action === 'play' || action === 'resume') {

            var startPositionTicks = parseInt(card.getAttribute('data-positionticks') || '0');

            playbackManager.play({
                ids: [playableItemId],
                startPositionTicks: startPositionTicks,
                serverId: serverId
            });
        }

        else if (action === 'queue') {

            if (playbackManager.isPlaying()) {
                playbackManager.queue({
                    ids: [playableItemId],
                    serverId: serverId
                });
                sendToast(globalize.translate('sharedcomponents#MediaQueued'));
            } else {
                playbackManager.queue({
                    ids: [playableItemId],
                    serverId: serverId
                });
            }
        }

        else if (action === 'playallfromhere') {
            playAllFromHere(card, serverId);
        }

        else if (action === 'queueallfromhere') {
            playAllFromHere(card, serverId, true);
        }

        else if (action === 'setplaylistindex') {
            playbackManager.setCurrentPlaylistItem(card.getAttribute('data-playlistitemid'));
        }

        else if (action === 'record') {
            onRecordCommand(serverId, id, type, card.getAttribute('data-timerid'), card.getAttribute('data-seriestimerid'));
        }

        else if (action === 'menu') {

            var options = target.getAttribute('data-playoptions') === 'false' ?
                {
                    shuffle: false,
                    instantMix: false,
                    play: false,
                    playAllFromHere: false,
                    queue: false,
                    queueAllFromHere: false
                } :
                {};

            options.positionTo = target;

            showContextMenu(card, options);
        }

        else if (action === 'playmenu') {
            showPlayMenu(card, target);
        }

        else if (action === 'edit') {
            getItem(target).then(function (item) {
                editItem(item, serverId);
            });
        }

        else if (action === 'playtrailer') {
            getItem(target).then(playTrailer);
        }

        else if (action === 'addtoplaylist') {
            getItem(target).then(addToPlaylist);
        }

        else if (action === 'custom') {

            var customAction = target.getAttribute('data-customaction');

            card.dispatchEvent(new CustomEvent('action-' + customAction, {
                detail: {
                    playlistItemId: card.getAttribute('data-playlistitemid')
                },
                cancelable: false,
                bubbles: true
            }));
        }
    }

    function addToPlaylist(item) {
        require(['playlistEditor'], function (playlistEditor) {

            new playlistEditor().show({
                items: [item.Id],
                serverId: item.ServerId

            });
        });
    }

    function playTrailer(item) {

        var apiClient = connectionManager.getApiClient(item.ServerId);

        apiClient.getLocalTrailers(apiClient.getCurrentUserId(), item.Id).then(function (trailers) {
            playbackManager.play({ items: trailers });
        });
    }

    function editItem(item, serverId) {

        var apiClient = connectionManager.getApiClient(serverId);

        return new Promise(function (resolve, reject) {

            var serverId = apiClient.serverInfo().Id;

            if (item.Type === 'Timer') {
                if (item.ProgramId) {
                    require(['recordingCreator'], function (recordingCreator) {

                        recordingCreator.show(item.ProgramId, serverId).then(resolve, reject);
                    });
                } else {
                    require(['recordingEditor'], function (recordingEditor) {

                        recordingEditor.show(item.Id, serverId).then(resolve, reject);
                    });
                }
            } else {
                require(['metadataEditor'], function (metadataEditor) {

                    metadataEditor.show(item.Id, serverId).then(resolve, reject);
                });
            }
        });
    }

    function onRecordCommand(serverId, id, type, timerId, seriesTimerId) {

        if (type === 'Program' || timerId || seriesTimerId) {

            var programId = type === 'Program' ? id : null;
            recordingHelper.toggleRecording(serverId, programId, timerId, seriesTimerId);
        }
    }

    function onClick(e) {

        var card = dom.parentWithClass(e.target, 'itemAction');

        if (card) {

            var actionElement = card;
            var action = actionElement.getAttribute('data-action');

            if (!action) {
                actionElement = dom.parentWithAttribute(actionElement, 'data-action');
                if (actionElement) {
                    action = actionElement.getAttribute('data-action');
                }
            }

            if (action) {
                executeAction(card, actionElement, action);

                e.preventDefault();
                e.stopPropagation();
                return false;
            }
        }
    }

    function onCommand(e) {

        var cmd = e.detail.command;

        if (cmd === 'play' || cmd === 'resume' || cmd === 'record' || cmd === 'menu' || cmd === 'info') {

            var target = e.target;
            var card = dom.parentWithClass(target, 'itemAction') || dom.parentWithAttribute(target, 'data-id');

            if (card) {
                e.preventDefault();
                e.stopPropagation();
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

    function getShortcutAttributesHtml(item, serverId) {

        var html = 'data-id="' + item.Id + '" data-serverid="' + (serverId || item.ServerId) + '" data-type="' + item.Type + '" data-mediatype="' + item.MediaType + '" data-channelid="' + item.ChannelId + '" data-isfolder="' + item.IsFolder + '"';

        var collectionType = item.CollectionType;
        if (collectionType) {
            html += ' data-collectiontype="' + collectionType + '"';
        }

        return html;
    }

    return {
        on: on,
        off: off,
        onClick: onClick,
        getShortcutAttributesHtml: getShortcutAttributesHtml
    };

});