
(function ($, document) {

    var view = "Backdrop";

	// The base query options
	var query = {

		SortBy: "SortName",
		SortOrder: "Ascending",
		MediaTypes: "Game",
		Recursive: true,
		Fields: "PrimaryImageAspectRatio,UserData,DisplayMediaType,Genres,Studios",
		Limit: LibraryBrowser.getDetaultPageSize(),
		StartIndex: 0
	};

	function reloadItems(page) {

		Dashboard.showLoadingMsg();

		ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

			var html = '';

			$('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

			for (var i = 0, length = result.Items.length; i < length; i++) {
				var item = result.Items[i];
//console.log(item);
				html += '<tr>';
				html += '<td><a href="gamedetail.html?id='+item.Id+'">' + item.Name + '</a></td>';
				html += '<td>' + item.DisplayMediaType + '</td>';
				html += '<td>' + item.ReleaseYear + '</td>';
				html += '<td>' + /*LibraryBrowser.renderGenres('', item)*/ + '</td>';
				html += '<td>' + /*LibraryBrowser.renderStudios('', item)*/ + '</td>';
				html += '<td>' + /* */ + '</td>';
				html += '</tr>';

			}

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

	$(document).on('pageinit', "#gamesPage", function () {

		var page = this;

		$('.radioSortBy', this).on('click', function () {
			query.StartIndex = 0;
			query.SortBy = this.getAttribute('data-sortby');
			reloadItems(page);
		});

		$('.radioSortOrder', this).on('click', function () {
			query.StartIndex = 0;
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

		$('#selectView', this).on('change', function () {

			view = this.value;

			reloadItems(page);
		});


	}).on('pagebeforeshow', "#gamesPage", function () {

			reloadItems(this);

		}).on('pageshow', "#gamesPage", function () {


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


			$('#selectView', this).val(view).selectmenu('refresh');

		});

})(jQuery, document);