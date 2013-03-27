var ItemByNameDetailPage = {

    onPageShow: function () {
        ItemByNameDetailPage.reload();
    },

    reload: function () {

        Dashboard.showLoadingMsg();

        var person = getParameterByName('person');

        if (person) {
            ApiClient.getPerson(person).done(ItemByNameDetailPage.renderItem);
            return;
        }

        var studio = getParameterByName('studio');

        if (studio) {
            ApiClient.getStudio(person).done(ItemByNameDetailPage.renderItem);
            return;
        }

        var genre = getParameterByName('genre');

        if (genre) {
            ApiClient.getGenre(genre).done(ItemByNameDetailPage.renderItem);
            return;
        }
    },

    renderItem: function (item) {

        Dashboard.hideLoadingMsg();
    }
};

$(document).on('pageshow', "#itemByNameDetailPage", ItemByNameDetailPage.onPageShow);
