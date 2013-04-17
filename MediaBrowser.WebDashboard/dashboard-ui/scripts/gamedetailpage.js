
(function ($, document, LibraryBrowser, window) {

	var currentItem;

	function reload(page) {

		var id = getParameterByName('id');

		Dashboard.showLoadingMsg();

		ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

			currentItem = item;

			var name = item.Name;

			if (item.IndexNumber != null) {
				name = item.IndexNumber + " - " + name;
			}
			if (item.ParentIndexNumber != null) {
				name = item.ParentIndexNumber + "." + name;
			}

			$('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

			Dashboard.setPageTitle(name);

			$('#itemName', page).html(name);

			setInitialCollapsibleState(page, item);
			renderDetails(page, item);

			if (LibraryBrowser.shouldDisplayGallery(item)) {
			    $('#galleryCollapsible', page).show();
			} else {
			    $('#galleryCollapsible', page).hide();
			}

			Dashboard.hideLoadingMsg();
		});
	}

	function setInitialCollapsibleState(page, item) {

		if (!item.LocalTrailerCount || item.LocalTrailerCount == 0) {
			$('#trailersCollapsible', page).hide();
		} else {
			$('#trailersCollapsible', page).show();
		}
	}

	function renderDetails(page, item) {

		if (item.Taglines && item.Taglines.length) {
			$('#itemTagline', page).html(item.Taglines[0]).show();
		} else {
			$('#itemTagline', page).hide();
		}

		if (item.Overview || item.OverviewHtml) {
			var overview = item.OverviewHtml || item.Overview;

			$('#itemOverview', page).html(overview).show();
			$('#itemOverview a').each(function () {
				$(this).attr("target", "_blank");
			});
		} else {
			$('#itemOverview', page).hide();
		}

		if (item.CommunityRating) {
			$('#itemCommunityRating', page).html(LibraryBrowser.getStarRatingHtml(item)).show().attr('title', item.CommunityRating);
		} else {
			$('#itemCommunityRating', page).hide();
		}

		LibraryBrowser.renderBudget($('#itemBudget', page), item);

		$('#itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

		LibraryBrowser.renderGenres($('#itemGenres', page), item);
		LibraryBrowser.renderStudios($('#itemStudios', page), item);
		renderUserDataIcons(page, item);
		LibraryBrowser.renderLinks($('#itemLinks', page), item);
	}

	function renderUserDataIcons(page, item) {
		$('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
	}

	function renderGallery(page, item) {

	    var html = LibraryBrowser.getGalleryHtml(item);

		$('#galleryContent', page).html(html).trigger('create');
	}

	function renderTrailers(page, item) {
		var html = '';

		ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), item.Id).done(function (trailers) {

			for (var i = 0, length = trailers.length; i < length; i++) {

				var trailer = trailers[i];

				html += '<div class="posterViewItem posterViewItemWithDualText">';
				html += '<a href="#play-Trailer-' + i + '" onclick="ItemDetailPage.playTrailer(' + i + ');">';

				var imageTags = trailer.ImageTags || {};

				if (imageTags.Primary) {

					var imgUrl = ApiClient.getImageUrl(trailer.Id, {
						maxwidth: 500,
						tag: imageTags.Primary,
						type: "primary"
					});

					html += '<img src="' + imgUrl + '" />';
				} else {
					html += '<img src="css/images/items/detail/video.png"/>';
				}

				html += '<div class="posterViewItemText posterViewItemPrimaryText">' + trailer.Name + '</div>';
				html += '<div class="posterViewItemText">';

				if (trailer.RunTimeTicks != "") {
					html += ticks_to_human(trailer.RunTimeTicks);
				}
				else {
					html += "&nbsp;";
				}
				html += '</div>';

				html += '</a>';

				html += '</div>';
			}

			$('#trailersContent', page).html(html);

		});
	}

	$(document).on('pageinit', "#gameDetailPage", function () {

		var page = this;

	}).on('pageshow', "#gameDetailPage", function () {

			var page = this;

			reload(page);

			$('#trailersCollapsible', page).on('expand.lazyload', function () {
				renderTrailers(page, currentItem);

				$(this).off('expand.lazyload');
			});

			$('#galleryCollapsible', page).on('expand.lazyload', function () {

				renderGallery(page, currentItem);

				$(this).off('expand.lazyload');
			});

		}).on('pagehide', "#gameDetailPage", function () {

			currentItem = null;
			var page = this;

			$('#trailersCollapsible', page).off('expand.lazyload');
			$('#galleryCollapsible', page).off('expand.lazyload');
		});

	function gameDetailPage() {

		var self = this;

		self.playTrailer = function (index) {
			ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), currentItem.Id).done(function (trailers) {
				MediaPlayer.play([trailers[index]]);
			});
		};
	}

	window.GameDetailPage = new gameDetailPage();


})(jQuery, document, LibraryBrowser, window);