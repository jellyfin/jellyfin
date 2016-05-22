define([], function () {

    function getDisplayName(item, options) {

        if (!item) {
            throw new Error("null item passed into getDisplayName");
        }

        options = options || {};

        var name = item.EpisodeTitle || item.Name || '';

        if (item.Type == "TvChannel") {

            if (item.Number) {
                return item.Number + ' ' + name;
            }
            return name;
        }
        if (/*options.isInlineSpecial &&*/ item.Type == "Episode" && item.ParentIndexNumber == 0) {

            name = Globalize.translate('sharedcomponents#ValueSpecialEpisodeName', name);

        } else if ((item.Type == "Episode" || item.Type == 'Program') && item.IndexNumber != null && item.ParentIndexNumber != null) {

            var displayIndexNumber = item.IndexNumber;

            var number = "E" + displayIndexNumber;

            if (options.includeParentInfo !== false) {
                number = "S" + item.ParentIndexNumber + ", " + number;
            }

            if (item.IndexNumberEnd) {

                displayIndexNumber = item.IndexNumberEnd;
                number += "-" + displayIndexNumber;
            }

            name = number + " - " + name;

        }

        if (item.EpisodeTitle) {
            name = item.Name + ' ' + name;
        }

        return name;
    }

    function supportsAddingToCollection(item) {
        var invalidTypes = ['Person', 'Genre', 'MusicGenre', 'Studio', 'GameGenre', 'BoxSet', 'Playlist', 'UserView', 'CollectionFolder', 'Audio', 'TvChannel', 'Program', 'MusicAlbum', 'Timer'];

        return !item.CollectionType && invalidTypes.indexOf(item.Type) == -1 && item.MediaType != 'Photo';
    }

    function supportsAddingToPlaylist(item) {
        if (item.Type == 'Program') {
            return false;
        }
        if (item.Type == 'Timer') {
            return false;
        }
        return item.RunTimeTicks || item.IsFolder || item.Type == "Genre" || item.Type == "MusicGenre" || item.Type == "MusicArtist";
    }

    return {
        getDisplayName: getDisplayName,
        supportsAddingToCollection: supportsAddingToCollection,
        supportsAddingToPlaylist: supportsAddingToPlaylist
    };
});