define(['playbackManager', 'inputManager', 'connectionManager', 'embyRouter', 'globalize', 'loading', 'dom', 'recordingHelper'], function (playbackManager, inputManager, connectionManager, embyRouter, globalize, loading, dom, recordingHelper) {
    'use strict';

    function playAllFromHere(card, serverId, queue) {

        var parent = card.parentNode;
        var className = card.classList.length ? ('.' + card.classList[0]) : '';
        var cards = parent.querySelectorAll(className + '[data-id]');

        var ids = [];

        var foundCard = false;
        for (var i = 0, length = cards.length; i < length; i++) {
            if (cards[i] === card) {
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
                ParentId: item.ParentId,
                SortBy: 'SortName'

            }).then(function (result) {

                var items = result.Items;

                var index = items.map(function (i) {
                    return i.Id;

                }).indexOf(startItemId);

                if (index === -1) {
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

    function showItem(item, options) {

        if (item.Type === 'Photo') {

            showSlideshow(item.Id, item.ServerId);
            return;
        }

        embyRouter.showItem(item, options);
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

    function showContextMenu(card, options) {

        getItem(card).then(function (item) {

            var playlistId = card.getAttribute('data-playlistid');
            var collectionId = card.getAttribute('data-collectionid');

            if (playlistId) {
                var elem = dom.parentWithAttribute(card, 'data-playlistitemid');
                item.PlaylistItemId = elem ? elem.getAttribute('data-playlistitemid') : null;
            }

            require(['itemContextMenu'], function (itemContextMenu) {

                itemContextMenu.show(Object.assign({
                    item: item,
                    play: true,
                    queue: true,
                    playAllFromHere: !item.IsFolder,
                    queueAllFromHere: !item.IsFolder,
                    playlistId: playlistId,
                    collectionId: collectionId

                }, options || {})).then(function (result) {

                    var itemsContainer;

                    if (result.command === 'playallfromhere' || result.command === 'queueallfromhere') {
                        executeAction(card, options.positionTo, result.command);
                    }
                    else if (result.command === 'removefromplaylist' || result.command === 'removefromcollection') {

                        itemsContainer = options.itemsContainer || dom.parentWithAttribute(card, 'is', 'emby-itemscontainer');

                        if (itemsContainer) {
                            itemsContainer.dispatchEvent(new CustomEvent('needsrefresh', {
                                detail: {},
                                cancelable: false,
                                bubbles: true
                            }));
                        }
                    }
                    else if (result.command === 'canceltimer') {

                        itemsContainer = options.itemsContainer || dom.parentWithAttribute(card, 'is', 'emby-itemscontainer');

                        if (itemsContainer) {
                            itemsContainer.dispatchEvent(new CustomEvent('timercancelled', {
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

        if (action === 'link') {

            showItem(item, {
                context: card.getAttribute('data-context')
            });
        }

        else if (action === 'programdialog') {

            showProgramDialog(item);
        }

        else if (action === 'instantmix') {
            playbackManager.instantMix(id, serverId);
        }

        else if (action === 'play') {

            var startPositionTicks = parseInt(card.getAttribute('data-positionticks') || '0');

            playbackManager.play({
                ids: [id],
                startPositionTicks: startPositionTicks,
                serverId: serverId
            });
        }

        else if (action === 'playallfromhere') {
            playAllFromHere(card, serverId);
        }

        else if (action === 'queueallfromhere') {
            playAllFromHere(card, serverId, true);
        }

        else if (action === 'setplaylistindex') {
            playbackManager.currentPlaylistIndex(parseInt(card.getAttribute('data-index')));
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
            recordingHelper.toggle(serverId, programId, timerId, seriesTimerId);
        }
    }

    function onClick(e) {

        var card = dom.parentWithClass(e.target, 'itemAction');

        if (card) {

            var actionElement = card;
            var action = actionElement.getAttribute('data-action');

            if (!action) {
                actionElement = dom.parentWithAttribute(actionElement, 'data-action');
                action = actionElement.getAttribute('data-action');
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

        if (cmd === 'play' || cmd === 'record' || cmd === 'menu' || cmd === 'info') {
            var card = dom.parentWithClass(e.target, 'itemAction');

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

    return {
        on: on,
        off: off,
        onClick: onClick,
        showContextMenu: showContextMenu
    };

});