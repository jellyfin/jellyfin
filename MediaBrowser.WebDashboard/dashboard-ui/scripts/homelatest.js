(function ($, document) {

    function loadSections(page, user) {

        var userId = user.Id;

        var i, length;
        var sectionCount = 3;

        var elem = $('.sections', page);

        if (!elem.html().length) {
            var html = '';
            for (i = 0, length = sectionCount; i < length; i++) {

                html += '<div class="homePageSection section' + i + '"></div>';
            }

            elem.html(html);
        }

        var context = 'home-latest';

        var latestMediElem = $('.section0', page);

        Dashboard.showLoadingMsg();
        var promises = [];

        promises.push(Sections.loadRecentlyAdded(latestMediElem, user, context));
        promises.push(Sections.loadLatestLiveTvRecordings($(".section1", page), userId));
        promises.push(Sections.loadLatestChannelItems($(".section2", page), userId));

        $.when(promises).done(function() {
            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pagebeforeshowready', "#homeLatestPage", function () {

        var page = this;

        Dashboard.getCurrentUser().done(function (user) {
            loadSections(page, user);
        });

    });

})(jQuery, document);