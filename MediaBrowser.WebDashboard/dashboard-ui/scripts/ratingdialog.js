define(['jQuery'], function ($) {

    window.RatingDialog = function (page) {

        var self = this;

        self.show = function (options) {

            require(['jqmpopup'], function () {
                self.showInternal(options);
            });
        };

        self.showInternal = function (options) {

            options = options || {};

            options.header = options.header || Globalize.translate('HeaderRateAndReview');

            var html = '<div data-role="popup" id="popupRatingDialog" class="popup" style="min-width:400px;">';

            html += '<div class="ui-bar-a" style="text-align: center; padding: 0 20px;">';
            html += '<h3>' + options.header + '</h3>';
            html += '</div>';

            html += '<div style="padding: 1em;">';
            html += '<form>';

            html += '<div style="margin:0;">';
            html += '<label for="txtRatingDialogRating" >' + Globalize.translate('LabelYourRating') + '</label>';
            html += '<input id="txtRatingDialogRating" name="rating" type="number" required="required" min=0 max=5 step=1 value=' + options.rating + ' />';
            html += '<label for="txtRatingDialogTitle" >' + Globalize.translate('LabelShortRatingDescription') + '</label>';
            html += '<input id="txtRatingDialogTitle" name="title" type="text" maxlength=160 />';
            html += '<label for="txtRatingDialogRecommend" >' + Globalize.translate('OptionIRecommendThisItem') + '</label>';
            html += '<input id="txtRatingDialogRecommend" name="recommend" type="checkbox" checked />';
            html += '<label for="txtRatingDialogReview" >' + Globalize.translate('LabelFullReview') + '</label>';
            html += '<textarea id="txtRatingDialogReview" name="review" rows=8 style="height:inherit" ></textarea>';
            html += '</div>';


            html += '<p>';
            html += '<button type="submit" data-theme="b" data-icon="check">' + Globalize.translate('ButtonOk') + '</button>';
            html += '<button type="button" data-icon="delete" onclick="$(this).parents(\'.popup\').popup(\'close\');">' + Globalize.translate('ButtonCancel') + '</button>';
            html += '</p>';
            html += '<p id="errorMsg" style="display:none; color:red; font-weight:bold">';
            html += '</p>';
            html += '</form>';
            html += '</div>';
            html += '</div>';

            $(page).append(html);

            var popup = $('#popupRatingDialog').popup().trigger('create').on("popupafteropen", function () {

                $('#txtRatingDialogTitle', this).focus();

            }).popup("open").on("popupafterclose", function () {

                $('form', this).off("submit");

                $(this).off("popupafterclose").remove();

            });

            $('form', popup).on('submit', function () {

                if (options.callback) {
                    var review = {
                        id: options.id,
                        rating: $('#txtRatingDialogRating', this).val(),
                        title: $('#txtRatingDialogTitle', this).val(),
                        recommend: $('#txtRatingDialogRecommend', this).checked(),
                        review: $('#txtRatingDialogReview', this).val(),
                    };

                    options.callback(review);
                } else console.log("No callback function provided");

                return false;
            });

        };

        self.close = function () {
            $('#popupRatingDialog', page).popup("close");
        };
    };

    window.RatingHelpers = {

        ratePackage: function (link) {
            var id = link.getAttribute('data-id');
            var rating = link.getAttribute('data-rating');

            var dialog = new RatingDialog($.mobile.activePage);
            dialog.show({
                header: Globalize.translate('HeaderRateAndReview'),
                id: id,
                rating: rating,
                callback: function (review) {
                    console.log(review);
                    dialog.close();

                    ApiClient.createPackageReview(review).then(function () {
                        Dashboard.alert({
                            message: Globalize.translate('MessageThankYouForYourReview'),
                            title: Globalize.translate('HeaderThankYou')
                        });
                    });
                }
            });
        }
    };

});