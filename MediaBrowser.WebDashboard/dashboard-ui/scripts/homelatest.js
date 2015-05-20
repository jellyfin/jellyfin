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

        Sections.loadRecentlyAdded(latestMediElem, user, context).done(function () {

            $('h1', latestMediElem).addClass('firstListHeader');
        });
        Sections.loadLatestLiveTvRecordings($(".section1", page), userId);
        Sections.loadLatestChannelItems($(".section2", page), userId);
    }

    $(document).on('pageshowready', "#homeLatestPage", function () {

        var page = this;

        Dashboard.getCurrentUser().done(function (user) {
            loadSections(page, user);
        });

    });

})(jQuery, document);