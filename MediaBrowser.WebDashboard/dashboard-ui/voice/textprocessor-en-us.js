define([], function () {

    function parseTextInternal(text) {

        var result = {
            action: '',
            itemName: '',
            itemType: '',
            category: '',
            filters: [],
            removeWords: [],
            sortby: '',
            sortorder: 'Ascending',
            limit: null,
            userId: Dashboard.getCurrentUserId()
        };

        var textLower = text.toLowerCase();
        var words = text.toLowerCase().split(' ');

        var displayWords = [
            'show',
            'pull up',
            'display',
            'go to',
            'view'
        ];

        if (displayWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            if (words.indexOf('guide') != -1) {
                result.action = 'show';
                result.category = 'tvguide';
            }

            if (words.indexOf('recordings') != -1) {
                result.action = 'show';
                result.category = 'recordings';
            }

            result.removeWords = displayWords;
            return result;
        }

        var searchWords = [
         'search',
         'search for',
         'find',
         'query'
        ];

        if (searchWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Search
            result.action = 'search';

            result.removeWords = searchWords;
            return result;
        }

        var playWords = [
         'play',
         'watch'
        ];

        if (playWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'play';

            result.removeWords = playWords;
            return result;
        }

        var controlWords = [
         'use',
         'control'
        ];

        if (controlWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'control';

            result.removeWords = controlWords;
            return result;
        }

        var enableWords = [
         'enable',
         'turn on'
        ];

        if (enableWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'enable';

            result.removeWords = enableWords;
            return result;
        }

        var disableWords = [
         'disable',
         'turn off'
        ];

        if (disableWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'disable';

            result.removeWords = disableWords;
            return result;
        }

        var toggleWords = [
         'toggle'
        ];

        if (toggleWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'toggle';

            result.removeWords = toggleWords;
            return result;
        }

        if (words.indexOf('shuffle') != -1) {

            // Play
            result.action = 'shuffle';

            result.removeWords.push('shuffle');
            return result;
        }

        if (words.indexOf('record') != -1) {

            // Record
            result.action = 'record';

            result.removeWords.push('record');
            return result;
        }

        if (words.indexOf('guide') != -1) {
            result.action = 'show';
            result.category = 'tvguide';
            return result;
        }

        return result;
    }

    function parseContext(text, result) {

        text = text.toLowerCase();

        var i, length;

        for (i = 0, length = result.removeWords.length; i < length; i++) {

            text = text.replace(result.removeWords[i], '');
        }

        text = text.trim();

        var removeAtStart = [
            'my'
        ];

        for (i = 0, length = removeAtStart.length; i < length; i++) {

            if (text.indexOf(removeAtStart[i]) == 0) {
                text = text.substring(removeAtStart[i].length);
            }
        }

        result.what = text;

        text = text.trim();
        var words = text.toLowerCase().split(' ');

        if (words.indexOf('favorite') != -1) {
            result.filters.push('favorite');
        }

        if (text.indexOf('latest movies') != -1 || text.indexOf('latest films') != -1) {

            result.sortby = 'datecreated';
            result.sortorder = 'Descending';
            result.filters.push('unplayed');
            result.itemType = 'Movie';

            return;
        }

        if (text.indexOf('latest episodes') != -1) {

            result.sortby = 'datecreated';
            result.sortorder = 'Descending';
            result.filters.push('unplayed');
            result.itemType = 'Episode';

            return;
        }

        if (text.indexOf('next up') != -1) {

            result.category = 'nextup';

            return;
        }

        if (text.indexOf('movies') != -1 || text.indexOf('films') != -1) {

            result.itemType = 'Movie';

            return;
        }

        if (text.indexOf('shows') != -1 || text.indexOf('series') != -1) {

            result.itemType = 'Series';

            return;
        }

        if (text.indexOf('songs') != -1) {

            result.itemType = 'Audio';

            return;
        }
    }

    return function (text) {
        var result = parseTextInternal(text);
        parseContext(text, result);
        return result;
    }
});