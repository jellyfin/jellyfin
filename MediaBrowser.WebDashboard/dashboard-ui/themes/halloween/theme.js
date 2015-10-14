(function () {

    Dashboard.importCss('themes/halloween/style.css');


    function onPageShow() {
        var page = this;

        if (!$.browser.mobile && !page.classList.contains('itemDetailPage')) {
            Backdrops.setBackdropUrl(page, 'themes/halloween/bg.jpg');
        }
    }

    pageClassOn('pageshow', "libraryPage", onPageShow);

    if ($($.mobile.activePage)[0].classList.contains('libraryPage')) {
        onPageShow.call($($.mobile.activePage)[0]);
    }

})();