(function ($, document, window) {

    function loadPage(page, config) {


        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', ".libraryPathMappingForm", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    });

    window.LibraryPathMappingPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {


                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;

        }

    };

})(jQuery, document, window);
