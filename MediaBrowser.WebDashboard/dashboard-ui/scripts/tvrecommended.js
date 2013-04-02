(function ($, document) {

	$(document).on('pageshow', "#tvRecommendedPage", function () {

		var page = this;

		var options = {

			SortBy: "DateCreated",
			SortOrder: "Descending",
			IncludeItemTypes: "Episode",
			Limit: 6,
			Recursive: true,
			Fields: "PrimaryImageAspectRatio"
		};

		ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

			$('#recentlyAddedItems', page).html(Dashboard.getPosterViewHtml({
				items: result.Items,
				useAverageAspectRatio: true
			}));

		});


		options = {

			SortBy: "DatePlayed",
			SortOrder: "Descending",
			IncludeItemTypes: "Episode",
			Filters: "IsResumable",
			Limit: 6,
			Recursive: true,
			Fields: "PrimaryImageAspectRatio"
		};

		ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

			if (result.Items.length) {
				$('#resumableSection', page).show();
			} else {
				$('#resumableSection', page).hide();
			}

			$('#resumableItems', page).html(Dashboard.getPosterViewHtml({
				items: result.Items,
				useAverageAspectRatio: true
			}));

		});

	});


})(jQuery, document);