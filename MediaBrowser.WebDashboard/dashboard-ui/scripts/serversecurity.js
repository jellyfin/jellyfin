define(["datetime", "loading", "libraryMenu", "dom", "globalize", "emby-button"], function(datetime, loading, libraryMenu, dom, globalize) {
    "use strict";

    function revoke(page, key) {
        require(["confirm"], function(confirm) {
            confirm(globalize.translate("MessageConfirmRevokeApiKey"), globalize.translate("HeaderConfirmRevokeApiKey")).then(function() {
                loading.show(), ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("Auth/Keys/" + key)
                }).then(function() {
                    loadData(page)
                })
            })
        })
    }

    function renderKeys(page, keys) {
        var rows = keys.map(function(item) {
            var html = "";
            html += '<tr class="detailTableBodyRow detailTableBodyRow-shaded">', html += '<td class="detailTableBodyCell">', html += '<button type="button" is="emby-button" data-token="' + item.AccessToken + '" class="raised raised-mini btnRevoke" data-mini="true" title="' + globalize.translate("ButtonRevoke") + '" style="margin:0;">' + globalize.translate("ButtonRevoke") + "</button>", html += "</td>", html += '<td class="detailTableBodyCell" style="vertical-align:middle;">', html += item.AccessToken, html += "</td>", html += '<td class="detailTableBodyCell" style="vertical-align:middle;">', html += item.AppName || "", html += "</td>", html += '<td class="detailTableBodyCell" style="vertical-align:middle;">';
            var date = datetime.parseISO8601Date(item.DateCreated, !0);
            return html += datetime.toLocaleDateString(date) + " " + datetime.getDisplayTime(date), html += "</td>", html += "</tr>"
        }).join("");
        page.querySelector(".resultBody").innerHTML = rows, loading.hide()
    }

    function loadData(page) {
        loading.show(), ApiClient.getJSON(ApiClient.getUrl("Auth/Keys")).then(function(result) {
            renderKeys(page, result.Items)
        })
    }

    function showNewKeyPrompt(page) {
        require(["prompt"], function(prompt) {
            prompt({
                title: globalize.translate("HeaderNewApiKey"),
                label: globalize.translate("LabelAppName"),
                description: globalize.translate("LabelAppNameExample")
            }).then(function(value) {
                ApiClient.ajax({
                    type: "POST",
                    url: ApiClient.getUrl("Auth/Keys", {
                        App: value
                    })
                }).then(function() {
                    loadData(page)
                })
            })
        })
    }

    function getTabs() {
        return [{
            href: "dashboardhosting.html",
            name: globalize.translate("TabHosting")
        }, {
            href: "serversecurity.html",
            name: globalize.translate("TabSecurity")
        }]
    }
    pageIdOn("pageinit", "serverSecurityPage", function() {
        var page = this;
        page.querySelector(".btnNewKey").addEventListener("click", function() {
            showNewKeyPrompt(page)
        }), page.querySelector(".tblApiKeys").addEventListener("click", function(e) {
            var btnRevoke = dom.parentWithClass(e.target, "btnRevoke");
            btnRevoke && revoke(page, btnRevoke.getAttribute("data-token"))
        })
    }), pageIdOn("pagebeforeshow", "serverSecurityPage", function() {
        libraryMenu.setTabs("adminadvanced", 1, getTabs), loadData(this)
    })
});