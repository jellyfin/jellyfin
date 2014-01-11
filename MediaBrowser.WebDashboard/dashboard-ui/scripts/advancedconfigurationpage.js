(function ($, document, window) {

    function loadPage(page, config, systemInfo) {

        var os = systemInfo.OperatingSystem.toLowerCase();

        if (os.indexOf('windows') != -1) {
            $('#windowsStartupDescription', page).show();
        } else {
            $('#windowsStartupDescription', page).hide();
        }
        
        if (systemInfo.SupportsAutoRunAtStartup) {
            $('#fldRunAtStartup', page).show();
        } else {
            $('#fldRunAtStartup', page).hide();
        }

        $('#selectAutomaticUpdateLevel', page).val(config.SystemUpdateLevel).selectmenu('refresh').trigger('change');
        $('#chkDebugLog', page).checked(config.EnableDebugLevelLogging).checkboxradio("refresh");

        $('#chkRunAtStartup', page).checked(config.RunAtStartup).checkboxradio("refresh");


        $('#txtCachePath', page).val(config.CachePath || '');

        var customCachePath = config.CachePath ? true : false;
        $('#chkEnableCustomCachePath', page).checked(customCachePath).checkboxradio("refresh");

        if (customCachePath) {
            $('#fldEnterCachePath', page).show();
            $('#txtCachePath', page).attr("required", "required");
        } else {
            $('#fldEnterCachePath', page).hide();
            $('#txtCachePath', page).removeAttr("required");
        }

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#advancedConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();

        var promise2 = ApiClient.getSystemInfo();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

        $('#btnSelectCachePath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtCachePath', page).val(path);
                    }
                    picker.close();
                },

                header: "Select Server Cache Path",

                instruction: "Browse or enter the path to use for Media Browser Server cache. The folder must be writeable. The location of this folder will directly impact server performance and should ideally be placed on a solid state drive."
            });
        });

        $('#chkEnableCustomCachePath', page).on("change.showCachePathText", function () {

            if (this.checked) {
                $('#fldEnterCachePath', page).show();
                $('#txtCachePath', page).attr("required", "required");
            } else {
                $('#fldEnterCachePath', page).hide();
                $('#txtCachePath', page).removeAttr("required");
            }

        });

    }).on('pagehide', "#advancedConfigurationPage", function () {

        var page = this;

        $('#chkEnableCustomCachePath', page).off("change.showCachePathText");
        $('#btnSelectCachePath', page).off("click.selectDirectory");

    }).on('pageinit', "#advancedConfigurationPage", function () {

        var page = this;

        $('#selectAutomaticUpdateLevel', page).on('change', function () {

            if (this.value == "Dev") {
                $('#devBuildWarning', page).show();
            } else {
                $('#devBuildWarning', page).hide();
            }

        });

    });

    function advancedConfigurationPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                if ($('#chkEnableCustomCachePath', form).checked()) {
                    config.CachePath = $('#txtCachePath', form).val();
                } else {
                    config.CachePath = '';
                }

                config.EnableDebugLevelLogging = $('#chkDebugLog', form).checked();

                config.RunAtStartup = $('#chkRunAtStartup', form).checked();
                config.SystemUpdateLevel = $('#selectAutomaticUpdateLevel', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.AdvancedConfigurationPage = new advancedConfigurationPage();

})(jQuery, document, window);
