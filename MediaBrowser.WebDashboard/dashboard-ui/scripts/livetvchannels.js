(function ($, document, apiClient) {

    function getChannelHtml(channel) {

        var html = '';

        html += '<a class="backdropTileItem tileItem" href="livetvchannel.html?id=' + channel.Id + '">';

        var imgUrl;
        var isDefault;

        if (channel.ImageTags.Primary) {


            imgUrl = apiClient.getUrl("LiveTV/Channels/" + channel.Id + "/Images/Primary", {

                tag: channel.ImageTags.Primary,
                height: 300

            });


        } else {

            imgUrl = "css/images/items/list/collection.png";
            isDefault = true;
        }

        var cssClass = isDefault ? "tileImage defaultTileImage" : "tileImage";

        html += '<div class="' + cssClass + '" style="background-image: url(\'' + imgUrl + '\');"></div>';


        html += '<div class="tileContent">';

        html += '<div class="tileName">' + channel.Name + '</div>';

        html += '<p class="itemMiscInfo">' + channel.Number + '</p>';

        html += '<p class="userDataIcons">' + LibraryBrowser.getUserDataIconsHtml(channel) + '</p>';

        html += '</div>';

        html += "</a>";

        return html;
    }

    function getChannelsHtml(channels) {

        var html = [];

        for (var i = 0, length = channels.length; i < length; i++) {

            html.push(getChannelHtml(channels[i]));
        }

        return html.join('');
    }

    function renderChannels(page, channels) {

        $('#items', page).html(getChannelsHtml(channels)).trigger('create');
    }

    $(document).on('pagebeforeshow', "#liveTvChannelsPage", function () {

        var page = this;

        apiClient.getLiveTvChannels({
            
            userId: Dashboard.getCurrentUserId()

        }).done(function (result) {

            renderChannels(page, result.Items);
        });
    });

})(jQuery, document, ApiClient);