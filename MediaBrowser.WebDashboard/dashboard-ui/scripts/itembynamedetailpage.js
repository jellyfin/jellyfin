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

        if (item.Type == "Person" && item.PremiereDate) {

            var birthday = parseISO8601Date(item.PremiereDate, { toLocal: true }).toDateString();

            $('#itemBirthday', page).show().html("Birthday:&nbsp;&nbsp;" + birthday);
        } else {
            $('#itemBirthday', page).hide();
        }

        if (item.Type == "Person" && item.EndDate) {

            var deathday = parseISO8601Date(item.EndDate, { toLocal: true }).toDateString();

            $('#itemDeathDate', page).show().html("Death day:&nbsp;&nbsp;" + deathday);
        } else {
            $('#itemDeathDate', page).hide();
        }

        if (item.Type == "Person" && item.ProductionLocations && item.ProductionLocations.length) {

            var gmap = '<a target="_blank" href="https://maps.google.com/maps?q=' + item.ProductionLocations[0] + '">' + item.ProductionLocations[0] + '</a>';

            $('#itemBirthLocation', page).show().html("Birthplace:&nbsp;&nbsp;" + gmap).trigger('create');
        } else {
            $('#itemBirthLocation', page).hide();
        }
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