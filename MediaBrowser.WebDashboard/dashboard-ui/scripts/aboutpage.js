(function ($, document) {

    $(document).on('pageshow', "#aboutPage", function () {

        var page = this;
        
        ApiClient.getSystemInfo().done(function(info) {
            $('#appVersionNumber', page).html(info.Version);
        });
    });

})(jQuery, document);