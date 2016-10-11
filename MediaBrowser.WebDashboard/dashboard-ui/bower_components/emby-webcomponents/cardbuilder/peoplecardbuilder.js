define(['cardBuilder'], function (cardBuilder) {
    'use strict';

    function buildPeopleCards(items, options) {

        options = Object.assign(options || {}, {
            cardLayout: true,
            centerText: true,
            showTitle: true,
            cardFooterAside: 'none',
            showPersonRoleOrType: true,
            cardCssClass: 'personCard'
        });
        cardBuilder.buildCards(items, options);
    }

    return {
        buildPeopleCards: buildPeopleCards
    };

});