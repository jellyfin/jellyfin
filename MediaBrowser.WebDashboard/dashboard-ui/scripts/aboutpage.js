(function ($, document, apiClient) {

    $(document).on('pageshow', "#aboutPage", function () {

        var page = this;
        
        apiClient.getSystemInfo().done(function(info) {
            $('#appVersionNumber', page).html(info.Version);
        });
    });

})(jQuery, document, ApiClient);