define(["focusManager", "searchFields", "searchResults", "events"], function(focusManager, SearchFields, SearchResults, events) {
    "use strict";
    return function(view, params) {
        function onSearch(e, value) {
            self.searchResults.search(value)
        }
        var self = this;
        view.addEventListener("viewshow", function() {
            self.searchFields || (self.searchFields = new SearchFields({
                element: view.querySelector(".searchFields")
            }), self.searchResults = new SearchResults({
                element: view.querySelector(".searchResults"),
                serverId: params.serverId || ApiClient.serverId(),
                parentId: params.parentId,
                collectionType: params.collectionType
            }), events.on(self.searchFields, "search", onSearch))
        }), view.addEventListener("viewdestroy", function() {
            self.searchFields && (self.searchFields.destroy(), self.searchFields = null), self.searchResults && (self.searchResults.destroy(), self.searchResults = null)
        })
    }
});