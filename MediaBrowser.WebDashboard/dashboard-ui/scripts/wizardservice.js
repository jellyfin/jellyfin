(function ($, document) {

    $(document).on('pageinitdepends', "#wizardServicePage", function () {

        var page = this;

        $('#btnNextPage', page).on('click', function () {

            Dashboard.navigate('wizardagreement.html');
        });
    });

})(jQuery, document, window);
