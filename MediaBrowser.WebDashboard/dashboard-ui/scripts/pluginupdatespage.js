var PluginUpdatesPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        $('.liPluginUpdate', this).remove();

        ApiClient.getInstalledPlugins().done(PluginUpdatesPage.loadPlugins);

    },

    loadPlugins: function (plugins) {

        var elem = $('#tbodyPluginUpdates', $.mobile.activePage).html('');

        for (var i = 0, length = plugins.length; i < length; i++) {

            PluginUpdatesPage.addPlugin(plugins[i], i, elem);

        }

        Dashboard.hideLoadingMsg();
    },

    addPlugin: function (plugin, fieldIndex, elem) {

        var html = "";

        html += "<tr>";

        html += "<td><h3>" + plugin.Name + "</h3></td>";

        var fieldId = "liPluginUpdateFielda" + fieldIndex;

        var enabledOptions = [
            { name: Globalize.translate('OptionOff'), value: 'Off' },
            { name: Globalize.translate('OptionOn'), value: 'On' }
        ];
        var options = PluginUpdatesPage.getHtmlOptions(enabledOptions, (plugin.EnableAutoUpdate ? "On" : "Off"));

        html += "<td style='vertical-align:middle;text-align:left;'>";
        html += "<select data-mini='true' data-id='" + plugin.Id + "' onchange='PluginUpdatesPage.setAutoUpdate(this);' data-role='slider' id='" + fieldId + "' name='" + fieldId + "'>" + options + "</select>";
        html += "</td>";

        fieldId = "liPluginUpdateFieldb" + fieldIndex;

        var updateOptions = [
            { name: Globalize.translate('OptionRelease'), value: 'Release' },
            { name: Globalize.translate('OptionBeta'), value: 'Beta' },
            { name: Globalize.translate('OptionDev'), value: 'Dev' }
        ];
        options = PluginUpdatesPage.getHtmlOptions(updateOptions, plugin.UpdateClass);

        html += "<td style='vertical-align:middle;text-align:left;'>";
        html += "<select data-mini='true' data-id='" + plugin.Id + "' onchange='PluginUpdatesPage.setUpdateClass(this);' data-inline='true' id='" + fieldId + "' name='" + fieldId + "'>" + options + "</select>";
        html += "</td>";

        html += "</tr>";

        elem.append(html).trigger('create');
    },

    getHtmlOptions: function (options, selectedValue) {

        var html = "";

        for (var i = 0, length = options.length; i < length; i++) {

            var option = options[i];
            var name = option.name;
            var value = option.value;

            if (value == selectedValue) {
                html += '<option value="' + value + '" selected="selected">' + name + '</option>';
            } else {
                html += '<option value="' + value + '">' + name + '</option>';
            }
        }


        return html;

    },

    setAutoUpdate: function (select) {

        var id = $(select).attr('data-id');

        Dashboard.showLoadingMsg();

        ApiClient.getPluginConfiguration(id).done(function (config) {

            config.EnableAutoUpdate = select.selectedIndex === 1;

            ApiClient.updatePluginConfiguration(id, config).done(Dashboard.hideLoadingMsg);
        });
    },

    setUpdateClass: function (select) {

        var id = $(select).attr('data-id');

        Dashboard.showLoadingMsg();

        ApiClient.getPluginConfiguration(id).done(function (config) {

            config.UpdateClass = select.value;

            ApiClient.updatePluginConfiguration(id, config).done(Dashboard.hideLoadingMsg);
        });
    }
};

$(document).on('pageshow', "#pluginUpdatesPage", PluginUpdatesPage.onPageShow);