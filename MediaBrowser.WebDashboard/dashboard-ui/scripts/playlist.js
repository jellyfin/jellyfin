(function ($, document) {

    function reloadPlaylist(page) {
        
        var html = '';

        html += '<table class="detailTable">';

        html += '<thead><tr>';
        html += '<th></th>';
        html += '<th>Name</th>';
        html += '<th>Album</th>';
        html += '<th>Time</th>';
        html += '<th>Rating</th>';
        html += '</tr></thead>';

        html += '<tbody>';

        $.each(MediaPlayer.playlist, function (i, item) {

            var name = LibraryBrowser.getPosterViewDisplayName(item);

            var parentName = item.SeriesName || item.Album || item.ProductionYear || '';

            html += '<tr>';
            html += '<td><a href="#" data-index="' + i + '" class="lnkPlay"><img src="css/images/media/playcircle.png" style="height: 20px;" /></a></td>';
            html += '<td>' + name + '</td>';
            html += '<td>' + parentName + '</td>';
            html += '<td>' + Dashboard.getDisplayTime(item.RunTimeTicks) + '</td>';
            html += '<td>' + LibraryBrowser.getUserDataIconsHtml(item) + '</td>';
            html += '<td><a href="#" data-index="' + i + '" class="lnkRemove"><img src="css/images/media/remove.png" style="height: 20px;" /></a></td>';
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

            MediaPlayer.currentPlaylistIndex(index);
            reloadPlaylist(page);

        }).on('click', '.lnkRemove', function () {

            var index = parseInt(this.getAttribute('data-index'));

            MediaPlayer.removeFromPlaylist(index);
            reloadPlaylist(page);
        });

    }).on('pagebeforeshow', "#playlistPage", function () {

        var page = this;

        reloadPlaylist(page);
    });


})(jQuery, document);