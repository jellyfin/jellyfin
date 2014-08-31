(function ($, document, window) {

    function populateHistory(packageInfo, page) {

        var html = '';

        for (var i = 0, length = Math.min(packageInfo.versions.length, 10) ; i < length; i++) {

            var version = packageInfo.versions[i];

            html += '<h2 style="margin:.5em 0;">' + version.versionStr + ' (' + version.classification + ')</h2>';

            html += '<div style="margin-bottom:1.5em;">' + version.description + '</div>';
        }

        $('#revisionHistory', page).html(html);
    }

    function populateVersions(packageInfo, page, installedPlugin) {
        var html = '';

        for (var i = 0, length = packageInfo.versions.length; i < length; i++) {

            var version = packageInfo.versions[i];

            html += '<option value="' + version.versionStr + '|' + version.classification + '">' + version.versionStr + ' (' + version.classification + ')</option>';

        }

        var selectmenu = $('#selectVersion', page).html(html);

        if (!installedPlugin) {

            $('#pCurrentVersion', page).hide().html("");
        }

        var packageVersion = packageInfo.versions.filter(function (current) {

            return current.classification == "Release";
        })[0];

        // If we still don't have a package version to select, pick the first Beta build
        if (!packageVersion) {

            packageVersion = packageInfo.versions.filter(function (current) {

                return current.classification == "Beta";
            })[0];
        }

        selectmenu.selectmenu('refresh');

        if (packageVersion) {
            var val = packageVersion.versionStr + '|' + packageVersion.classification;

            selectmenu.val(val).selectmenu('refresh');
        }
    }

    function populateReviews(id, page) {

        ApiClient.getPackageReviews(id, null, null, 3).done(function (positive) {

            var html = '';

            if (positive && positive.length > 0) {

                html += '<div data-role="collapsible" data-collapsed="true" style="margin-top: 2em;" >';
                html += '<h3>' + Globalize.translate('HeaderLatestReviews') + '</h3>';

                html += "<div><br/>";

                for (var i = 0; i < positive.length; i++) {
                    var review = positive[i];

                    html += "<div>";
                    html += "<span class='storeItemReviewText'>";
                    html += new Date(review.timestamp).toDateString();
                    html += " " + RatingHelpers.getStoreRatingHtml(review.rating, review.id, review.name, true);
                    html += " " + review.title;
                    html += "</span>";
                    if (review.review) {
                        html += "<p class='storeItemReviewText'>";
                        html += review.review;
                        html += "</p>";
                    }

                    html += "</div>";
                    html += "<hr/>";
                }
                html += "</div>";
                html += "</div>";
            }

            $('#latestReviews', page).html(html).trigger('create');
        });
    }

    function renderPackage(pkg, installedPlugins, pluginSecurityInfo, page) {

        var installedPlugin = installedPlugins.filter(function (ip) {
            return ip.Name == pkg.name;
        })[0];

        populateVersions(pkg, page, installedPlugin);
        populateHistory(pkg, page);
        if (pkg.totalRatings > 0) populateReviews(pkg.id, page);

        $('.pluginName', page).html(pkg.name);

        if (pkg.targetSystem == 'Server') {
            $("#btnInstallDiv", page).show();
            $("#nonServerMsg", page).hide();
            $("#pSelectVersion", page).show();
        } else {
            $("#btnInstallDiv", page).hide();
            $("#pSelectVersion", page).hide();

            var msg = Globalize.translate('MessageInstallPluginFromApp');
            $("#nonServerMsg", page).html(msg).show();
        }

        if (pkg.shortDescription) {
            $('#tagline', page).show().html(pkg.shortDescription);
        } else {
            $('#tagline', page).hide();
        }

        $('#overview', page).html(pkg.overview || "");


        $('#developer', page).html(pkg.owner);

        if (pkg.isPremium) {
            $('.premiumPackage', page).show();

            // Fill in registration info
            var regStatus = "<strong>";
            if (pkg.isRegistered) {
            } else {

                if (new Date(pkg.expDate).getTime() < new Date(1970, 1, 1).getTime()) {
                } else {
                    if (new Date(pkg.expDate).getTime() <= new Date().getTime()) {
                        regStatus += Globalize.translate('MessageTrialExpired');
                    } else {

                        regStatus += Globalize.translate('MessageTrialWillExpireIn').replace('{0}', Math.round((new Date(pkg.expDate).getTime() - new Date().getTime()) / (86400000)));
                    }
                }
            }

            regStatus += "</strong>";
            $('#regStatus', page).html(regStatus);

            if (pluginSecurityInfo.IsMBSupporter) {
                $('#regInfo', page).html(pkg.regInfo || "");

                $('.premiumDescription', page).hide();
                $('.supporterDescription', page).hide();

                if (pkg.price > 0) {
                    // Fill in PayPal info
                    $('.premiumHasPrice', page).show();
                    $('#featureId', page).val(pkg.featureId);
                    $('#featureName', page).val(pkg.name);
                    $('#amount', page).val(pkg.price);

                    $('#regPrice', page).html("<h3>" + Globalize.translate('ValuePriceUSD').replace('{0}', "$" + pkg.price.toFixed(2)) + "</h3>");

                    var url = "http://mb3admin.com/admin/service/user/getPayPalEmail?id=" + pkg.owner;

                    $.getJSON(url).done(function (dev) {
                        if (dev.payPalEmail) {
                            $('#payPalEmail', page).val(dev.payPalEmail);

                        } else {
                            $('#ppButton', page).hide();
                        }
                    });
                } else {
                    // Supporter-only feature
                    $('.premiumHasPrice', page).hide();
                }
            } else {

                if (pkg.price) {
                    $('.premiumDescription', page).show();
                    $('.supporterDescription', page).hide();
                    $('#regInfo', page).html("");

                } else {
                    $('.premiumDescription', page).hide();
                    $('.supporterDescription', page).show();
                    $('#regInfo', page).html("");
                }

                $('#ppButton', page).hide();
            }

        } else {
            $('.premiumPackage', page).hide();
        }

        //Ratings and Reviews
        var ratingHtml = RatingHelpers.getStoreRatingHtml(pkg.avgRating, pkg.id, pkg.name);
        ratingHtml += "<span class='storeReviewCount'>";
        ratingHtml += " " + Globalize.translate('ValueReviewCount').replace('{0}', pkg.totalRatings);
        ratingHtml += "</span>";

        $('#ratingLine', page).html(ratingHtml);

        if (pkg.richDescUrl) {
            $('#pViewWebsite', page).show();
            $('#pViewWebsite a', page)[0].href = pkg.richDescUrl;
        } else {
            $('#pViewWebsite', page).hide();
        }

        if (pkg.previewImage || pkg.thumbImage) {

            var color = pkg.tileColor || "#38c";
            var img = pkg.previewImage ? pkg.previewImage : pkg.thumbImage;
            $('#pPreviewImage', page).show().html("<img src='" + img + "' style='max-width: 100%;-moz-box-shadow: 0 0 20px 3px " + color + ";-webkit-box-shadow: 0 0 20px 3px " + color + ";box-shadow: 0 0 20px 3px " + color + ";' />");
        } else {
            $('#pPreviewImage', page).hide().html("");
        }

        if (installedPlugin) {

            var currentVersionText = Globalize.translate('MessageYouHaveVersionInstalled').replace('{0}', '<strong>' + installedPlugin.Version + '</strong>');
            $('#pCurrentVersion', page).show().html(currentVersionText);

        } else {
            $('#pCurrentVersion', page).hide().html("");
        }

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#addPluginPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var name = getParameterByName('name');
        var guid = getParameterByName('guid');

        var promise1 = ApiClient.getPackageInfo(name, guid);
        var promise2 = ApiClient.getInstalledPlugins();
        var promise3 = ApiClient.getPluginSecurityInfo();

        $.when(promise1, promise2, promise3).done(function (response1, response2, response3) {

            renderPackage(response1[0], response2[0], response3[0], page);

        });

    });

    function performInstallation(packageName, guid, updateClass, version) {

        ApiClient.installPlugin(packageName, guid, updateClass, version).done(function () {

            Dashboard.hideLoadingMsg();
        });
    }

    function addPluginpage() {

        var self = this;

        self.onSubmit = function () {

            Dashboard.showLoadingMsg();

            var page = $(this).parents('#addPluginPage');

            $('#btnInstall', page).buttonEnabled(false);

            var name = getParameterByName('name');
            var guid = getParameterByName('guid');

            ApiClient.getInstalledPlugins().done(function (plugins) {

                var installedPlugin = plugins.filter(function (ip) {
                    return ip.Name == name;
                })[0];

                var vals = $('#selectVersion', page).val().split('|');

                var version = vals[0];

                if (installedPlugin && installedPlugin.Version == version) {

                    Dashboard.hideLoadingMsg();

                    Dashboard.alert({
                        message: Globalize.translate('MessageAlreadyInstalled'),
                        title: Globalize.translate('HeaderPluginInstallation')
                    });
                } else {
                    performInstallation(name, guid, vals[1], version);
                }
            });

            return false;
        };
    }

    window.AddPluginPage = new addPluginpage();

})(jQuery, document, window);