var LibraryBrowser = {
    
    getPosterViewHtml: function (options) {

        var items = options.items;

        var primaryImageAspectRatio = options.useAverageAspectRatio ? LibraryBrowser.getAveragePrimaryImageAspectRatio(items) : null;

        var html = "";

        for (var i = 0, length = items.length; i < length; i++) {
            var item = items[i];

            var hasPrimaryImage = item.ImageTags && item.ImageTags.Primary;

            var href = item.url || (item.IsFolder ? (item.Id ? "itemList.html?parentId=" + item.Id : "#") : "itemdetails.html?id=" + item.Id);

            var showText = options.showTitle || !hasPrimaryImage || (item.Type !== 'Movie' && item.Type !== 'Series' && item.Type !== 'Season' && item.Type !== 'Trailer');

            var cssClass = showText ? "posterViewItem" : "posterViewItem posterViewItemWithNoText";

            html += "<div class='" + cssClass + "'><a href='" + href + "'>";

            if (options.preferBackdrop && item.BackdropImageTags && item.BackdropImageTags.length) {
                html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    height: 198,
                    width: 352,
                    tag: item.BackdropImageTags[0]
                }) + "' />";
            } else if (hasPrimaryImage) {

                var height = 300;
                var width = primaryImageAspectRatio ? parseInt(height * primaryImageAspectRatio) : null;

                html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                    type: "Primary",
                    height: height,
                    width: width,
                    tag: item.ImageTags.Primary
                }) + "' />";

            } else if (item.BackdropImageTags && item.BackdropImageTags.length) {
                html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    height: 198,
                    width: 352,
                    tag: item.BackdropImageTags[0]
                }) + "' />";
            }
            else if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist") {

                html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/audio.png' />";
            }
            else if (item.MediaType == "Video") {

                html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/video.png' />";
            }
            else {

                html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/collection.png' />";
            }

            if (showText) {
                html += "<div class='posterViewItemText'>";
                html += item.Name;
                html += "</div>";
            }

            html += "</a></div>";
        }

        return html;
    },

    getEpisodePosterViewHtml: function (options) {

        var items = options.items;

        var primaryImageAspectRatio = options.useAverageAspectRatio ? LibraryBrowser.getAveragePrimaryImageAspectRatio(items) : null;

        var html = "";

        for (var i = 0, length = items.length; i < length; i++) {
            var item = items[i];

            var hasPrimaryImage = item.ImageTags && item.ImageTags.Primary;

            var href = item.url || (item.IsFolder ? (item.Id ? "itemList.html?parentId=" + item.Id : "#") : "itemdetails.html?id=" + item.Id);

            var showText = options.showTitle || !hasPrimaryImage || (item.Type !== 'Movie' && item.Type !== 'Series' && item.Type !== 'Season' && item.Type !== 'Trailer');

            var cssClass = showText ? "posterViewItem posterViewItemWithDualText" : "posterViewItem posterViewItemWithNoText";

            html += "<div class='" + cssClass + "'><a href='" + href + "'>";

            if (options.preferBackdrop && item.BackdropImageTags && item.BackdropImageTags.length) {
                html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    height: 198,
                    width: 352,
                    tag: item.BackdropImageTags[0]
                }) + "' />";
            } else if (hasPrimaryImage) {

                var height = 300;
                var width = primaryImageAspectRatio ? parseInt(height * primaryImageAspectRatio) : null;

                html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                    type: "Primary",
                    height: height,
                    width: width,
                    tag: item.ImageTags.Primary
                }) + "' />";

            } else if (item.BackdropImageTags && item.BackdropImageTags.length) {
                html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    height: 198,
                    width: 352,
                    tag: item.BackdropImageTags[0]
                }) + "' />";
            } else {
                html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/collection.png' />";
            }

            if (showText) {
                html += "<div class='posterViewItemText posterViewItemPrimaryText'>";
                if (item.SeriesName != null) {
                    html += item.SeriesName;
                    html += "</div>";
                    html += "<div class='posterViewItemText'>";
                }
                if (item.ParentIndexNumber != null) {
                    html += item.ParentIndexNumber + ".";
                }
                if (item.IndexNumber != null) {
                    html += item.IndexNumber + " -";
                }

                html += " " + item.Name;
                html += "</div>";
            }

            html += "</a></div>";
        }

        return html;
    },

	getBoxsetPosterViewHtml: function (options) {

		var items = options.items;

		var primaryImageAspectRatio = options.useAverageAspectRatio ? LibraryBrowser.getAveragePrimaryImageAspectRatio(items) : null;

		var html = "";

		for (var i = 0, length = items.length; i < length; i++) {
			var item = items[i];

			var hasPrimaryImage = item.ImageTags && item.ImageTags.Primary;

			var href = item.url || ("boxset.html?id=" + item.Id);

			var showText = options.showTitle || !hasPrimaryImage || (item.Type !== 'Movie' && item.Type !== 'Series' && item.Type !== 'Season' && item.Type !== 'Trailer');

			var cssClass = showText ? "posterViewItem posterViewItemWithDualText" : "posterViewItem posterViewItemWithNoText";

			html += "<div class='" + cssClass + "'><a href='" + href + "'>";

			if (options.preferBackdrop && item.BackdropImageTags && item.BackdropImageTags.length) {
				html += "<img src='" + ApiClient.getImageUrl(item.Id, {
					type: "Backdrop",
					height: 198,
					width: 352,
					tag: item.BackdropImageTags[0]
				}) + "' />";
			} else if (hasPrimaryImage) {

				var height = 300;
				var width = primaryImageAspectRatio ? parseInt(height * primaryImageAspectRatio) : null;

				html += "<img src='" + ApiClient.getImageUrl(item.Id, {
					type: "Primary",
					height: height,
					width: width,
					tag: item.ImageTags.Primary
				}) + "' />";

			} else if (item.BackdropImageTags && item.BackdropImageTags.length) {
				html += "<img src='" + ApiClient.getImageUrl(item.Id, {
					type: "Backdrop",
					height: 198,
					width: 352,
					tag: item.BackdropImageTags[0]
				}) + "' />";
			} else {
				html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/video.png' />";
			}

			if (showText) {
				html += "<div class='posterViewItemText posterViewItemPrimaryText'>";
				html += item.Name;
				html += "</div>";
				html += "<div class='posterViewItemText'>";
				html += item.ChildCount+" Movie";
				if (item.ChildCount > 1) html += "s";
				html += "</div>";
			}

			html += "</a></div>";
		}

		return html;
	},

    getAveragePrimaryImageAspectRatio: function (items) {

        var values = [];

        for (var i = 0, length = items.length; i < length; i++) {

            var ratio = items[i].PrimaryImageAspectRatio || 0;

            if (!ratio) {
                continue;
            }

            values[values.length] = ratio;
        }

        if (!values.length) {
            return null;
        }

        // Use the median
        values.sort(function (a, b) { return a - b; });

        var half = Math.floor(values.length / 2);

        if (values.length % 2)
            return values[half];
        else
            return (values[half - 1] + values[half]) / 2.0;
    },

    metroColors: ["#6FBD45", "#4BB3DD", "#4164A5", "#E12026", "#800080", "#E1B222", "#008040", "#0094FF", "#FF00C7", "#FF870F", "#7F0037"],

    getRandomMetroColor: function () {

        var index = Math.floor(Math.random() * (LibraryBrowser.metroColors.length - 1));

        return LibraryBrowser.metroColors[index];
    },

    getMetroColor: function (str) {

        if (str) {
            var char = String(str.substr(0, 1).charCodeAt());
	        var sum = 0;
	        for (var i = 0; i < char.length; i++) {
		        sum += parseInt(char.charAt(i));
	        }
            var index = String(sum).substr(-1);

            return LibraryBrowser.metroColors[index];
        } else {
            return LibraryBrowser.getRandomMetroColor();
        }

    },

	renderLinks: function (item) {
		var page = $.mobile.activePage;
//console.log(item);
		if (item.ProviderIds) {
			var elem = $('#itemLink', page).show();

			var html = 'Links:&nbsp;&nbsp;';
			var links = [];

			if (item.ProviderIds.Imdb) {
				if (item.Type == "Movie" || item.Type == "Episode")
					links.push('<a class="ui-link" href="http://www.imdb.com/title/' + item.ProviderIds.Imdb + '" target="_blank">IMDB</a>');
				else if (item.Type == "Person")
					links.push('<a class="ui-link" href="http://www.imdb.com/name/' + item.ProviderIds.Imdb + '" target="_blank">IMDB</a>');
			}
			if (item.ProviderIds.Tmdb) {
				if (item.Type == "Movie")
					links.push('<a class="ui-link" href="http://www.themoviedb.org/movie/' + item.ProviderIds.Tmdb + '" target="_blank">TMDB</a>');
				else if (item.Type == "Person")
					links.push('<a class="ui-link" href="http://www.themoviedb.org/person/' + item.ProviderIds.Tmdb + '" target="_blank">TMDB</a>');
			}
			if (item.ProviderIds.Tvdb)
				links.push('<a class="ui-link" href="http://thetvdb.com/index.php?tab=series&id=' + item.ProviderIds.Tvdb + '" target="_blank">TVDB</a>');
			if (item.ProviderIds.Tvcom)
				links.push('<a class="ui-link" href="http://www.tv.com/shows/' + item.ProviderIds.Tvcom + '" target="_blank">TV.com</a>');
			if (item.ProviderIds.Musicbrainz)
				links.push('<a class="ui-link" href="http://musicbrainz.org/release/' + item.ProviderIds.Musicbrainz + '" target="_blank">MusicBrainz</a>');
			if (item.ProviderIds.Gamesdb)
				links.push('<a class="ui-link" href="http://www.games-db.com/Game/' + item.ProviderIds.Gamesdb + '" target="_blank">GamesDB</a>');

			html += links.join('&nbsp;&nbsp;/&nbsp;&nbsp;');

			$('#itemLinks', page).html(html);

		} else {
			$('#itemLinks', page).hide();
		}
	}
};