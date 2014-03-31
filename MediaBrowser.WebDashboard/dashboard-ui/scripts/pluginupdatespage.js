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
            Globalize.translate('OptionOff'),
            Globalize.translate('OptionOn')
        ];
        var options = PluginUpdatesPage.getHtmlOptions(enabledOptions, (plugin.EnableAutoUpdate ? "On" : "Off"));

        html += "<td style='vertical-align:middle;text-align:left;'>";
        html += "<select data-mini='true' data-id='" + plugin.Id + "' onchange='PluginUpdatesPage.setAutoUpdate(this);' data-role='slider' id='" + fieldId + "' name='" + fieldId + "'>" + options + "</select>";
        html += "</td>";

        fieldId = "liPluginUpdateFieldb" + fieldIndex;

        var updateOptions = [
            Globalize.translate('OptionRelease'),
            Globalize.translate('OptionBeta'),
            Globalize.translate('OptionDev')
        ];
        options = PluginUpdatesPage.getHtmlOptions(updateOptions, plugin.UpdateClass);

        html += "<td style='vertical-align:middle;text-align:left;'>";
        html += "<select data-mini='true' data-id='" + plugin.Id + "' onchange='PluginUpdatesPage.setUpdateClass(this);' data-inline='true' id='" + fieldId + "' name='" + fieldId + "'>" + options + "</select>";
        html += "</td>";

        html += "</tr>";

        elem.append(html).trigger('create');
    },

    getHtmlOptions: function (names, selectedValue) {

        var html = "";

        for (var i = 0, length = names.length; i < length; i++) {

            var name = names[i];

            if (name == selectedValue) {
                html += '<option value="' + name + '" selected="selected">' + name + '</option>';
            } else {
                html += '<option value="' + name + '">' + name + '</option>';
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