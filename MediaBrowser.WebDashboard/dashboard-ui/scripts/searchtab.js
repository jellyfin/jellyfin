define(["searchFields", "searchResults", "events"], function(SearchFields, SearchResults, events) {
    "use strict";

    function init(instance, tabContent, options) {
        tabContent.innerHTML = '<div class="padded-left padded-right searchFields"></div><div class="searchResults padded-top" style="padding-top:1.5em;"></div>', instance.searchFields = new SearchFields({
            element: tabContent.querySelector(".searchFields")
        }), instance.searchResults = new SearchResults({
            element: tabContent.querySelector(".searchResults"),
            serverId: ApiClient.serverId(),
            parentId: options.parentId,
            collectionType: options.collectionType
        }), events.on(instance.searchFields, "search", function(e, value) {
            instance.searchResults.search(value)
        })
    }

    function SearchTab(view, tabContent, options) {
        var self = this;
        options = options || {}, init(this, tabContent, options), self.preRender = function() {}, self.renderTab = function() {
            var searchFields = this.searchFields;
            searchFields && searchFields.focus()
        }
    }
    return SearchTab.prototype.destroy = function() {
        var searchFields = this.searchFields;
        searchFields && searchFields.destroy(), this.searchFields = null;
        var searchResults = this.searchResults;
        searchResults && searchResults.destroy(), this.searchResults = null
    }, SearchTab
});