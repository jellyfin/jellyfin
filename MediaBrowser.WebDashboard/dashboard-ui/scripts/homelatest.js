(function ($, document) {

    function loadSections(page, userId) {

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

        Sections.loadRecentlyAdded($('.section0', page), userId, context);
        Sections.loadLatestLiveTvRecordings($(".section1", page), userId);
        Sections.loadLatestChannelItems($(".section2", page), userId);
    }

    $(document).on('pagebeforeshow', "#homeLatestPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        loadSections(page, userId);

    });

})(jQuery, document);