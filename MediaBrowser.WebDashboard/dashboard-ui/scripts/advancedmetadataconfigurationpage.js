var AdvancedMetadataConfigurationPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();
        var promise2 = ApiClient.getItemTypes({ HasInternetProvider: true });

        $.when(promise1, promise2).done(function (response1, response2) {

            AdvancedMetadataConfigurationPage.load(page, response1[0], response2[0]);

        });
    },

    load: function (page, config, itemTypes) {

        AdvancedMetadataConfigurationPage.loadItemTypes(page, config, itemTypes);
        Dashboard.hideLoadingMsg();
    },

    loadItemTypes: function (page, configuration, types) {

        var html = '<div data-role="controlgroup">';

        for (var i = 0, length = types.length; i < length; i++) {

            var type = types[i];
            var id = "checkbox-" + i + "a";

            var checkedAttribute = configuration.InternetProviderExcludeTypes.indexOf(type) != -1 ? ' checked="checked"' : '';

            html += '<input' + checkedAttribute + ' class="chkItemType" data-mini="true" data-itemtype="' + type + '" type="checkbox" name="' + id + '" id="' + id + '" />';
            html += '<label for="' + id + '">' + type + '</label>';
        }

        html += "</div>";

        $('#divItemTypes', page).html(html).trigger("create");
    },

    onSubmit: function () {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.InternetProviderExcludeTypes = $.map($('.chkItemType:checked', form), function (currentCheckbox) {

                return currentCheckbox.getAttribute('data-itemtype');
            });

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#advancedMetadataConfigurationPage", AdvancedMetadataConfigurationPage.onPageShow);