(function ($, document, window) {

    function loadProfile(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');
        var url = id ? 'Dlna/Profiles/' + id :
            'Dlna/Profiles/Default';

        $.getJSON(ApiClient.getUrl(url)).done(function (result) {

            renderProfile(page, result);

            Dashboard.hideLoadingMsg();
        });
    }
    
    function renderProfile(page, profile) {
        
    }

    $(document).on('pageshow', "#dlnaProfilePage", function () {

        var page = this;

        loadProfile(page);

    });

    window.DlnaProfilePage = {        
      
        onSubmit: function() {

            return false;
        }
    };

})(jQuery, document, window);
