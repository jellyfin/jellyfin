
(function ($, document, LibraryBrowser) {

	function reload(page) {

		var id = getParameterByName('id');

		Dashboard.showLoadingMsg();

		ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

			var name = item.Name;

			$('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

			Dashboard.setPageTitle(name);

			$('#itemName', page).html(name);

			renderDetails(page, item);

			Dashboard.hideLoadingMsg();
		});
	}

	function renderDetails(page, item) {

	    LibraryBrowser.renderOverview($('#itemOverview', page), item);

		if (item.CommunityRating) {
			$('#itemCommunityRating', page).html(LibraryBrowser.getStarRatingHtml(item)).show().attr('title', item.CommunityRating);
		} else {
			$('#itemCommunityRating', page).hide();
		}

		$('#itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

		LibraryBrowser.renderGenres($('#itemGenres', page), item);
		LibraryBrowser.renderStudios($('#itemStudios', page), item);
		renderUserDataIcons(page, item);
		LibraryBrowser.renderLinks($('#itemLinks', page), item);
	}

	function renderUserDataIcons(page, item) {
		$('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
	}

	$(document).on('pageshow', "#gamesystemPage", function () {
		reload(this);
	});


})(jQuery, document, LibraryBrowser);