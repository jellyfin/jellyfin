(function ($, document, LibraryBrowser) {

    function reload(page) {

        Dashboard.showLoadingMsg();

        var getItemPromise;

        var person = getParameterByName('person');

        if (person) {
            getItemPromise = ApiClient.getPerson(person);
        }

        else if (getParameterByName('studio')) {
            getItemPromise = ApiClient.getStudio(getParameterByName('studio'));
        }
        else if (getParameterByName('genre')) {
            getItemPromise = ApiClient.getGenre(getParameterByName('genre'));
        } else {
            throw new Error('Invalid request');
        }

        getItemPromise.done(function (item) {

            var name = item.Name;

            $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

            Dashboard.setPageTitle(name);

            $('#itemName', page).html(name);

            renderDetails(page, item);

            Dashboard.hideLoadingMsg();
        });
    }

    function renderDetails(page, item) {

        if (item.Overview || item.OverviewHtml) {
            var overview = item.OverviewHtml || item.Overview;

            $('#itemOverview', page).html(overview).show();
            $('#itemOverview a').each(function () {
                $(this).attr("target", "_blank");
            });
        } else {
            $('#itemOverview', page).hide();
        }

        renderUserDataIcons(page, item);
        renderLinks(page, item);
    }

    function renderLinks(page, item) {
        if (item.ProviderIds) {

            $('#itemLinks', page).html(LibraryBrowser.getLinksHtml(item));

        } else {
            $('#itemLinks', page).hide();
        }
    }

    function renderUserDataIcons(page, item) {
        $('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    $(document).on('pageshow', "#itemByNameDetailPage", function () {
        reload(this);
    });


})(jQuery, document, LibraryBrowser);