(function ($, document) {

    function reloadItems(page) {

        Sections.loadLatestChannelItems(page.querySelector('.items'), Dashboard.getCurrentUserId());
    }

    $(document).on('pagebeforeshowready', "#channelsLatestPage", function () {

        reloadItems(this);

    });

})(jQuery, document);