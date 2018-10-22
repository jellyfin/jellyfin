define(["loading", "emby-checkbox", "emby-button", "emby-select"], function(loading) {
    "use strict";

    function save(page) {
        loading.show();
        var apiClient = ApiClient;
        apiClient.getJSON(apiClient.getUrl("Startup/Configuration")).then(function(config) {
            config.PreferredMetadataLanguage = page.querySelector("#selectLanguage").value, config.MetadataCountryCode = page.querySelector("#selectCountry").value, apiClient.ajax({
                type: "POST",
                data: config,
                url: apiClient.getUrl("Startup/Configuration")
            }).then(function() {
                loading.hide(), navigateToNextPage()
            })
        })
    }

    function populateLanguages(select, languages) {
        var html = "";
        html += "<option value=''></option>";
        for (var i = 0, length = languages.length; i < length; i++) {
            var culture = languages[i];
            html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>"
        }
        select.innerHTML = html
    }

    function populateCountries(select, allCountries) {
        var html = "";
        html += "<option value=''></option>";
        for (var i = 0, length = allCountries.length; i < length; i++) {
            var culture = allCountries[i];
            html += "<option value='" + culture.TwoLetterISORegionName + "'>" + culture.DisplayName + "</option>"
        }
        select.innerHTML = html
    }

    function reloadData(page, config, cultures, countries) {
        populateLanguages(page.querySelector("#selectLanguage"), cultures), populateCountries(page.querySelector("#selectCountry"), countries), page.querySelector("#selectLanguage").value = config.PreferredMetadataLanguage, page.querySelector("#selectCountry").value = config.MetadataCountryCode, loading.hide()
    }

    function reload(page) {
        loading.show();
        var apiClient = ApiClient,
            promise1 = apiClient.getJSON(apiClient.getUrl("Startup/Configuration")),
            promise2 = apiClient.getCultures(),
            promise3 = apiClient.getCountries();
        Promise.all([promise1, promise2, promise3]).then(function(responses) {
            reloadData(page, responses[0], responses[1], responses[2])
        })
    }

    function navigateToNextPage() {
        Dashboard.navigate("wizardremoteaccess.html")
    }

    function onSubmit(e) {
        return save(this), e.preventDefault(), !1
    }
    return function(view, params) {
        view.querySelector(".wizardSettingsForm").addEventListener("submit", onSubmit), view.addEventListener("viewshow", function() {
            document.querySelector(".skinHeader").classList.add("noHomeButtonHeader"), reload(this)
        }), view.addEventListener("viewhide", function() {
            document.querySelector(".skinHeader").classList.remove("noHomeButtonHeader")
        })
    }
});