define(['jQuery'], function ($) {

    function showSignIn(page) {

        $('.kidsOptionsLogin', page).fadeIn();

        $('#txtPinCode', page).val('');
        $('.btnOptions', page).hide();
        $('.kidContent', page).hide();
    }

    function validatePin(page) {

    }

    function loadContent(page) {

        var options = {

            SortBy: "Random",
            SortOrder: "Descending",
            MediaTypes: "Video",
            Limit: 100,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            ExcludeLocationTypes: "Virtual",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Thumb"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            $('.itemsContainer', page).html(LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "horizontalBackdrop",
                preferThumb: true,
                overlayText: true,
                lazy: true,
                defaultAction: 'play',
                coverImage: true,
                enableImageEnhancers: false

            })).lazyChildren();

        });

    }

    function onPinSubmit() {
        var page = $(this).parents('.page');

        if (validatePin(page)) {

            $('.kidsOptionsLogin', page).hide();
            $('.kidsOptions', page).fadeIn();

        } else {
            Dashboard.alert({
                message: 'Invalid pin code entered. Please try again.',
                title: 'Input Error'
            });
        }

        return false;
    }

    function onOptionsSubmit() {
        var page = $(this).parents('.page');

        $('.kidsOptions', page).fadeOut();
        $('.btnOptions', page).show();
        $('.kidContent', page).show();

        return false;
    }

    $(document).on('pageshow', "#kidsPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        $('.kidContent', page).hide();

        $('.kidsWelcome', page).show();
        $('.lnkPinCode', page).attr('href', 'myprofile.html?userId=' + userId);
        $('.btnOptions', page).buttonEnabled(false);
        $('.kidsOptionsLogin', page).hide();
        $('.kidsOptions', page).hide();

        $('.kidsBackdropContainer').css('background-image', 'url(css/images/kids/bg.jpg)');

    }).on('pageinit', "#kidsPage", function () {

        var page = this;

        $('.btnDismissWelcome', page).on('click', function () {

            $('.kidsWelcome', page).fadeOut();
            $('.btnOptions', page).buttonEnabled(true);
            $('.kidContent', page).show();
            loadContent(page);

        });

        $('.btnOptions', page).on('click', function () {

            showSignIn(page);
        });

        $('.btnCancelPin', page).on('click', function () {

            $('.kidsOptionsLogin', page).fadeOut();
            $('.btnOptions', page).show();
            $('.kidContent', page).show();
        });

        $('.kidPinForm').off('submit', onPinSubmit).on('submit', onPinSubmit);
        $('.kidsOptionsForm').off('submit', onOptionsSubmit).on('submit', onOptionsSubmit);
    });

});
