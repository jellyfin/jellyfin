
(function ($, document) {

	// The base query options
	var query = {

		SortBy: "SortName",
		SortOrder: "Ascending",
		IncludeItemTypes: "GamePlatform",
		Recursive: true,
		Fields: "PrimaryImageAspectRatio,ItemCounts,ItemCounts,DateCreated,UserData",
		Limit: LibraryBrowser.getDetaultPageSize(),
		StartIndex: 0
	};


	function reloadItems(page) {

		Dashboard.showLoadingMsg();

		ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

			var html = '';

			$('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

			html += LibraryBrowser.getPosterDetailViewHtml({
				items: result.Items,
				useAverageAspectRatio: true
			});

			html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

			$('#items', page).html(html).trigger('create');

			$('.selectPage', page).on('change', function () {
			    query.StartIndex = (parseInt(this.value) - 1) * query.Limit;
				reloadItems(page);
			});

			$('.btnNextPage', page).on('click', function () {
			    query.StartIndex += query.Limit;
			    reloadItems(page);
			});

			$('.btnPreviousPage', page).on('click', function () {
			    query.StartIndex -= query.Limit;
			    reloadItems(page);
			});

			Dashboard.hideLoadingMsg();
		});
	}

	$(document).on('pageinit', "#gamesystemsPage", function () {

		var page = this;

		$('.radioSortBy', this).on('click', function () {
			query.SortBy = this.getAttribute('data-sortby');
			reloadItems(page);
		});

		$('.radioSortOrder', this).on('click', function () {
			query.SortOrder = this.getAttribute('data-sortorder');
			reloadItems(page);
		});

		$('.chkStandardFilter', this).on('change', function () {

			var filterName = this.getAttribute('data-filter');
			var filters = query.Filters || "";

			filters = (',' + filters).replace(',' + filterName, '').substring(1);

			if (this.checked) {
				filters = filters ? (filters + ',' + filterName) : filterName;
			}

			query.StartIndex = 0;
			query.Filters = filters;

			reloadItems(page);
		});

	}).on('pagebeforeshow', "#gamesystemsPage", function () {

			reloadItems(this);

		}).on('pageshow', "#gamesystemsPage", function () {

			// Reset form values using the last used query
			$('.radioSortBy', this).each(function () {

				this.checked = query.SortBy == this.getAttribute('data-sortby');

			}).checkboxradio('refresh');

			$('.radioSortOrder', this).each(function () {

				this.checked = query.SortOrder == this.getAttribute('data-sortorder');

			}).checkboxradio('refresh');

			$('.chkStandardFilter', this).each(function () {

				var filters = "," + (query.Filters || "");
				var filterName = this.getAttribute('data-filter');

				this.checked = filters.indexOf(',' + filterName) != -1;

			}).checkboxradio('refresh');
		});

})(jQuery, document);
