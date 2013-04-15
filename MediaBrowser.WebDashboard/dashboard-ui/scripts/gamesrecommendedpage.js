(function ($, document) {

	$(document).on('pagebeforeshow', "#gamesRecommendedPage", function () {

		var page = this;

		var options = {

			SortBy: "DateCreated",
			SortOrder: "Descending",
			MediaTypes: "Game",
			Limit: 5,
			Recursive: true,
			Fields: "PrimaryImageAspectRatio",
			Filters: "IsUnplayed"
		};

		ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

			$('#recentlyAddedItems', page).html(LibraryBrowser.getGamePosterViewHtml({
				items: result.Items,
				useAverageAspectRatio: true,
				showNewIndicator: false
			}));

		});

		var options = {

			SortBy: "DatePlayed",
			SortOrder: "Descending",
			MediaTypes: "Game",
			Limit: 5,
			Recursive: true,
			Fields: "PrimaryImageAspectRatio",
			Filters: "IsPlayed"
		};

		ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

			$('#resumableItems', page).html(LibraryBrowser.getGamePosterViewHtml({
				items: result.Items,
				useAverageAspectRatio: true,
				showNewIndicator: false
			}));

		});

	});

})(jQuery, document);