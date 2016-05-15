define([], function () {

    function getDisplayName(item, options) {

        if (!item) {
            throw new Error("null item passed into getDisplayName");
        }

        options = options || {};

        var name = item.EpisodeTitle || item.Name || '';

        if (item.Type == "TvChannel") {

            if (item.Number) {
                return item.Number + ' ' + name;
            }
            return name;
        }
        if (/*options.isInlineSpecial &&*/ item.Type == "Episode" && item.ParentIndexNumber == 0) {

            name = Globalize.translate('sharedcomponents#ValueSpecialEpisodeName', name);

        } else if ((item.Type == "Episode" || item.Type == 'Program') && item.IndexNumber != null && item.ParentIndexNumber != null) {

            var displayIndexNumber = item.IndexNumber;

            var number = "E" + displayIndexNumber;

            if (options.includeParentInfo !== false) {
                number = "S" + item.ParentIndexNumber + ", " + number;
            }

            if (item.IndexNumberEnd) {

                displayIndexNumber = item.IndexNumberEnd;
                number += "-" + displayIndexNumber;
            }

            name = number + " - " + name;

        }

        if (item.EpisodeTitle) {
            name = item.Name + ' ' + name;
        }

        return name;
    }

    return {
        getDisplayName: getDisplayName
    };
});