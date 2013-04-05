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

	    ItemByNameDetailPage.item = item;

        ItemByNameDetailPage.renderImage(item);
        ItemByNameDetailPage.renderOverviewBlock(item);
	    ItemByNameDetailPage.renderFav(item);

        $('#itemName', page).html(name);

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
            var style = useBackgroundColor ? "background-color:" + LibraryBrowser.getMetroColor(item.Id) + ";" : "";

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

    },

	renderFav: function (item) {
		var html = '';
		var page = $.mobile.activePage;

		var userData = item.UserData || {};

		if (typeof userData.Likes == "undefined") {
		    html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" onclick="ItemDetailPage.setDislike();" />';
		    html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" onclick="ItemDetailPage.setLike();" />';
		} else if (userData.Likes) {
		    html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" onclick="ItemDetailPage.setDislike();" />';
		    html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_up_on.png" alt="Liked" title="Like" onclick="ItemDetailPage.clearLike();" />';
		} else {
		    html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_down_on.png" alt="Dislike" title="Dislike" onclick="ItemDetailPage.clearLike();" />';
		    html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" onclick="ItemDetailPage.setLike();" />';
		}

		if (userData.IsFavorite) {
		    html += '<img class="imgUserItemRating" src="css/images/userdata/heart_on.png" alt="Favorite" title="Favorite" onclick="ItemDetailPage.setFavorite();" />';
		} else {
		    html += '<img class="imgUserItemRating" src="css/images/userdata/heart_off.png" alt="Favorite" title="Favorite" onclick="ItemDetailPage.setFavorite();" />';
		}

		$('#itemRatings', page).html(html);
	},

	setFavorite: function () {
		var item = ItemByNameDetailPage.item;
/*
		item.UserData = item.UserData || {};

		var setting = !item.UserData.IsFavorite;
		item.UserData.IsFavorite = setting;

		ApiClient.updateFavoriteStatus(Dashboard.getCurrentUserId(), item.Id, setting);
*/
		ItemByNameDetailPage.renderFav(item);
	},

	setLike: function () {

		var item = ItemDetailPage.item;
/*
		item.UserData = item.UserData || {};

		item.UserData.Likes = true;

		ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), item.Id, true);
*/
		ItemByNameDetailPage.renderFav(item);
	},

	clearLike: function () {

		var item = ItemDetailPage.item;
/*
		item.UserData = item.UserData || {};

		item.UserData.Likes = undefined;

		ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), item.Id);
*/
		ItemByNameDetailPage.renderFav(item);
	},

	setDislike: function () {
		var item = ItemDetailPage.item;
/*
		item.UserData = item.UserData || {};

		item.UserData.Likes = false;

		ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), item.Id, false);
*/
		ItemByNameDetailPage.renderFav(item);
	}

};

$(document).on('pageshow', "#itemByNameDetailPage", ItemByNameDetailPage.onPageShow);
