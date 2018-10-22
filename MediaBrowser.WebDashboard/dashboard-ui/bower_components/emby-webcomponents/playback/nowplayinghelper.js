define([], function() {
    "use strict";

    function getNowPlayingNames(nowPlayingItem, includeNonNameInfo) {
        var topItem = nowPlayingItem,
            bottomItem = null,
            topText = nowPlayingItem.Name;
        nowPlayingItem.AlbumId && "Audio" === nowPlayingItem.MediaType && (topItem = {
            Id: nowPlayingItem.AlbumId,
            Name: nowPlayingItem.Album,
            Type: "MusicAlbum",
            IsFolder: !0
        }), "Video" === nowPlayingItem.MediaType && (null != nowPlayingItem.IndexNumber && (topText = nowPlayingItem.IndexNumber + " - " + topText), null != nowPlayingItem.ParentIndexNumber && (topText = nowPlayingItem.ParentIndexNumber + "." + topText));
        var bottomText = "";
        nowPlayingItem.ArtistItems && nowPlayingItem.ArtistItems.length ? (bottomItem = {
            Id: nowPlayingItem.ArtistItems[0].Id,
            Name: nowPlayingItem.ArtistItems[0].Name,
            Type: "MusicArtist",
            IsFolder: !0
        }, bottomText = nowPlayingItem.ArtistItems.map(function(a) {
            return a.Name
        }).join(", ")) : nowPlayingItem.Artists && nowPlayingItem.Artists.length ? bottomText = nowPlayingItem.Artists.join(", ") : nowPlayingItem.SeriesName || nowPlayingItem.Album ? (bottomText = topText, topText = nowPlayingItem.SeriesName || nowPlayingItem.Album, bottomItem = topItem, topItem = nowPlayingItem.SeriesId ? {
            Id: nowPlayingItem.SeriesId,
            Name: nowPlayingItem.SeriesName,
            Type: "Series",
            IsFolder: !0
        } : null) : nowPlayingItem.ProductionYear && !1 !== includeNonNameInfo && (bottomText = nowPlayingItem.ProductionYear);
        var list = [];
        return list.push({
            text: topText,
            item: topItem
        }), bottomText && list.push({
            text: bottomText,
            item: bottomItem
        }), list
    }
    return {
        getNowPlayingNames: getNowPlayingNames
    }
});