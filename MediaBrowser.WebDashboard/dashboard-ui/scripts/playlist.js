(function (window) {

    function playlist() {
        var self = this;





        return self;
    }


    window.Playlist = new playlist();
})(window);

(function ($, document) {

	$(document).on('pagebeforeshow', "#playlistPage", function () {

		var page = this;

		Dashboard.showLoadingMsg();

		$("#queueTable").html('');

		//currently playing item
		if (MediaPlayer.playing) {
			var html = '';
			html += '<tr>';
			html += '<td></td>';
			html += '<td>' + MediaPlayer.playing.Name + '</td>';
			html += '<td>' + MediaPlayer.playing.Album + '</td>';
			html += '<td>' + ticks_to_human(MediaPlayer.playing.RunTimeTicks) + '</td>';
			html += '<td>' + LibraryBrowser.getUserDataIconsHtml(MediaPlayer.playing) + '</td>';
			html += '<td></td>';
			html += '</tr>';
			$("#queueTable").append(html);
		}

		$.each(MediaPlayer.queue, function(i, item){
			var html = '';
			var name = item.Name;

			if (item.IndexNumber != null) {
				name = item.IndexNumber + " - " + name;
			}
			if (item.ParentIndexNumber != null) {
				name = item.ParentIndexNumber + "." + name;
			}

			//$('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

			if (item.SeriesName || item.Album) {
				var seriesName = item.SeriesName || item.Album;
			}else {
				var seriesName = item.ProductionYear;
			}

			html += '<tr>';
			html += '<td><img src="css/images/media/playCircle.png" style="height: 28px;cursor:pointer;" data-queue-index="'+i+'" onclick="MediaPlayer.queuePlay(this)" /></td>';
			html += '<td>' + name + '</td>';
			html += '<td>' + seriesName + '</td>';
			html += '<td>' + ticks_to_human(item.RunTimeTicks) + '</td>';
			html += '<td>' + LibraryBrowser.getUserDataIconsHtml(item) + '</td>';
			html += '<td><a href="" data-queue-index="'+i+'" onclick="MediaPlayer.queueRemove(this)">remove</a></td>';
			html += '</tr>';

			$("#queueTable").append(html);
		});


		Dashboard.hideLoadingMsg();
	});


})(jQuery, document);