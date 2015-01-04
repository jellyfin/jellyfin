(function () {

    function cancelJob(page, id) {

        $('.jobMenu', page).on("popupafterclose.deleteuser", function () {

            $(this).off('popupafterclose.deleteuser');

            var msg = Globalize.translate('CancelSyncJobConfirmation');

            Dashboard.confirm(msg, Globalize.translate('HeaderCancelSyncJob'), function (result) {

                if (result) {
                    Dashboard.showLoadingMsg();

                    ApiClient.ajax({

                        url: ApiClient.getUrl('Sync/Jobs/' + id),
                        type: 'DELETE'

                    }).done(function () {

                        reloadData(page);
                    });
                }
            });

        }).popup('close');
    }

    function getSyncTargetName(targets, id) {

        var target = targets.filter(function (t) {

            return t.Id == id;
        })[0];

        return target ? target.Name : 'Unknown Device';
    }

    function getSyncJobHtml(page, job, cardBoxCssClass, syncJobPage) {

        var html = '';

        html += "<div class='card squareCard' data-id='" + job.Id + "'>";

        html += '<div class="' + cardBoxCssClass + '">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        syncJobPage += '?id=' + job.Id;

        html += '<a class="cardContent" href="' + syncJobPage + '">';

        var imgUrl;
        var style = '';

        if (job.PrimaryImageItemId) {
            imgUrl = ApiClient.getScaledImageUrl(job.PrimaryImageItemId, {
                type: "Primary",
                width: 400,
                tag: job.PrimaryImageTag
            });
            style = "background-position:center center;";
        } else {
            style = "background-color:#38c;background-position:center center;";
            imgUrl = "css/images/items/detail/video.png";
        }

        html += '<div class="cardImage coveredCardImage lazy" data-src="' + imgUrl + '" style="' + style + '">';

        if (job.Progress && job.Progress < 100) {
            html += '<div class="cardFooter">';
            html += "<div class='cardText cardProgress'>";
            html += '<progress class="itemProgressBar" min="0" max="100" value="' + job.Progress + '"></progress>';
            html += "</div>";
            html += "</div>";
        }

        html += "</div>";

        if (job.Status == 'Completed') {
            html += '<div class="playedIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
        }
        else if (job.Status == 'CompletedWithError') {
            html += '<div class="playedIndicator" style="background-color:#cc0000;"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
        }

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        var textLines = [];

        if (job.ParentName) {
            textLines.push(job.ParentName);
        }

        textLines.push(job.Name);

        if (job.ItemCount == 1) {
            textLines.push(job.ItemCount + ' item');
        } else {
            textLines.push(job.ItemCount + ' items');
        }

        if (!job.ParentName) {
            textLines.push('&nbsp;');
        }

        html += '<div class="cardText" style="text-align:right; position:absolute; bottom:5px; right: 5px;">';
        html += '<button class="btnJobMenu" type="button" data-inline="true" data-iconpos="notext" data-icon="ellipsis-v" style="margin: 0 0 0;"></button>';
        html += "</div>";

        for (var i = 0, length = textLines.length; i < length; i++) {
            html += "<div class='cardText' style='margin-right:30px;'>";
            html += textLines[i];
            html += "</div>";
        }

        // cardFooter
        html += "</div>";

        // cardBox
        html += "</div>";

        // card
        html += "</div>";

        return html;
    }

    function loadData(page, jobs, targets) {

        var html = '';
        var lastTargetName = '';

        var cardBoxCssClass = 'cardBox visualCardBox';
        var barCssClass = 'ui-bar-a';

        if ($(page).hasClass('libraryPage')) {
            cardBoxCssClass += ' visualCardBox-b';
            barCssClass = 'detailSectionHeader';
        }

        var syncJobPage = 'syncjob.html';

        if ($(page).hasClass('mySyncPage')) {
            syncJobPage = 'mysyncjob.html';
        }

        for (var i = 0, length = jobs.length; i < length; i++) {

            var job = jobs[i];
            var targetName = getSyncTargetName(targets, job.TargetId);

            if (targetName != lastTargetName) {

                if (lastTargetName) {
                    html += '<br/>';
                    html += '<br/>';
                    html += '<br/>';
                }

                lastTargetName = targetName;

                html += '<div class="' + barCssClass + '" style="padding: 0 1em;"><p>' + targetName + '</p></div>';
            }

            html += getSyncJobHtml(page, job, cardBoxCssClass, syncJobPage);
        }

        var elem = $('.syncActivity', page).html(html).trigger('create');

        $('.btnJobMenu', elem).on('click', function () {
            showJobMenu(this);
        });

        if (!jobs.length) {

            elem.html('<div style="padding:1em .25em;">' + Globalize.translate('MessageNoSyncJobsFound') + '</div>');
        }
    }

    function showJobMenu(elem) {

        var card = $(elem).parents('.card');
        var page = $(elem).parents('.page');
        var id = card.attr('data-id');

        $('.jobMenu', page).popup("close").remove();

        var html = '<div data-role="popup" class="jobMenu tapHoldMenu" data-theme="a">';

        html += '<ul data-role="listview" style="min-width: 180px;">';
        html += '<li data-role="list-divider">' + Globalize.translate('HeaderMenu') + '</li>';

        html += '<li data-icon="delete"><a href="#" class="btnCancelJob" data-id="' + id + '">' + Globalize.translate('ButtonCancel') + '</a></li>';

        html += '</ul>';

        html += '</div>';

        page.append(html);

        var flyout = $('.jobMenu', page).popup({ positionTo: elem || "window" }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        });

        $('.btnCancelJob', flyout).on('click', function () {
            cancelJob(page, this.getAttribute('data-id'));
        });
    }

    function reloadData(page) {

        Dashboard.showLoadingMsg();

        var options = {};

        Dashboard.getCurrentUser().done(function (user) {

            if ($(page).hasClass('mySyncPage')) {
                options.UserId = Dashboard.getCurrentUserId();
            }

            var promise1 = ApiClient.getJSON(ApiClient.getUrl('Sync/Jobs', options));

            var promise2 = ApiClient.getJSON(ApiClient.getUrl('Sync/Targets', options));

            $.when(promise1, promise2).done(function (response1, response2) {

                loadData(page, response1[0].Items, response2[0]);

                Dashboard.hideLoadingMsg();

            });
        });
    }

    $(document).on('pageshow', ".syncActivityPage", function () {

        var page = this;

        reloadData(page);

    });

})();