(function ($, document) {

    function reloadFromChannels(page, channels) {

        var channelsHtml = channels.map(function (c) {

            return '<div id="channel' + c.Id + '"></div>';

        }).join('');

        $('.items', page).html(channelsHtml);

        for (var i = 0, length = channels.length; i < length; i++) {

            var channel = channels[i];

            reloadFromChannel(page, channel, i);
        }
    }

    function reloadFromChannel(page, channel, index) {

        var options = {

            Limit: 7,
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed",
            UserId: Dashboard.getCurrentUserId(),
            ChannelIds: channel.Id
        };

        $.getJSON(ApiClient.getUrl("Channels/Items/Latest", options)).done(function (result) {

            var html = '';

            if (result.Items.length) {

                var text = Globalize.translate('HeaderLatestFromChannel').replace('{0}', channel.Name);
                if (index) {
                    html += '<h1 class="listHeader">' + text + '</h1>';
                } else {
                    html += '<h1 class="listHeader firstListHeader">' + text + '</h1>';
                }
            }
            html += LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: 'auto',
                showTitle: true,
                centerText: true,
                context: 'channels',
                lazy: true
            });

            $('#channel' + channel.Id + '', page).html(html).trigger('create').createPosterItemMenus();
        });
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var options = {

            UserId: Dashboard.getCurrentUserId(),
            SupportsLatestItems: true
        };

        $.getJSON(ApiClient.getUrl("Channels", options)).done(function (result) {

            reloadFromChannels(page, result.Items);

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pagebeforeshow', "#channelsLatestPage", function () {

        reloadItems(this);

    });

})(jQuery, document);