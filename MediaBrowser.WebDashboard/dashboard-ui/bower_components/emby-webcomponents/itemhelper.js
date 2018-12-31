define(['apphost', 'globalize'], function (appHost, globalize) {
    'use strict';

    function getDisplayName(item, options) {

        if (!item) {
            throw new Error("null item passed into getDisplayName");
        }

        options = options || {};

        if (item.Type === 'Timer') {
            item = item.ProgramInfo || item;
        }

        var name = ((item.Type === 'Program' || item.Type === 'Recording') && (item.IsSeries || item.EpisodeTitle) ? item.EpisodeTitle : item.Name) || '';

        if (item.Type === "TvChannel") {

            if (item.ChannelNumber) {
                return item.ChannelNumber + ' ' + name;
            }
            return name;
        }
        if (/*options.isInlineSpecial &&*/ item.Type === "Episode" && item.ParentIndexNumber === 0) {

            name = globalize.translate('sharedcomponents#ValueSpecialEpisodeName', name);

        } else if ((item.Type === "Episode" || item.Type === 'Program') && item.IndexNumber != null && item.ParentIndexNumber != null && options.includeIndexNumber !== false) {

            var displayIndexNumber = item.IndexNumber;

            var number = displayIndexNumber;
            var nameSeparator = " - ";

            if (options.includeParentInfo !== false) {
                number = "S" + item.ParentIndexNumber + ":E" + number;
            } else {
                nameSeparator = ". ";
            }

            if (item.IndexNumberEnd) {

                displayIndexNumber = item.IndexNumberEnd;
                number += "-" + displayIndexNumber;
            }

            if (number) {
                name = name ? (number + nameSeparator + name) : number;
            }
        }

        return name;
    }

    function supportsAddingToCollection(item) {

        var invalidTypes = ['Genre', 'MusicGenre', 'Studio', 'GameGenre', 'UserView', 'CollectionFolder', 'Audio', 'Program', 'Timer', 'SeriesTimer'];

        if (item.Type === 'Recording') {
            if (item.Status !== 'Completed') {
                return false;
            }
        }

        return !item.CollectionType && invalidTypes.indexOf(item.Type) === -1 && item.MediaType !== 'Photo' && !isLocalItem(item);
    }

    function supportsAddingToPlaylist(item) {

        if (item.Type === 'Program') {
            return false;
        }
        if (item.Type === 'TvChannel') {
            return false;
        }
        if (item.Type === 'Timer') {
            return false;
        }
        if (item.Type === 'SeriesTimer') {
            return false;
        }
        if (item.MediaType === 'Photo') {
            return false;
        }

        if (item.Type === 'Recording') {
            if (item.Status !== 'Completed') {
                return false;
            }
        }

        if (isLocalItem(item)) {
            return false;
        }
        if (item.CollectionType === 'livetv') {
            return false;
        }

        return item.MediaType || item.IsFolder || item.Type === "Genre" || item.Type === "MusicGenre" || item.Type === "MusicArtist";
    }

    function canEdit(user, item) {

        var itemType = item.Type;

        if (itemType === "UserRootFolder" || /*itemType == "CollectionFolder" ||*/ itemType === "UserView") {
            return false;
        }

        if (itemType === 'Program') {
            return false;
        }

        if (itemType === 'Timer') {
            return false;
        }

        if (itemType === 'SeriesTimer') {
            return false;
        }

        if (item.Type === 'Recording') {
            if (item.Status !== 'Completed') {
                return false;
            }
        }

        if (isLocalItem(item)) {
            return false;
        }

        return user.Policy.IsAdministrator;
    }

    function isLocalItem(item) {

        if (item && item.Id && item.Id.indexOf('local') === 0) {
            return true;
        }

        return false;
    }

    return {
        getDisplayName: getDisplayName,
        supportsAddingToCollection: supportsAddingToCollection,
        supportsAddingToPlaylist: supportsAddingToPlaylist,
        isLocalItem: isLocalItem,

        canIdentify: function (user, item) {

            var itemType = item.Type;

            if (itemType === "Movie" ||
                itemType === "Trailer" ||
                itemType === "Series" ||
                itemType === "Game" ||
                itemType === "BoxSet" ||
                itemType === "Person" ||
                itemType === "Book" ||
                itemType === "MusicAlbum" ||
                itemType === "MusicArtist" ||
                itemType === "MusicVideo") {

                if (user.Policy.IsAdministrator) {

                    if (!isLocalItem(item)) {
                        return true;
                    }
                }
            }

            return false;
        },

        canEdit: canEdit,

        canEditImages: function (user, item) {

            var itemType = item.Type;

            if (item.MediaType === 'Photo') {
                return false;
            }

            if (itemType === 'UserView') {
                if (user.Policy.IsAdministrator) {

                    return true;
                }

                return false;
            }

            if (item.Type === 'Recording') {
                if (item.Status !== 'Completed') {
                    return false;
                }
            }

            return itemType !== 'Timer' && itemType !== 'SeriesTimer' && canEdit(user, item) && !isLocalItem(item);
        },

        canSync: function (user, item) {

            if (user && !user.Policy.EnableContentDownloading) {
                return false;
            }

            if (isLocalItem(item)) {
                return false;
            }

            return item.SupportsSync;
        },

        canShare: function (item, user) {

            if (item.Type === 'Program') {
                return false;
            }
            if (item.Type === 'TvChannel') {
                return false;
            }
            if (item.Type === 'Timer') {
                return false;
            }
            if (item.Type === 'SeriesTimer') {
                return false;
            }
            if (item.Type === 'Recording') {
                if (item.Status !== 'Completed') {
                    return false;
                }
            }
            if (isLocalItem(item)) {
                return false;
            }
            return user.Policy.EnablePublicSharing && appHost.supports('sharing');
        },

        enableDateAddedDisplay: function (item) {
            return !item.IsFolder && item.MediaType && item.Type !== 'Program' && item.Type !== 'TvChannel' && item.Type !== 'Trailer';
        },

        canMarkPlayed: function (item) {

            if (item.Type === 'Program') {
                return false;
            }

            if (item.MediaType === 'Video') {
                if (item.Type !== 'TvChannel') {
                    return true;
                }
            }

            else if (item.MediaType === 'Audio') {
                if (item.Type === 'AudioPodcast') {
                    return true;
                }
                if (item.Type === 'AudioBook') {
                    return true;
                }
            }

            if (item.Type === "Series" ||
                item.Type === "Season" ||
                item.Type === "BoxSet" ||
                item.MediaType === "Game" ||
                item.MediaType === "Book" ||
                item.MediaType === "Recording") {
                return true;
            }

            return false;
        },

        canRate: function (item) {

            if (item.Type === 'Program' || item.Type === 'Timer' || item.Type === 'SeriesTimer' || item.Type === 'CollectionFolder' || item.Type === 'UserView' || item.Type === 'Channel') {
                return false;
            }

            return true;
        },

        canConvert: function (item, user) {

            if (!user.Policy.EnableMediaConversion) {
                return false;
            }

            if (isLocalItem(item)) {
                return false;
            }

            var mediaType = item.MediaType;
            if (mediaType === 'Book' || mediaType === 'Photo' || mediaType === 'Game' || mediaType === 'Audio') {
                return false;
            }

            var collectionType = item.CollectionType;
            if (collectionType === 'livetv') {
                return false;
            }

            var type = item.Type;
            if (type === 'Channel' || type === 'Person' || type === 'Year' || type === 'Program' || type === 'Timer' || type === 'SeriesTimer') {
                return false;
            }

            if (item.LocationType === 'Virtual' && !item.IsFolder) {
                return false;
            }

            if (item.IsPlaceHolder) {
                return false;
            }

            return true;
        },

        canRefreshMetadata: function (item, user) {

            if (user.Policy.IsAdministrator) {

                var collectionType = item.CollectionType;
                if (collectionType === 'livetv') {
                    return false;
                }

                if (item.Type !== 'Timer' && item.Type !== 'SeriesTimer' && item.Type !== 'Program' && item.Type !== 'TvChannel' && !(item.Type === 'Recording' && item.Status !== 'Completed')) {

                    if (!isLocalItem(item)) {
                        return true;
                    }
                }
            }

            return false;
        },

        supportsMediaSourceSelection: function (item) {

            if (item.MediaType !== 'Video') {
                return false;
            }
            if (item.Type === 'TvChannel') {
                return false;
            }
            if (!item.MediaSources || (item.MediaSources.length === 1 && item.MediaSources[0].Type === 'Placeholder')) {
                return false;
            }
            if (item.EnableMediaSourceDisplay === false) {
                return false;
            }
            if (item.EnableMediaSourceDisplay == null && item.SourceType && item.SourceType !== 'Library') {
                return false;
            }

            return true;
        }
    };
});