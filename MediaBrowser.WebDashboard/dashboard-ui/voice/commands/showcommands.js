
define([], function () {

    return function (result) {
        result.success = true;
        switch (result.item.sourceid) {
            case 'music':
                Dashboard.navigate('music.html');
                break;
            case 'movies':
                if (result.properties.movieName) {
                    //TODO: Find a way to display movie
                    var query = {

                        Limit: 1,
                        UserId: result.userId,
                        ExcludeLocationTypes: "Virtual"
                    };


                    if (result.item.itemType) {
                        query.IncludeItemTypes = result.item.itemType;
                    }

                    query.SearchTerm = result.properties.movieName;

                    ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (queryResult) {

                        var s = queryResult[0];

                    });

                }
                else
                    Dashboard.navigate('movies.html');

                break;
            case 'tvseries':
                Dashboard.navigate('tv.html');
                break;
            case 'livetv':
                var act = result.item.menuid;
                if (act) {
                    if (act.indexOf('livetv') != -1)
                        Dashboard.navigate('livetv.html?tab=0');
                    else if (act.indexOf('guide') != -1)
                        Dashboard.navigate('livetv.html?tab=1');
                    else if (act.indexOf('channels') != -1)
                        Dashboard.navigate('livetv.html?tab=2');
                    else if (act.indexOf('recordings') != -1)
                        Dashboard.navigate('livetv.html?tab=3');
                    else if (act.indexOf('scheduled') != -1)
                        Dashboard.navigate('livetv.html?tab=4');
                    else if (act.indexOf('series') != -1)
                        Dashboard.navigate('livetv.html?tab=5');
                    else
                        Dashboard.navigate('livetv.html?tab=0');
                }
                else
                    Dashboard.navigate('livetv.html?tab=0');
                break;
            case 'recordings':
                Dashboard.navigate('livetv.html?tab=3');
                break;
            case 'latestepisodes':
                Dashboard.navigate('tv.html?tab=1');
            case 'home':
                var act = result.item.menuid;
                if (act) {
                    if (act.indexOf('home') != -1)
                        Dashboard.navigate('index.html');
                    else if (act.indexOf('nextup') != -1)
                        Dashboard.navigate('index.html?tab=2');
                    else if (act.indexOf('favorites') != -1)
                        Dashboard.navigate('index.html?tab=2');
                    else if (act.indexOf('upcoming') != -1)
                        Dashboard.navigate('index.html?tab=3');
                    else if (act.indexOf('nowplaying') != -1)
                        Dashboard.navigate('nowplaying.html');
                    else
                        Dashboard.navigate('index.html');
                }
                else
                    Dashboard.navigate('index.html');
            case 'group':
                break;
            default:
                result.success = false;
                return;
        }

    }
});