(function ($, document, window) {

    function populateDownloadList(page, config, downloadableList) {

        if (downloadableList.length) {
            $('.channelDownloading', page).show();
        } else {
            $('.channelDownloading', page).hide();
        }

        var html = '';
        
        html += '<div data-role="controlgroup">';

        for (var i = 0, length = downloadableList.length; i < length; i++) {

            var channel = downloadableList[i];

            var id = 'chkChannelDownload' + i;

            var isChecked = config.DownloadingChannels.indexOf(channel.Id) != -1 ? ' checked="checked"' : '';

            html += '<input class="chkChannelDownload" type="checkbox" name="' + id + '" id="' + id + '" data-channelid="' + channel.Id + '" data-mini="true"' + isChecked + '>';
            html += '<label for="' + id + '">' + channel.Name + '</label>';
        }

        html += '</div>';

        $('.channelDownloadingList', page).html(html).trigger('create');
    }
    
    function loadPage(page, config, allChannelFeatures) {

        if (allChannelFeatures.length) {
            $('.noChannelsHeader', page).hide();
        } else {
            $('.noChannelsHeader', page).show();
        }
        
        var downloadableList = allChannelFeatures.filter(function (i) {
            return i.SupportsContentDownloading;
        });

        populateDownloadList(page, config, downloadableList);

        $('#selectChannelResolution', page).val(config.PreferredStreamingWidth || '')
            .selectmenu("refresh");

        $('#txtDownloadAge', page).val(config.MaxDownloadAge || '');

        $('#txtCachePath', page).val(config.DownloadPath || '');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageinit', "#channelSettingsPage", function () {

        var page = this;

        $('#btnSelectCachePath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtCachePath', page).val(path);
                    }
                    picker.close();
                },

                header: Globalize.translate('HeaderSelectChannelDownloadPath'),

                instruction: Globalize.translate('HeaderSelectChannelDownloadPathHelp')
            });
        });

    }).on('pageshow', "#channelSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getNamedConfiguration("channels");
        var promise2 = $.getJSON(ApiClient.getUrl("Channels/Features"));

        $.when(promise1, promise2).done(function (response1, response2) {

            var config = response1[0];
            var allFeatures = response2[0];

            loadPage(page, config, allFeatures);

        });

    });

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("channels").done(function (config) {

            // This should be null if empty
            config.PreferredStreamingWidth = $('#selectChannelResolution', form).val() || null;
            config.MaxDownloadAge = $('#txtDownloadAge', form).val() || null;

            config.DownloadPath = $('#txtCachePath', form).val() || null;

            config.DownloadingChannels = $('.chkChannelDownload:checked', form)
                .get()
                .map(function(i) {
                    return i.getAttribute('data-channelid');
                });

            ApiClient.updateNamedConfiguration(config).done("channels", Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    window.ChannelSettingsPage = {
        onSubmit: onSubmit
    };

})(jQuery, document, window);
