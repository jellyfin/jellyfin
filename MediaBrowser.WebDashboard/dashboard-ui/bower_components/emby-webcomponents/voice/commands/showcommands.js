define(['inputManager', 'connectionManager', 'embyRouter'], function (inputManager, connectionManager, embyRouter) {
    'use strict';

    function getMusicCommand(result) {
        return function () {
            inputManager.trigger('music');
        };
    }

    function getMoviesCommand(result) {
        return function () {
            if (result.properties.movieName) {

                //TODO: Find a way to display movie
                var query = {
                    Limit: 1,
                    UserId: result.userId,
                    ExcludeLocationTypes: "Virtual",
                    NameStartsWith: result.item.itemType
                };

                if (result.item.itemType) {
                    query.IncludeItemTypes = result.item.itemType;
                }

                var apiClient = connectionManager.currentApiClient();
                apiClient.getItems(apiClient.getCurrentUserId(), query).then(function (queryResult) {

                    if (queryResult.Items.length) {
                        embyRouter.showItem(queryResult.Items[0]);
                    }
                });

            } else {
                inputManager.trigger('movies');
            }
        };
    }

    function getTVCommand(result) {
        return function () {
            inputManager.trigger('tv');
        };
    }

    function getLiveTVCommand(result) {
        return function () {
            var act = result.item.menuid;
            if (act) {
                if (act.indexOf('livetv') !== -1) {
                    inputManager.trigger('livetv');
                } else if (act.indexOf('guide') !== -1) {
                    inputManager.trigger('guide');
                } else if (act.indexOf('channels') !== -1) {
                    inputManager.trigger('livetv');
                } else if (act.indexOf('recordings') !== -1) {
                    inputManager.trigger('recordedtv');
                } else if (act.indexOf('scheduled') !== -1) {
                    inputManager.trigger('recordedtv');
                } else if (act.indexOf('series') !== -1) {
                    inputManager.trigger('recordedtv');
                } else {
                    inputManager.trigger('livetv');
                }
            } else {
                inputManager.trigger('livetv');
            }
        };
    }

    function getRecordingsCommand(result) {
        return function () {
            inputManager.trigger('recordedtv');
        };
    }

    function getLatestEpisodesCommand(result) {
        return function () {
            inputManager.trigger('latestepisodes');
        };
    }

    function getHomeCommand(result) {
        return function () {
            var act = result.item.menuid;
            if (act) {
                if (act.indexOf('home') !== -1) {
                    inputManager.trigger('home');
                }
                else if (act.indexOf('nextup') !== -1) {
                    inputManager.trigger('nextup');
                }
                else if (act.indexOf('favorites') !== -1) {
                    inputManager.trigger('favorites');
                } else if (act.indexOf('upcoming') !== -1) {
                    inputManager.trigger('upcomingtv');
                }
                else if (act.indexOf('nowplaying') !== -1) {
                    inputManager.trigger('nowplaying');
                }
                else {
                    inputManager.trigger('home');
                }
            } else {
                inputManager.trigger('home');
            }
        };
    }

    return function (result) {

        switch (result.item.sourceid) {
            case 'music':
                return getMusicCommand(result);
            case 'movies':
                return getMoviesCommand(result);
            case 'tvseries':
                return getTVCommand(result);
            case 'livetv':
                return getLiveTVCommand(result);
            case 'recordings':
                return getRecordingsCommand(result);
            case 'latestepisodes':
                return getLatestEpisodesCommand(result);
            case 'home':
                return getHomeCommand(result);
            case 'group':
                return;
            default:
                return;
        }

    };
});