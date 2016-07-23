define(['dom'], function (dom) {

    function onGroupedCardClick(e, card) {

        var itemId = card.getAttribute('data-id');

        var userId = Dashboard.getCurrentUserId();

        var playedIndicator = card.querySelector('.playedIndicator');
        var playedIndicatorHtml = playedIndicator ? playedIndicator.innerHTML : null;

        var options = {

            Limit: parseInt(playedIndicatorHtml || '10'),
            Fields: "PrimaryImageAspectRatio,DateCreated",
            ParentId: itemId,
            GroupItems: false
        };

        var actionableParent = dom.parentWithTag(e.target, ['A', 'BUTTON', 'INPUT']);

        if (actionableParent && !actionableParent.classList.contains('cardContent')) {
            return;
        }

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).then(function (items) {

            if (items.length == 1) {
                Dashboard.navigate(LibraryBrowser.getHref(items[0]));
                return;
            }

            var url = 'itemdetails.html?id=' + itemId;

            Dashboard.navigate(url);
        });

        e.stopPropagation();
        e.preventDefault();
        return false;
    }

    function onItemsContainerClick(e) {
        var groupedCard = dom.parentWithClass(e.target, 'groupedCard');

        if (groupedCard) {
            onGroupedCardClick(e, groupedCard);
        }
    }
    return {
        onItemsContainerClick: onItemsContainerClick
    };
});