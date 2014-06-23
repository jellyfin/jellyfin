(function ($, document) {

    function reloadItems(page) {

        Sections.loadLatestChannelItems($(".items", page), Dashboard.getCurrentUserId());
    }

    $(document).on('pagebeforeshow', "#channelsLatestPage", function () {

        reloadItems(this);

    });

})(jQuery, document);