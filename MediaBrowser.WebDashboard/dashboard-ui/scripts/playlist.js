(function (window) {

    function playlist() {
        var self = this;

	    if (typeof(self.queue) == 'undefined') {
            self.queue = [];
        }

        self.add = function (item) {

            self.queue.push(item);

        };

        self.remove = function (index) {

	        self.queue.splice(index, 1);
        };

        self.play = function (elem) {
			var index = $(elem).attr("data-queue-index");

            MediaPlayer.play(new Array(self.queue[index]));
	        self.queue.shift();
        };

        return self;
    }

    window.Playlist = new playlist();
})(window);

(function ($, document) {

	$(document).on('pagebeforeshow', "#playlistPage", function () {

		var page = this;

		Dashboard.showLoadingMsg();

		$.each(Playlist.queue, function(i, item){
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
			html += '<td><img src="css/images/media/playCircle.png" style="height: 28px;" data-queue-index="'+i+'" onclick="Playlist.play(this)" /></td>';
			html += '<td>' + name + '</td>';
			html += '<td>' + seriesName + '</td>';
			html += '<td>' + ticks_to_human(item.RunTimeTicks) + '</td>';
			html += '<td></td>';
			html += '</tr>';

			$("#queueTable").append(html);
		});


		Dashboard.hideLoadingMsg();
	});


})(jQuery, document);