define(["dom", "appRouter", "connectionManager"], function(dom, appRouter, connectionManager) {
    "use strict";

    function onGroupedCardClick(e, card) {
        var itemId = card.getAttribute("data-id"),
            serverId = card.getAttribute("data-serverid"),
            apiClient = connectionManager.getApiClient(serverId),
            userId = apiClient.getCurrentUserId(),
            playedIndicator = card.querySelector(".playedIndicator"),
            playedIndicatorHtml = playedIndicator ? playedIndicator.innerHTML : null,
            options = {
                Limit: parseInt(playedIndicatorHtml || "10"),
                Fields: "PrimaryImageAspectRatio,DateCreated",
                ParentId: itemId,
                GroupItems: !1
            },
            actionableParent = dom.parentWithTag(e.target, ["A", "BUTTON", "INPUT"]);
        if (!actionableParent || actionableParent.classList.contains("cardContent")) return apiClient.getJSON(apiClient.getUrl("Users/" + userId + "/Items/Latest", options)).then(function(items) {
            if (1 === items.length) return void appRouter.showItem(items[0]);
            var url = "itemdetails.html?id=" + itemId + "&serverId=" + serverId;
            Dashboard.navigate(url)
        }), e.stopPropagation(), e.preventDefault(), !1
    }

    function onItemsContainerClick(e) {
        var groupedCard = dom.parentWithClass(e.target, "groupedCard");
        groupedCard && onGroupedCardClick(e, groupedCard)
    }
    return {
        onItemsContainerClick: onItemsContainerClick
    }
});