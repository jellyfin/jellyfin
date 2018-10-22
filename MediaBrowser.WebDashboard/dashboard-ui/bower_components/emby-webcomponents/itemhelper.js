define(["apphost", "globalize"], function(appHost, globalize) {
    "use strict";

    function getDisplayName(item, options) {
        if (!item) throw new Error("null item passed into getDisplayName");
        options = options || {}, "Timer" === item.Type && (item = item.ProgramInfo || item);
        var name = ("Program" !== item.Type && "Recording" !== item.Type || !item.IsSeries && !item.EpisodeTitle ? item.Name : item.EpisodeTitle) || "";
        if ("TvChannel" === item.Type) return item.ChannelNumber ? item.ChannelNumber + " " + name : name;
        if ("Episode" === item.Type && 0 === item.ParentIndexNumber) name = globalize.translate("sharedcomponents#ValueSpecialEpisodeName", name);
        else if (("Episode" === item.Type || "Program" === item.Type) && null != item.IndexNumber && null != item.ParentIndexNumber && !1 !== options.includeIndexNumber) {
            var displayIndexNumber = item.IndexNumber,
                number = displayIndexNumber,
                nameSeparator = " - ";
            !1 !== options.includeParentInfo ? number = "S" + item.ParentIndexNumber + ":E" + number : nameSeparator = ". ", item.IndexNumberEnd && (displayIndexNumber = item.IndexNumberEnd, number += "-" + displayIndexNumber), number && (name = name ? number + nameSeparator + name : number)
        }
        return name
    }

    function supportsAddingToCollection(item) {
        var invalidTypes = ["Genre", "MusicGenre", "Studio", "GameGenre", "UserView", "CollectionFolder", "Audio", "Program", "Timer", "SeriesTimer"];
        return ("Recording" !== item.Type || "Completed" === item.Status) && (!item.CollectionType && -1 === invalidTypes.indexOf(item.Type) && "Photo" !== item.MediaType && !isLocalItem(item))
    }

    function supportsAddingToPlaylist(item) {
        return "Program" !== item.Type && ("TvChannel" !== item.Type && ("Timer" !== item.Type && ("SeriesTimer" !== item.Type && ("Photo" !== item.MediaType && (("Recording" !== item.Type || "Completed" === item.Status) && (!isLocalItem(item) && ("livetv" !== item.CollectionType && (item.MediaType || item.IsFolder || "Genre" === item.Type || "MusicGenre" === item.Type || "MusicArtist" === item.Type))))))))
    }

    function canEdit(user, item) {
        var itemType = item.Type;
        return "UserRootFolder" !== itemType && "UserView" !== itemType && ("Program" !== itemType && ("Timer" !== itemType && ("SeriesTimer" !== itemType && (("Recording" !== item.Type || "Completed" === item.Status) && (!isLocalItem(item) && user.Policy.IsAdministrator)))))
    }

    function isLocalItem(item) {
        return !(!item || !item.Id || 0 !== item.Id.indexOf("local"))
    }
    return {
        getDisplayName: getDisplayName,
        supportsAddingToCollection: supportsAddingToCollection,
        supportsAddingToPlaylist: supportsAddingToPlaylist,
        isLocalItem: isLocalItem,
        canIdentify: function(user, item) {
            var itemType = item.Type;
            return !("Movie" !== itemType && "Trailer" !== itemType && "Series" !== itemType && "Game" !== itemType && "BoxSet" !== itemType && "Person" !== itemType && "Book" !== itemType && "MusicAlbum" !== itemType && "MusicArtist" !== itemType && "MusicVideo" !== itemType || !user.Policy.IsAdministrator || isLocalItem(item))
        },
        canEdit: canEdit,
        canEditImages: function(user, item) {
            var itemType = item.Type;
            return "Photo" !== item.MediaType && ("UserView" === itemType ? !!user.Policy.IsAdministrator : ("Recording" !== item.Type || "Completed" === item.Status) && ("Timer" !== itemType && "SeriesTimer" !== itemType && canEdit(user, item) && !isLocalItem(item)))
        },
        canSync: function(user, item) {
            return !(user && !user.Policy.EnableContentDownloading) && (!isLocalItem(item) && item.SupportsSync)
        },
        canShare: function(item, user) {
            return "Program" !== item.Type && ("TvChannel" !== item.Type && ("Timer" !== item.Type && ("SeriesTimer" !== item.Type && (("Recording" !== item.Type || "Completed" === item.Status) && (!isLocalItem(item) && (user.Policy.EnablePublicSharing && appHost.supports("sharing")))))))
        },
        enableDateAddedDisplay: function(item) {
            return !item.IsFolder && item.MediaType && "Program" !== item.Type && "TvChannel" !== item.Type && "Trailer" !== item.Type
        },
        canMarkPlayed: function(item) {
            if ("Program" === item.Type) return !1;
            if ("Video" === item.MediaType) {
                if ("TvChannel" !== item.Type) return !0
            } else if ("Audio" === item.MediaType) {
                if ("AudioPodcast" === item.Type) return !0;
                if ("AudioBook" === item.Type) return !0
            }
            return "Series" === item.Type || "Season" === item.Type || "BoxSet" === item.Type || "Game" === item.MediaType || "Book" === item.MediaType || "Recording" === item.MediaType
        },
        canRate: function(item) {
            return "Program" !== item.Type && "Timer" !== item.Type && "SeriesTimer" !== item.Type && "CollectionFolder" !== item.Type && "UserView" !== item.Type && "Channel" !== item.Type && "Season" !== item.Type && "Studio" !== item.Type && !!item.UserData
        },
        canConvert: function(item, user) {
            if (!user.Policy.EnableMediaConversion) return !1;
            if (isLocalItem(item)) return !1;
            var mediaType = item.MediaType;
            if ("Book" === mediaType || "Photo" === mediaType || "Game" === mediaType || "Audio" === mediaType) return !1;
            if ("livetv" === item.CollectionType) return !1;
            var type = item.Type;
            return "Channel" !== type && "Person" !== type && "Year" !== type && "Program" !== type && "Timer" !== type && "SeriesTimer" !== type && (!("Virtual" === item.LocationType && !item.IsFolder) && !item.IsPlaceHolder)
        },
        canRefreshMetadata: function(item, user) {
            if (user.Policy.IsAdministrator) {
                if ("livetv" === item.CollectionType) return !1;
                if ("Timer" !== item.Type && "SeriesTimer" !== item.Type && "Program" !== item.Type && "TvChannel" !== item.Type && ("Recording" !== item.Type || "Completed" === item.Status) && !isLocalItem(item)) return !0
            }
            return !1
        },
        supportsMediaSourceSelection: function(item) {
            return "Video" === item.MediaType && ("TvChannel" !== item.Type && (!(!item.MediaSources || 1 === item.MediaSources.length && "Placeholder" === item.MediaSources[0].Type) && (!1 !== item.EnableMediaSourceDisplay && (null != item.EnableMediaSourceDisplay || !item.SourceType || "Library" === item.SourceType))))
        }
    }
});