(function () {

    function reload(page) {

        ApiClient.getServerConfiguration().done(function (config) {

            var notificationOptions = config.NotificationOptions;
            
            $('#chkNewLibraryContent', page).checked(notificationOptions.SendOnNewLibraryContent).checkboxradio('refresh');
            $('#chkFailedTasks', page).checked(notificationOptions.SendOnFailedTasks).checkboxradio('refresh');
            $('#chkUpdates', page).checked(notificationOptions.SendOnUpdates).checkboxradio('refresh');
            
            $('#chkServerRestartRequired', page).checked(notificationOptions.SendOnServerRestartRequired).checkboxradio('refresh');

            $('#chkVideoPlayback', page).checked(notificationOptions.SendOnVideoPlayback).checkboxradio('refresh');
            $('#chkAudioPlayback', page).checked(notificationOptions.SendOnAudioPlayback).checkboxradio('refresh');
            $('#chkGamePlayback', page).checked(notificationOptions.SendOnGamePlayback).checkboxradio('refresh');

        });
    }

    function save(page) {

        ApiClient.getServerConfiguration().done(function (config) {

            var notificationOptions = config.NotificationOptions;

            notificationOptions.SendOnNewLibraryContent = $('#chkNewLibraryContent', page).checked();
            notificationOptions.SendOnFailedTasks = $('#chkFailedTasks', page).checked();
            notificationOptions.SendOnUpdates = $('#chkUpdates', page).checked();
            
            notificationOptions.SendOnServerRestartRequired = $('#chkServerRestartRequired', page).checked();

            notificationOptions.SendOnVideoPlayback = $('#chkVideoPlayback', page).checked();
            notificationOptions.SendOnAudioPlayback = $('#chkAudioPlayback', page).checked();
            notificationOptions.SendOnGamePlayback = $('#chkGamePlayback', page).checked();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

    }

    $(document).on('pageshow', "#notificationSettingsPage", function () {

        var page = this;

        reload(page);
    });

    window.NotificationSettingsPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');
            save(page);
            return false;
        }
    };

})(jQuery, window);