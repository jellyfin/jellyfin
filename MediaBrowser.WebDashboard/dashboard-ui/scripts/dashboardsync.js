(function () {

    function getSyncTargetName(targets, id) {

        var target = targets.filter(function (t) {

            return t.Id == id;
        })[0];

        return target ? target.Name : 'Unknown Device';
    }

    function getSyncJobHtml(job) {

        var html = '';

        html += "<div class='card squareCard'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        html += '<div class="cardContent">';

        var imgUrl;

        if (job.PrimaryImageItemId) {
            imgUrl = ApiClient.getScaledImageUrl(job.PrimaryImageItemId, {
                type: "Primary",
                width: 400,
                tag: job.PrimaryImageTag
            });
        }

        if (imgUrl) {
            html += '<div class="cardImage coveredCardImage lazy" data-src="' + imgUrl + '">';
            html += "</div>";
        }

        if (job.Status == 'Completed') {
            html += '<div class="playedIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
        }
        else if (job.Status == 'Queued') {
            html += '<div class="playedIndicator" style="background-color:#38c;"><div class="ui-icon-clock ui-btn-icon-notext"></div></div>';
        }
        else if (job.Status == 'Transcoding' || job.Status == 'Transferring') {
            html += '<div class="playedIndicator"><div class="ui-icon-refresh ui-btn-icon-notext"></div></div>';
        }
        else if (job.Status == 'Cancelled') {
            html += '<div class="playedIndicator" style="background-color:#FF6A00;"><div class="ui-icon-minus ui-btn-icon-notext"></div></div>';
        }
        else if (job.Status == 'TranscodingFailed') {
            html += '<div class="playedIndicator" style="background-color:#cc0000;"><div class="ui-icon-delete ui-btn-icon-notext"></div></div>';
        }

        // cardContent
        html += "</div>";

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

        for (var i = 0, length = textLines.length; i < length; i++) {
            html += "<div class='cardText'>";
            html += textLines[i];
            html += "</div>";
        }

        //if (!plugin.isExternal) {
        //    html += "<div class='cardText packageReviewText'>";
        //    html += plugin.price > 0 ? "$" + plugin.price.toFixed(2) : Globalize.translate('LabelFree');
        //    html += RatingHelpers.getStoreRatingHtml(plugin.avgRating, plugin.id, plugin.name);

        //    html += "<span class='storeReviewCount'>";
        //    html += " " + Globalize.translate('LabelNumberReviews').replace("{0}", plugin.totalRatings);
        //    html += "</span>";

        //    html += "</div>";
        //}

        //var installedPlugin = plugin.isApp ? null : installedPlugins.filter(function (ip) {
        //    return ip.Name == plugin.name;
        //})[0];

        //html += "<div class='cardText'>";

        //if (installedPlugin) {
        //    html += Globalize.translate('LabelVersionInstalled').replace("{0}", installedPlugin.Version);
        //} else {
        //    html += '&nbsp;';
        //}
        //html += "</div>";

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

        for (var i = 0, length = jobs.length; i < length; i++) {

            var job = jobs[i];
            var targetName = getSyncTargetName(targets, job.TargetId);

            if (targetName != lastTargetName) {
                html += '<p style="font-size: 24px; border-bottom: 1px solid #ddd; margin: .5em 0; font-weight:300;">' + targetName + '</p>';

                lastTargetName = targetName;
            }

            html += getSyncJobHtml(job);
        }

        $('.syncActivity', page).html(html).trigger('create');
    }

    $(document).on('pageshow', "#dashboardSyncPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getJSON(ApiClient.getUrl('Sync/Jobs'));

        var promise2 = ApiClient.getJSON(ApiClient.getUrl('Sync/Targets'));

        $.when(promise1, promise2).done(function (response1, response2) {

            loadData(page, response1[0].Items, response2[0]);

            Dashboard.hideLoadingMsg();

        });

    }).on('pageinit', "#dashboardSyncPage", function () {

        var page = this;

    });

})();