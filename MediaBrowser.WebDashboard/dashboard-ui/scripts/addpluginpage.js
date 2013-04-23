var AddPluginPage = {

    onPageShow: function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var name = getParameterByName('name');

        var promise1 = ApiClient.getPackageInfo(name);
        var promise2 = ApiClient.getInstalledPlugins();
        var promise3 = ApiClient.getPluginSecurityInfo();

        $.when(promise1, promise2, promise3).done(function (response1, response2, response3) {

            AddPluginPage.renderPackage(response1[0], response2[0], response3[0], page);

        });
    },

    renderPackage: function (pkg, installedPlugins, pluginSecurityInfo, page) {

        var installedPlugin = installedPlugins.filter(function (ip) {
            return ip.Name == pkg.name;
        })[0];

        AddPluginPage.populateVersions(pkg, page, installedPlugin);
        AddPluginPage.populateHistory(pkg);

        Dashboard.setPageTitle(pkg.name);

	    if (pkg.targetSystem == 'Server') {
		    $("#btnInstallDiv", page).show();
		    $("#nonServerMsg", page).hide();
		    $("#pSelectVersion", page).show();
	    }else {
	        $("#btnInstallDiv", page).hide();
	        $("#pSelectVersion", page).hide();
		    $("#nonServerMsg", page).html("This plugin must be installed from "+pkg.targetSystem).show();
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
                    regStatus += "This feature has no registration information";
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
                if (pkg.price > 0) {
                    // Fill in PayPal info
                    $('.premiumHasPrice', page).show();
                    $('#featureId', page).val(pkg.featureId);
                    $('#featureName', page).val(pkg.name);
                    $('#amount', page).val(pkg.price);
                    $('#regPrice', page).html("<h2>Price: $" + pkg.price.toFixed(2) + " (USD)</h2>");
                    var url = "http://mb3admin.com/admin/service/user/getPayPalEmail?id=" + pkg.owner;
                    $.getJSON(url).done(function(dev) {
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
                $('#regInfo', page).html("You must be a <a href='supporter.html'>Media Browser Supporter</a> in order to gain access to this feature.").trigger('create');
                $('#ppButton', page).hide();
            }

        } else {
            $('.premiumPackage', page).hide();
        }

        if (pkg.richDescUrl) {
            $('#pViewWebsite', page).show();
            $('#pViewWebsite a', page)[0].href = pkg.richDescUrl;
        } else {
            $('#pViewWebsite', page).hide();
        }

        if (pkg.previewImage) {

            var color = pkg.tileColor || "#2572EB";
            var img = pkg.previewImage ? pkg.previewImage : pkg.thumbImage;
            $('#pPreviewImage', page).show().html("<img src='" + img + "' style='max-width: 100%;border-radius:10px;-moz-box-shadow: 0 0 20px 3px " + color + ";-webkit-box-shadow: 0 0 20px 3px " + color + ";box-shadow: 0 0 20px 3px " + color + ";' />");
        } else {
            $('#pPreviewImage', page).hide().html("");
        }

        if (installedPlugin) {
            $('#pCurrentVersion', page).show().html("You currently have version <strong>" + installedPlugin.Version + "</strong> installed.");

        } else {
            $('#pCurrentVersion', page).hide().html("");
        }

        Dashboard.hideLoadingMsg();
    },

    populateVersions: function (packageInfo, page, installedPlugin) {

        var html = '';

        for (var i = 0, length = packageInfo.versions.length; i < length; i++) {

            var version = packageInfo.versions[i];

            html += '<option value="' + version.versionStr + '|' + version.classification + '">' + version.versionStr + ' (' + version.classification + ')</option>';

        }

        var selectmenu = $('#selectVersion', page).html(html);

        var packageVersion;

        if (installedPlugin) {

            // Select the first available package with the same update class as the installed version
            packageVersion = packageInfo.versions.filter(function (current) {

                return current.classification == installedPlugin.UpdateClass;
            })[0];


        } else {
            $('#pCurrentVersion', page).hide().html("");
        }

        // If we don't have a package version to select, pick the first release build
        if (!packageVersion) {

            // Select the first available package with the same update class as the installed version
            packageVersion = packageInfo.versions.filter(function (current) {

                return current.classification == "Release";
            })[0];
        }

        // If we still don't have a package version to select, pick the first Beta build
        if (!packageVersion) {

            // Select the first available package with the same update class as the installed version
            packageVersion = packageInfo.versions.filter(function (current) {

                return current.classification == "Beta";
            })[0];
        }

        if (packageVersion) {
            var val = packageVersion.versionStr + '|' + packageVersion.classification;

            $('#selectVersion', page).val(val);
        }

        selectmenu.selectmenu('refresh');
    },

    populateHistory: function (packageInfo) {

        var html = '';

        for (var i = 0, length = Math.min(packageInfo.versions.length, 10) ; i < length; i++) {

            var version = packageInfo.versions[i];

            html += '<h2 style="margin:.5em 0;">' + version.versionStr + ' (' + version.classification + ')</h2>';

            html += '<div style="margin-bottom:1.5em;">' + version.description + '</div>';
        }

        $('#revisionHistory', $.mobile.activePage).html(html);
    },

    onSubmit: function () {

        Dashboard.showLoadingMsg();

        $('#btnInstall', $.mobile.activePage).button('disable');

        var name = getParameterByName('name');

        ApiClient.getInstalledPlugins().done(function (plugins) {

            var installedPlugin = plugins.filter(function (ip) {
                return ip.Name == name;
            })[0];

            var vals = $('#selectVersion', $.mobile.activePage).val().split('|');

            var version = vals[0];

            if (installedPlugin && installedPlugin.Version == version) {

                Dashboard.hideLoadingMsg();

                Dashboard.confirm("Are you sure you wish to reinstall the same version you already have? In most cases this will not have any effect.", "Plugin Reinstallation", function (confirmResult) {

                    if (confirmResult) {

                        Dashboard.showLoadingMsg();
                        AddPluginPage.performInstallation(name, vals[1], version);
                    } else {
                        $('#btnInstall', $.mobile.activePage).button('enable');
                    }

                });
            } else {
                AddPluginPage.performInstallation(name, vals[1], version);
            }
        });


        return false;
    },

    performInstallation: function (packageName, updateClass, version) {

        ApiClient.installPlugin(packageName, updateClass, version).done(function () {

            Dashboard.hideLoadingMsg();
        });
    }
};

$(document).on('pageshow', "#addPluginPage", AddPluginPage.onPageShow);