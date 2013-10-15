(function ($, document) {

    function save(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getScheduledTasks().done(function (tasks) {

            var chapterTask = tasks.filter(function (t) {
                return t.Name.toLowerCase() == 'chapter image extraction';
            })[0];

            if (!chapterTask) {
                throw new Error('Cannot find chapter scheduled task');
            }

            // First update the chapters scheduled task
            var triggers = $('#chkChapters', page).checked() ? [{
                "Type": "DailyTrigger",
                "TimeOfDayTicks": 144000000000
            }] : [];

            ApiClient.updateScheduledTaskTriggers(chapterTask.Id, triggers).done(function () {


                // After saving chapter task, now save server config
                ApiClient.getServerConfiguration().done(function (config) {

                    config.SaveLocalMeta = $('#chkSaveLocalMetadata', page).checked();
                    config.EnableVideoImageExtraction = $('#chkVIdeoImages', page).checked();
                    config.ImageSavingConvention = $('#selectImageSavingConvention', page).val();

                    ApiClient.updateServerConfiguration(config).done(function (result) {

                        Dashboard.processServerConfigurationUpdateResult(result);

                        navigateToNextPage();

                    });
                });


            });
        });

    }

    function navigateToNextPage() {

        ApiClient.getSystemInfo().done(function (systemInfo) {

            var os = systemInfo.OperatingSystem.toLowerCase();

            if (os.indexOf('windows') != -1) {
                Dashboard.navigate('wizardservice.html');
            } else {
                Dashboard.navigate('wizardfinish.html');
            }

        });
    }

    $(document).on('pageinit', "#wizardSettingsPage", function () {

        var page = this;

        $('#btnNextPage', page).on('click', function () {

            save(page);
        });
    });

})(jQuery, document, window);
