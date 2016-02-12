(function ($, document, window) {

    var metadataKey = "xbmcmetadata";

    function loadPage(page, config, users) {

        var html = '<option value="" selected="selected"></option>';

        html += users.map(function (user) {
            return '<option value="' + user.Id + '">' + user.Name + '</option>';
        }).join('');

        $('#selectUser', page).html(html).val(config.UserId || '');
        $('#selectReleaseDateFormat', page).val(config.ReleaseDateFormat);

        page.querySelector('#chkSaveImagePaths').checked = config.SaveImagePathsInNfo;
        page.querySelector('#chkEnablePathSubstitution').checked = config.EnablePathSubstitution;
        page.querySelector('#chkEnableExtraThumbs').checked = config.EnableExtraThumbsDuplication;

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration(metadataKey).then(function (config) {

            config.UserId = $('#selectUser', form).val() || null;
            config.ReleaseDateFormat = $('#selectReleaseDateFormat', form).val();

            config.SaveImagePathsInNfo = form.querySelector('#chkSaveImagePaths').checked;
            config.EnablePathSubstitution = form.querySelector('#chkEnablePathSubstitution').checked;
            config.EnableExtraThumbsDuplication = form.querySelector('#chkEnableExtraThumbs').checked;

            ApiClient.updateNamedConfiguration(metadataKey, config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#metadataNfoPage", function () {

        $('.metadataNfoForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#metadataNfoPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getUsers();
        var promise2 = ApiClient.getNamedConfiguration(metadataKey);

        Promise.all([promise1, promise2]).then(function (responses) {

            loadPage(page, responses[1], responses[0]);
        });
    });

})(jQuery, document, window);
