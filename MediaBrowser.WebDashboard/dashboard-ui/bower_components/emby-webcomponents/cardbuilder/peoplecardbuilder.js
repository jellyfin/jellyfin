define(['imageLoader', 'itemShortcuts', 'connectionManager'], function (imageLoader, itemShortcuts, connectionManager) {

    function buildPeopleCardsHtml(people, options) {

        var className = 'card ' + (options.shape || 'portrait') + 'Card personCard';

        if (options.block || options.rows) {
            className += ' block';
        }

        var html = '';
        var itemsInRow = 0;

        var serverId = options.serverId;
        var apiClient = connectionManager.getApiClient(serverId);

        for (var i = 0, length = people.length; i < length; i++) {

            if (options.rows && itemsInRow == 0) {
                html += '<div class="cardColumn">';
            }

            var person = people[i];

            html += buildPersonCard(person, apiClient, serverId, options, className);
            itemsInRow++;

            if (options.rows && itemsInRow >= options.rows) {
                itemsInRow = 0;
                html += '</div>';
            }
        }

        return html;
    }

    function getImgUrl(person, maxWidth, apiClient) {

        if (person.PrimaryImageTag) {

            return apiClient.getScaledImageUrl(person.Id, {

                maxWidth: maxWidth,
                tag: person.PrimaryImageTag,
                type: "Primary"
            });
        }

        return null;
    }

    function buildPersonCard(person, apiClient, serverId, options, className) {

        className += " itemAction scalableCard";

        var imgUrl = getImgUrl(person, options.width, apiClient);

        var cardImageContainerClass = 'cardImageContainer';
        if (options.coverImage) {
            cardImageContainerClass += ' coveredImage';
        }
        var cardImageContainer = imgUrl ? ('<div class="' + cardImageContainerClass + ' lazy" data-src="' + imgUrl + '">') : ('<div class="' + cardImageContainerClass + '">');

        if (!imgUrl) {
            cardImageContainer += '<i class="md-icon cardImageIcon">person</i>';
        }

        var nameHtml = '';
        nameHtml += '<div class="cardText">' + person.Name + '</div>';

        if (person.Role) {
            nameHtml += '<div class="cardText">as ' + person.Role + '</div>';
        }
        else if (person.Type) {
            nameHtml += '<div class="cardText">' + Globalize.translate('core#' + person.Type) + '</div>';
        } else {
            nameHtml += '<div class="cardText">&nbsp;</div>';
        }

        var html = '\
<button type="button" data-isfolder="' + person.IsFolder + '" data-type="' + person.Type + '" data-action="link" data-id="' + person.Id + '" data-serverid="' + serverId + '" raised class="' + className + '"> \
<div class="visualCardBox cardBox">\
<div class="cardScalable">\
<div class="cardPadder"></div>\
<div class="cardContent">\
' + cardImageContainer + '\
</div>\
</div>\
</div>\
<div class="cardFooter">\
' + nameHtml + '\
</div>\
</div>\
</button>'
        ;

        return html;
    }

    function buildPeopleCards(items, options) {

        if (options.parentContainer) {
            // Abort if the container has been disposed
            if (!document.body.contains(options.parentContainer)) {
                return;
            }

            if (items.length) {
                options.parentContainer.classList.remove('hide');
            } else {
                options.parentContainer.classList.add('hide');
                return;
            }
        }

        var html = buildPeopleCardsHtml(items, options);

        options.itemsContainer.innerHTML = html;

        imageLoader.lazyChildren(options.itemsContainer);

        itemShortcuts.off(options.itemsContainer);
        itemShortcuts.on(options.itemsContainer);
    }

    return {
        buildPeopleCards: buildPeopleCards
    };

});