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
        // Get the latest positive and negative reviews
        var promise1 = ApiClient.getPackageReviews(id, 4, null, 3, true);
        var promise2 = ApiClient.getPackageReviews(id, null, 2, 3, true);

        $.when(promise1, promise2).done(function (response1, response2) {
            //positive
            var html = '<div data-role="collapsible" data-collapsed="true" style="margin-top: 2em;" >';
            html += '<h3>Latest Outstanding Reviews</h3>';

            var positive = response1[0];
            var hasReviews = false;
            if (positive && positive.length > 0) {
                for (var i = 0; i < positive.length; i++) {
                    var review = positive[i];
                    if (review.title) {
                        hasReviews = true;
                        html += "<div>";
                        html += "<span class='storeItemReviewText'>";
                        html += new Date(review.timestamp).toDateString();
                        html += " " + Dashboard.getStoreRatingHtml(review.rating, review.id, review.name, true);
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
                }
            }
            if (!hasReviews) {
                html += "<p>No Outstanding Reviews with additional information</p>";
            }

            html += "</div>";

            //negative
            html += '<div data-role="collapsible" data-collapsed="true" style="margin-top: 2em;" >';
            html += '<h3>Latest Negative Reviews</h3>';
            var negative = response2[0];
            hasReviews = false;
            if (negative && negative.length > 0) {
                for (var i = 0; i < negative.length; i++) {
                    review = negative[i];
                    if (review.title) {
                        hasReviews = true;
                        html += "<div>";
                        html += "<span class='storeItemReviewText'>";
                        html += new Date(review.timestamp).toDateString();
                        html += " " + Dashboard.getStoreRatingHtml(review.rating, review.id, review.name, true);
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
                }
            }

            if (!hasReviews) {
                html += "<p>No Negative Reviews with additional information</p>";
            }

            html += "</div>";


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

        Dashboard.setPageTitle(pkg.name);

        if (pkg.targetSystem == 'Server') {
            $("#btnInstallDiv", page).show();
            $("#nonServerMsg", page).hide();
            $("#pSelectVersion", page).show();
        } else {
            $("#btnInstallDiv", page).hide();
            $("#pSelectVersion", page).hide();
            var msg = "This plug-in must be installed from " + pkg.targetSystem;
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
                regStatus += "You are currently registered for this feature";
            } else {
                if (new Date(pkg.expDate).getTime() < new Date(1970, 1, 1).getTime()) {
                } else {
                    if (new Date(pkg.expDate).getTime() <= new Date().getTime()) {
                        regStatus += "The trial period for this feature has expired";
                    } else {
                        regStatus += "The trial period for this feature will expire in " + Math.round((new Date(pkg.expDate).getTime() - new Date().getTime()) / (86400000)) + " day(s)";
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
                    $('#regPrice', page).html("<h2>Price: $" + pkg.price.toFixed(2) + " (USD)</h2>");
                    var url = "http://mb3admin.com/admin/service/user/getPayPalEmail?id=" + pkg.owner;
                    $.getJSON(url).done(function (dev) {
                        if (dev.payPalEmail) {
                            $('#payPalEmail', page).val(dev.payPalEmail);

                        } else {
                            $('#ppButton', page).hide();
                            $('#noEmail', page).show();
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
        var ratingHtml = "<strong>Overall </strong>" + Dashboard.getStoreRatingHtml(pkg.avgRating, pkg.id, pkg.name);
        ratingHtml += "<span class='storeReviewCount'>";
        ratingHtml += " " + pkg.totalRatings + " Reviews";
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
            $('#pCurrentVersion', page).show().html("You currently have version <strong>" + installedPlugin.Version + "</strong> installed.");

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

                    Dashboard.confirm("Are you sure you wish to reinstall the same version you already have? In most cases this will not have any effect.", "Plugin Reinstallation", function (confirmResult) {

                        if (confirmResult) {

                            Dashboard.showLoadingMsg();
                            performInstallation(name, guid, vals[1], version);
                        } else {
                            $('#btnInstall', page).buttonEnabled(true);
                        }

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