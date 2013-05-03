(function (window) {

    function playlist() {
        var self = this;

	    if (typeof(self.queue) == 'undefined') {
            self.queue = [];
        }

        self.add = function (item) {

            self.queue.push(item);

        };

        self.remove = function (elem) {
	        var index = $(elem).attr("data-queue-index");

	        self.queue.splice(index, 1);

	        $(elem).parent().parent().remove();
	        return false;
        };

        self.play = function (elem) {
			var index = $(elem).attr("data-queue-index");

            MediaPlayer.play(new Array(self.queue[index]));
	        self.queue.splice(index, 1);
        };

	    self.playNext = function (item) {
		    if (typeof self.queue[0] != "undefined") {
			    MediaPlayer.play(new Array(self.queue[0]));
			    self.queue.shift();
		    }
	    };

	    self.addNext = function (item) {
		    if (typeof self.queue[0] != "undefined") {
			    self.queue.unshift(item);
		    }else {
			    self.add(item);
		    }
	    };

	    self.inQueue = function (item) {
		    $.each(Playlist.queue, function(i, queueItem){
			    if (item.Id == queueItem.Id) {
				    return true;
			    }
		    });
		    return false;
	    };

        return self;
    }


    window.Playlist = new playlist();
})(window);

(function ($, document) {

	$(document).on('pagebeforeshow', "#playlistPage", function () {

		var page = this;

		Dashboard.showLoadingMsg();

		$("#queueTable").html('');
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
			html += '<td><img src="css/images/media/playCircle.png" style="height: 28px;cursor:pointer;" data-queue-index="'+i+'" onclick="Playlist.play(this)" /></td>';
			html += '<td>' + name + '</td>';
			html += '<td>' + seriesName + '</td>';
			html += '<td>' + ticks_to_human(item.RunTimeTicks) + '</td>';
			html += '<td>' + LibraryBrowser.getUserDataIconsHtml(item) + '</td>';
			html += '<td><a href="" data-queue-index="'+i+'" onclick="Playlist.remove(this)">remove</a></td>';
			html += '</tr>';

			$("#queueTable").append(html);
		});


		Dashboard.hideLoadingMsg();
	});


})(jQuery, document);