define(["jQuery", "dom", "loading", "libraryMenu", "listViewStyle"], function($, dom, loading, libraryMenu) {
    "use strict";

    function populateLanguages(select) {
        return ApiClient.getCultures().then(function(languages) {
            var html = "";
            html += "<option value=''></option>";
            for (var i = 0, length = languages.length; i < length; i++) {
                var culture = languages[i];
                html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>"
            }
            select.innerHTML = html
        })
    }

    function populateCountries(select) {
        return ApiClient.getCountries().then(function(allCountries) {
            var html = "";
            html += "<option value=''></option>";
            for (var i = 0, length = allCountries.length; i < length; i++) {
                var culture = allCountries[i];
                html += "<option value='" + culture.TwoLetterISORegionName + "'>" + culture.DisplayName + "</option>"
            }
            select.innerHTML = html
        })
    }

    function loadPage(page) {
        var promises = [ApiClient.getServerConfiguration(), populateLanguages(page.querySelector("#selectLanguage")), populateCountries(page.querySelector("#selectCountry"))];
        Promise.all(promises).then(function(responses) {
            var config = responses[0];
            page.querySelector("#selectLanguage").value = config.PreferredMetadataLanguage || "", page.querySelector("#selectCountry").value = config.MetadataCountryCode || "", loading.hide()
        })
    }

    function onSubmit() {
        var form = this;
        return loading.show(), ApiClient.getServerConfiguration().then(function(config) {
            config.PreferredMetadataLanguage = form.querySelector("#selectLanguage").value, config.MetadataCountryCode = form.querySelector("#selectCountry").value, ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult)
        }), !1
    }

    function getTabs() {
        return [{
            href: "library.html",
            name: Globalize.translate("HeaderLibraries")
        }, {
            href: "librarydisplay.html",
            name: Globalize.translate("TabDisplay")
        }, {
            href: "metadataimages.html",
            name: Globalize.translate("TabMetadata")
        }, {
            href: "metadatanfo.html",
            name: Globalize.translate("TabNfoSettings")
        }, {
            href: "librarysettings.html",
            name: Globalize.translate("TabAdvanced")
        }]
    }
    $(document).on("pageinit", "#metadataImagesConfigurationPage", function() {
        $(".metadataImagesConfigurationForm").off("submit", onSubmit).on("submit", onSubmit)
    }).on("pageshow", "#metadataImagesConfigurationPage", function() {
        libraryMenu.setTabs("metadata", 2, getTabs), loading.show(), loadPage(this)
    })
});