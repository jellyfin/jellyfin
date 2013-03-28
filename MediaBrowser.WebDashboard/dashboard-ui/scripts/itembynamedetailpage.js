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
            ApiClient.getStudio(studio).done(ItemByNameDetailPage.renderItem);
            return;
        }

        var genre = getParameterByName('genre');

        if (genre) {
            ApiClient.getGenre(genre).done(ItemByNameDetailPage.renderItem);
            return;
        }
    },

    renderItem: function (item) {

        var page = $.mobile.activePage;
        var name = item.Name;

        Dashboard.setPageTitle(name);

        ItemByNameDetailPage.renderImage(item);
        ItemByNameDetailPage.renderOverviewBlock(item);

        $('#itemByNameDetailPage', page).html(name);

        Dashboard.hideLoadingMsg();
    },

    renderImage: function (item) {

        var page = $.mobile.activePage;
        var imageTags = item.ImageTags || {};
        var html = '';
        var url;
        var useBackgroundColor;

        if (item.Type == "Person") {
            if (imageTags.Primary) {

                url = ApiClient.getPersonImageUrl(item.Name, {
                    width: 800,
                    tag: imageTags.Primary,
                    type: "primary"
                });

            } else {
                url = 'css/images/items/list/person.png';
                useBackgroundColor = true;
            }
        }else if (item.Type == "Studio") {
            if (imageTags.Primary) {

                url = ApiClient.getStudioImageUrl(item.Name, {
                    width: 800,
                    tag: item.PrimaryImageTag,
                    type: "primary"
                });

            } else {
                url = 'css/images/items/detail/video.png';
                useBackgroundColor = true;
            }
        }else if (item.Type == "Genre") {
            if (imageTags.Primary) {

                url = ApiClient.getGenreImageUrl(item.Name, {
                    width: 800,
                    tag: item.PrimaryImageTag,
                    type: "primary"
                });

            } else {
                url = 'css/images/items/detail/video.png';
                useBackgroundColor = true;
            }
        }

        if (url) {
            var style = useBackgroundColor ? "background-color:" + Dashboard.getRandomMetroColor() + ";" : "";

            html += "<img class='itemDetailImage' src='" + url + "' style='" + style + "' />";
        }

        $('#itemImage', page).html(html);
    },

    renderOverviewBlock: function (item) {

        var page = $.mobile.activePage;

        if (item.Overview || item.OverviewHtml) {
            var overview = item.OverviewHtml || item.Overview;

            $('#itemOverview', page).html(overview).show();
            $('#itemOverview a').each(function(){
                $(this).attr("target","_blank");
            });
        } else {
            $('#itemOverview', page).hide();
        }

    }

};

$(document).on('pageshow', "#itemByNameDetailPage", ItemByNameDetailPage.onPageShow);
