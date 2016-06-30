define(['itemHelper', 'mediaInfo', 'indicators', 'css!./listview'], function (itemHelper, mediaInfo, indicators) {

    function getListViewHtml(items, options) {

        var outerHtml = "";

        var index = 0;
        var groupTitle = '';
        var action = options.action || 'link';

        var isLargeStyle = options.imageSize == 'large';
        var enableOverview = options.enableOverview;

        outerHtml += items.map(function (item) {

            var html = '';

            var cssClass = "itemAction listItem";

            var downloadWidth = 80;

            if (isLargeStyle) {
                cssClass += " largeImage";
                downloadWidth = 500;
            }

            html += '<button class="' + cssClass + '" data-index="' + index + '" data-action="' + action + '" data-isfolder="' + item.IsFolder + '" data-id="' + item.Id + '"  data-serverid="' + item.ServerId + '" data-type="' + item.Type + '">';

            var imgUrl = Emby.Models.imageUrl(item, {
                width: downloadWidth,
                type: "Primary"
            });

            if (!imgUrl) {
                imgUrl = Emby.Models.thumbImageUrl(item, {
                    width: downloadWidth,
                    type: "Thumb"
                });
            }

            if (imgUrl) {
                html += '<div class="listItemImage lazy" data-src="' + imgUrl + '" item-icon>';
            } else {
                html += '<div class="listItemImage" item-icon>';
            }

            var indicatorsHtml = '';
            indicatorsHtml += indicators.getPlayedIndicatorHtml(item);

            if (indicatorsHtml) {
                html += '<div class="indicators">' + indicatorsHtml + '</div>';
            }

            var progressHtml = indicators.getProgressBarHtml(item);

            if (progressHtml) {
                html += progressHtml;
            }
            html += '</div>';

            var textlines = [];

            if (options.showParentTitle) {
                if (item.Type == 'Episode') {
                    textlines.push(item.SeriesName || '&nbsp;');
                } else if (item.Type == 'MusicAlbum') {
                    textlines.push(item.AlbumArtist || '&nbsp;');
                }
            }

            var displayName = itemHelper.getDisplayName(item);

            if (options.showIndexNumber && item.IndexNumber != null) {
                displayName = item.IndexNumber + ". " + displayName;
            }
            textlines.push(displayName);

            if (item.Type == 'Audio') {
                textlines.push(item.ArtistItems.map(function (a) {
                    return a.Name;

                }).join(', ') || '&nbsp;');
            }

            var lineCount = textlines.length;
            if (!options.enableSideMediaInfo) {
                lineCount++;
            }
            if (enableOverview && item.Overview) {
                lineCount++;
            }

            html += '<div class="listItemBody">';

            for (var i = 0, textLinesLength = textlines.length; i < textLinesLength; i++) {

                if (i == 0 && isLargeStyle) {
                    html += '<h2 class="listItemTitle">';
                }
                else if (i == 0) {
                    html += '<div>';
                } else {
                    html += '<div class="secondary">';
                }
                html += textlines[i] || '&nbsp;';
                if (i == 0 && isLargeStyle) {
                    html += '</h2>';
                } else {
                    html += '</div>';
                }
            }

            if (!options.enableSideMediaInfo) {
                html += '<div class="secondary listItemMediaInfo">' + mediaInfo.getPrimaryMediaInfoHtml(item) + '</div>';
            }

            if (enableOverview && item.Overview) {
                html += '<div class="secondary overview">';
                html += item.Overview;
                html += '</div>';
            }

            html += '</div>';

            if (options.enableSideMediaInfo) {
                html += '<div class="secondary listItemMediaInfo">' + mediaInfo.getPrimaryMediaInfoHtml(item) + '</div>';
            }

            html += '</button>';

            index++;
            return html;

        }).join('');

        return outerHtml;
    }

    return {
        getListViewHtml: getListViewHtml
    };
});