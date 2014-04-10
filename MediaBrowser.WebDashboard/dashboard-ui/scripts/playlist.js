(function ($, document) {

    function reloadPlaylist(page) {

        var html = '';

        html += '<table class="detailTable">';

        html += '<thead><tr>';
        html += '<th></th>';
        html += '<th>Name</th>';
        html += '<th>Album</th>';
        html += '<th>Artist</th>';
        html += '<th>Album Artist</th>';
        html += '<th>Time</th>';
        html += '</tr></thead>';

        html += '<tbody>';

        $.each(MediaPlayer.playlist, function (i, item) {

            var name = LibraryBrowser.getPosterViewDisplayName(item);

            var parentName = item.SeriesName || item.Album;

            html += '<tr>';
            html += '<td><button type="button" data-index="' + i + '" class="lnkPlay" data-icon="play" data-iconpos="notext">Play</button></td>';
            html += '<td>';
            html += '<a href="itemdetails.html?id=' + item.Id + '">' + name + '</a>';
            html += '</td>';

            html += '<td>';
            if (parentName) {
                var parentId = item.AlbumId || item.SeriesId || item.ParentId;
                html += '<a href="itemdetails.html?id=' + parentId + '">' + parentName + '</a>';
            }
            html += '</td>';

            html += '<td>';
            html += LibraryBrowser.getArtistLinksHtml(item.Artists || []);
            html += '</td>';

            html += '<td>';
            if (item.AlbumArtist) {
                html += LibraryBrowser.getArtistLinksHtml([item.AlbumArtist]);
            }
            html += '</td>';

            html += '<td>' + Dashboard.getDisplayTime(item.RunTimeTicks) + '</td>';
            html += '<td><button type="button" data-index="' + i + '" class="lnkRemove" data-icon="delete" data-iconpos="notext">Remove</button></td>';
            html += '</tr>';
        });

        html += '</tbody>';
        html += '</table>';

        $("#playlist", page).html(html).trigger('create');
    }

    $(document).on('pageinit', "#playlistPage", function () {

        var page = this;

        $(page).on('click', '.lnkPlay', function () {

            var index = parseInt(this.getAttribute('data-index'));

            MediaController.currentPlaylistIndex(index);
            reloadPlaylist(page);

        }).on('click', '.lnkRemove', function () {

            var index = parseInt(this.getAttribute('data-index'));

            MediaController.removeFromPlaylist(index);
            reloadPlaylist(page);
        });

    }).on('pagebeforeshow', "#playlistPage", function () {

        var page = this;

        reloadPlaylist(page);
    });


})(jQuery, document);