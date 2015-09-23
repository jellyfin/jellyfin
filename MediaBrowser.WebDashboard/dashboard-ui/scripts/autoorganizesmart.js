(function ($, document, window) {

    var query = {

        StartIndex: 0,
        Limit: 100000
    };

    var currentResult;

    function reloadList(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getSmartMatchInfos(query).done(function (infos) {

            currentResult = infos;

            populateList(page, infos);

            Dashboard.hideLoadingMsg();
        });
    }

    function populateList(page, result) {

        var infos = result.Items;

        if (infos.length > 0) {
            infos = infos.sort(function (a, b) {

                a = a.OrganizerType + " " + a.Name;
                b = b.OrganizerType + " " + b.Name;

                if (a == b) {
                    return 0;
                }

                if (a < b) {
                    return -1;
                }

                return 1;
            });
        }

        var html = "";
        var currentType;

        for (var i = 0, length = infos.length; i < length; i++) {

            var info = infos[i];

            if (info.OrganizerType != currentType) {
                currentType = info.OrganizerType;

                if (html.length > 0)
                {
                    html += "</ul>";
                }

                html += "<h2>" + currentType + "</h2>";

                html += '<ul data-role="listview" data-inset="true" data-auto-enhanced="false" data-split-icon="action">';
            }

            html += "<li data-role='list-divider'><h3 style='font-weight:bold'>" + info.Name + "</h3></li>";

            for (var n = 0; n < info.MatchStrings.length; n++) {
                html += "<li title='" + info.MatchStrings[n] + "'>";

                html += "<a style='padding-top: 0.5em; padding-bottom: 0.5em'>";

                html += "<p>" + info.MatchStrings[n] + "</p>";

                html += "<a id='btnDeleteMatchEntry" + info.Id + "' class='btnDeleteMatchEntry' href='#' data-id='" + info.Id + "' data-matchstring='" + info.MatchStrings[n]  + "' data-icon='delete'>" + Globalize.translate('ButtonDelete') + "</a>";

                html += "</a>";

                html += "</li>";
            }
        }

        html += "</ul>";

        $('.divMatchInfos', page).html(html).trigger('create');
    }


    $(document).on('pageinit', "#libraryFileOrganizerSmartMatchPage", function () {

        var page = this;

        $('.divMatchInfos', page).on('click', '.btnDeleteMatchEntry', function () {

            var button = this;
            var id = button.getAttribute('data-id');

            var options = {

                MatchString: button.getAttribute('data-matchstring')
            };

            ApiClient.deleteSmartMatchEntry(id, options).done(function () {

                reloadList(page);

            });

        });

    }).on('pageshowready', "#libraryFileOrganizerSmartMatchPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        reloadList(page);

    }).on('pagebeforehide', "#libraryFileOrganizerSmartMatchPage", function () {

        var page = this;
        currentResult = null;
    });

})(jQuery, document, window);