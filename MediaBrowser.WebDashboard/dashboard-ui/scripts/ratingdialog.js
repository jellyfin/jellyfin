(function (window, document, $) {

    window.RatingDialog = function (page) {

        var self = this;

        self.show = function (options) {

            options = options || {};

            options.header = options.header || "Rate and Review";
            
            var html = '<div data-role="popup" id="popupRatingDialog" class="ui-corner-all popup" style="min-width:400px;" data-dismissible="false">';

            html += '<div class="ui-corner-top ui-bar-a" style="text-align: center; padding: 0 20px;">';
            html += '<h3>' + options.header + '</h3>';
            html += '</div>';

            html += '<div data-role="content" class="ui-corner-bottom ui-content">';
            html += '<form>';

            html += '<div style="margin:0;">';
            html += '<label for="txtRatingDialogRating" >Your Rating:</label>';
            html += '<input id="txtRatingDialogRating" name="rating" type="number" required="required" min=0 max=5 step=1 value=' + options.rating + ' />';
            html += '<label for="txtRatingDialogTitle" >Short Overall Rating Description:</label>';
            html += '<input id="txtRatingDialogTitle" name="title" type="text" maxlength=160 />';
            html += '<label for="txtRatingDialogRecommend" >I recommend this item</label>';
            html += '<input id="txtRatingDialogRecommend" name="recommend" type="checkbox" checked />';
            html += '<label for="txtRatingDialogReview" >Full Review</label>';
            html += '<textarea id="txtRatingDialogReview" name="review" rows=8 style="height:inherit" ></textarea>';
            html += '</div>';


            html += '<p>';
            html += '<button type="submit" data-theme="b" data-icon="ok">OK</button>';
            html += '<button type="button" data-icon="delete" onclick="$(this).parents(\'.popup\').popup(\'close\');">Cancel</button>';
            html += '</p>';
            html += '<p id="errorMsg" style="display:none; color:red; font-weight:bold">';
            html += '</p>';
            html += '</form>';
            html += '</div>';
            html += '</div>';

            $(page).append(html);

            var popup = $('#popupRatingDialog').popup().trigger('create').on("popupafteropen", function() {

                $('#txtRatingDialogTitle', this).focus();

            }).popup("open").on("popupafterclose", function() {

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

                    if (review.rating < 3) {
                        if (!review.title) {
                            $('#errorMsg', this).html("Please give reason for low rating").show();
                            $('#txtRatingDialogTitle', this).focus();
                            return false;
                        }
                    }

                    if (!review.recommend) {
                        if (!review.title) {
                            $('#errorMsg', this).html("Please give reason for not recommending").show();
                            $('#txtRatingDialogTitle', this).focus();
                            return false;
                        }
                    }

                    options.callback(review);
                } else console.log("No callback function provided");

                return false;
            });

        };

        self.close = function () {
            $('#popupRatingDialog', page).popup("close");
        };
    };

})(window, document, jQuery);