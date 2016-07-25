define(['jQuery'], function ($) {

    var metadataKey = "xbmcmetadata";

    function loadPage(page, config, users) {

        var html = '<option value="" selected="selected">' + Globalize.translate('OptionNone') + '</option>';

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

            ApiClient.updateNamedConfiguration(metadataKey, config).then(function () {
                Dashboard.processServerConfigurationUpdateResult();

                showConfirmMessage(config);
            });
        });

        // Disable default form submission
        return false;
    }

    function showConfirmMessage(config) {

        var msg = [];

        msg.push(Globalize.translate('MetadataSettingChangeHelp'));

        require(['alert'], function (alert) {
            alert({
                text: msg.join('<br/><br/>')
            });
        });
    }

    function getTabs() {
        return [
        {
            href: 'metadata.html',
            name: Globalize.translate('TabSettings')
        },
         {
             href: 'metadataimages.html',
             name: Globalize.translate('TabServices')
         },
         {
             href: 'metadatanfo.html',
             name: Globalize.translate('TabNfoSettings')
         }];
    }

    $(document).on('pageinit', "#metadataNfoPage", function () {

        $('.metadataNfoForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#metadataNfoPage", function () {

        LibraryMenu.setTabs('metadata', 2, getTabs);
        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getUsers();
        var promise2 = ApiClient.getNamedConfiguration(metadataKey);

        Promise.all([promise1, promise2]).then(function (responses) {

            loadPage(page, responses[1], responses[0]);
        });
    });

});
