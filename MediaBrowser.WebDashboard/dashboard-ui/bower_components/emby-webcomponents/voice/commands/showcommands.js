define(['inputManager', 'connectionManager', 'embyRouter'], function (inputManager, connectionManager, embyRouter) {

    return function (result) {
        result.success = true;
        switch (result.item.sourceid) {
            case 'music':
                inputManager.trigger('music');
                break;
            case 'movies':
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

                break;
            case 'tvseries':
                inputManager.trigger('tv');
                break;
            case 'livetv':
                var act = result.item.menuid;
                if (act) {
                    if (act.indexOf('livetv') != -1) {
                        inputManager.trigger('livetv');
                    } else if (act.indexOf('guide') != -1) {
                        inputManager.trigger('guide');
                    } else if (act.indexOf('channels') != -1) {
                        inputManager.trigger('livetv');
                    } else if (act.indexOf('recordings') != -1) {
                        inputManager.trigger('recordedtv');
                    } else if (act.indexOf('scheduled') != -1) {
                        inputManager.trigger('recordedtv');
                    } else if (act.indexOf('series') != -1) {
                        inputManager.trigger('recordedtv');
                    } else {
                        inputManager.trigger('livetv');
                    }
                } else {
                    inputManager.trigger('livetv');
                }
                break;
            case 'recordings':
                inputManager.trigger('recordedtv');
                break;
            case 'latestepisodes':
                inputManager.trigger('latestepisodes');
            case 'home':
                var act = result.item.menuid;
                if (act) {
                    if (act.indexOf('home') != -1) {
                        inputManager.trigger('home');
                    }
                    else if (act.indexOf('nextup') != -1) {
                        inputManager.trigger('nextup');
                    }
                    else if (act.indexOf('favorites') != -1) {
                        inputManager.trigger('favorites');
                    } else if (act.indexOf('upcoming') != -1) {
                        inputManager.trigger('upcomingtv');
                    }
                    else if (act.indexOf('nowplaying') != -1) {
                        inputManager.trigger('nowplaying');
                    }
                    else {
                        inputManager.trigger('home');
                    }
                } else {
                    inputManager.trigger('home');
                }
            case 'group':
                break;
            default:
                result.success = false;
                return;
        }

    }
});