define([], function () {

    return {

        showPanel: function (items) {

            require(['playlisteditor'], function (playlisteditor) {
                new playlisteditor().show(items);
            });
        },

        supportsPlaylists: function (item) {

            if (item.Type == 'Program') {
                return false;
            }
            return item.RunTimeTicks || item.IsFolder || item.Type == "Genre" || item.Type == "MusicGenre" || item.Type == "MusicArtist";
        }
    };
});