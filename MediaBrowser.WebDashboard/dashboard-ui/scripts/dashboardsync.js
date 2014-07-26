(function () {

    function getSyncTargetName(targets, id) {

        var target = targets.filter(function (t) {

            return t.Id == id;
        })[0];

        return target ? target.Name : 'Unknown Device';
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