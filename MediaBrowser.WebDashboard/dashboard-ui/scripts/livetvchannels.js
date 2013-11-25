(function ($, document, apiClient) {

    function getChannelHtml(channel) {

        var html = '';

        html += '<a class="squareTileItem tileItem" href="livetvchannel.html?id=' + channel.Id + '">';

        var imgUrl;
        var isDefault;

        if (channel.PrimaryImageTag) {


            imgUrl = apiClient.getUrl("LiveTV/Channels/" + channel.Id + "/Images/Primary", {

                tag: channel.PrimaryImageTag,
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

        //var pagingHtml = LibraryBrowser.getPagingHtml({

        //    StartIndex: 0,
        //    Limit: channels.length

        //}, channels.length, true);

        //$('.listTopPaging', page).html(pagingHtml).trigger('create');

        $('#items', page).html(getChannelsHtml(channels)).trigger('create');
    }

    $(document).on('pagebeforeshow', "#liveTvChannelsPage", function () {

        var page = this;

        apiClient.getLiveTvChannels().done(function (result) {

            renderChannels(page, result.Items);
        });
    });

})(jQuery, document, ApiClient);