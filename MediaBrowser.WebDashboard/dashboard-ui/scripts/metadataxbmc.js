(function ($, document, window) {

    var metadataKey = "xbmcmetadata";
    
    function loadPage(page, config, users) {

        var html = '<option value="" selected="selected"></option>';

        html += users.map(function (user) {
            return '<option value="' + user.Id + '">' + user.Name + '</option>';
        }).join('');

        $('#selectUser', page).html(html).val(config.UserId || '').selectmenu('refresh');
        $('#selectReleaseDateFormat', page).val(config.ReleaseDateFormat).selectmenu('refresh');
        $('#chkSaveImagePaths', page).checked(config.SaveImagePathsInNfo).checkboxradio('refresh');
        $('#chkEnablePathSubstitution', page).checked(config.EnablePathSubstitution).checkboxradio('refresh');
        $('#chkEnableExtraThumbs', page).checked(config.EnableExtraThumbsDuplication).checkboxradio('refresh');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#metadataXbmcPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getUsers();
        var promise2 = ApiClient.getNamedConfiguration(metadataKey);

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response2[0], response1[0]);
        });
    });

    window.XbmcMetadataPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getNamedConfiguration(metadataKey).done(function (config) {

                config.UserId = $('#selectUser', form).val() || null;
                config.ReleaseDateFormat = $('#selectReleaseDateFormat', form).val();
                config.SaveImagePathsInNfo = $('#chkSaveImagePaths', form).checked();
                config.EnablePathSubstitution = $('#chkEnablePathSubstitution', form).checked();
                config.EnableExtraThumbsDuplication = $('#chkEnableExtraThumbs', form).checked();

                ApiClient.updateNamedConfiguration(metadataKey, config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
