define(['connectionManager', 'playbackManager', 'globalize'], function (connectionManager, playbackManager, globalize) {
    'use strict';

    /// <summary> Play items. </summary>
    /// <param name="items"> The items. </param>
    /// <param name="shuffle"> The shuffle. </param>
    /// <returns> . </returns>
    function playItems(items, shuffle) {

        if (shuffle) {
            items = shuffleArray(items);
        }

        if (items.length) {
            var serverId = items[0].ServerId;
            items = items.map(function (i) {
                return i.Id;
            });

            playbackManager.play({
                ids: items,
                serverId: serverId
            });
        }
        else {
            require(['toast'], function (toast) {
                toast(globalize.translate('sharedcomponents#NoItemsFound'));
            });
        }
    }

    /// <summary> Shuffle array. </summary>
    /// <param name="array"> The array. </param>
    /// <returns> . </returns>
    function shuffleArray(array) {
        var currentIndex = array.length, temporaryValue, randomIndex;

        // While there remain elements to shuffle...
        while (0 !== currentIndex) {

            // Pick a remaining element...
            randomIndex = Math.floor(Math.random() * currentIndex);
            currentIndex -= 1;

            // And swap it with the current element.
            temporaryValue = array[currentIndex];
            array[currentIndex] = array[randomIndex];
            array[randomIndex] = temporaryValue;
        }

        return array;
    }

    return function (result) {

        return function () {
            var query = {
                Limit: result.item.limit || 100,
                UserId: result.userId,
                ExcludeLocationTypes: "Virtual"
            };

            if (result.item.itemType) {
                query.IncludeItemTypes = result.item.itemType;
            }

            var apiClient = connectionManager.currentApiClient();

            if (result.item.sourceid === 'nextup') {

                apiClient.getNextUpEpisodes(query).then(function (queryResult) {

                    playItems(queryResult.Items, result.item.shuffle);

                });
            }

            if (result.item.shuffle) {
                result.item.sortBy = result.sortBy ? 'Random,' + result.item.sortBy : 'Random';
            }

            query.SortBy = result.item.sortBy;
            query.SortOrder = result.item.sortOrder;
            query.Recursive = true;

            if (result.item.filters.indexOf('unplayed') !== -1) {
                query.IsPlayed = false;
            }
            if (result.item.filters.indexOf('played') !== -1) {
                query.IsPlayed = true;
            }
            if (result.item.filters.indexOf('favorite') !== -1) {
                query.Filters = 'IsFavorite';
            }

            apiClient.getItems(apiClient.getCurrentUserId(), query).then(function (queryResult) {

                playItems(queryResult.Items, result.item.shuffle);
            });
        };
    };
});