define(['jQuery', 'registrationServices', 'emby-checkbox', 'emby-input', 'listViewStyle', 'paper-icon-button-light'], function ($, registrationServices) {

    return function (page, providerId, options) {

        var self = this;

        function getListingProvider(config, id) {

            if (config && id) {

                var result = config.ListingProviders.filter(function (i) {
                    return i.Id == id;
                })[0];

                if (result) {
                    return Promise.resolve(result);
                }

                return getListingProvider();
            }

            return ApiClient.getJSON(ApiClient.getUrl('LiveTv/ListingProviders/Default'));
        }

        function reload() {

            Dashboard.showLoadingMsg();

            ApiClient.getNamedConfiguration("livetv").then(function (config) {

                getListingProvider(config, providerId).then(function (info) {
                    page.querySelector('.txtPath').value = info.Path || '';
                    page.querySelector('.txtKids').value = (info.KidsCategories || []).join('|');
                    page.querySelector('.txtNews').value = (info.NewsCategories || []).join('|');
                    page.querySelector('.txtSports').value = (info.SportsCategories || []).join('|');
                    page.querySelector('.txtMovies').value = (info.MovieCategories || []).join('|');

                    page.querySelector('.chkAllTuners').checked = info.EnableAllTuners;

                    if (page.querySelector('.chkAllTuners').checked) {
                        page.querySelector('.selectTunersSection').classList.add('hide');
                    } else {
                        page.querySelector('.selectTunersSection').classList.remove('hide');
                    }

                    refreshTunerDevices(page, info, config.TunerHosts);
                    Dashboard.hideLoadingMsg();
                });
            });
        }

        function getCategories(txtInput) {

            var value = txtInput.value;

            return value ? value.split('|') : [];
        }

        function submitListingsForm() {

            Dashboard.showLoadingMsg();

            var id = providerId;

            ApiClient.getNamedConfiguration("livetv").then(function (config) {

                var info = config.ListingProviders.filter(function (i) {
                    return i.Id == id;
                })[0] || {};

                info.Type = 'xmltv';

                info.Path = page.querySelector('.txtPath').value;

                info.MovieCategories = getCategories(page.querySelector('.txtMovies'));
                info.KidsCategories = getCategories(page.querySelector('.txtKids'));
                info.NewsCategories = getCategories(page.querySelector('.txtNews'));
                info.SportsCategories = getCategories(page.querySelector('.txtSports'));

                info.EnableAllTuners = page.querySelector('.chkAllTuners').checked;
                info.EnabledTuners = info.EnableAllTuners ? [] : $('.chkTuner', page).get().filter(function (i) {
                    return i.checked;
                }).map(function (i) {
                    return i.getAttribute('data-id');
                });

                ApiClient.ajax({
                    type: "POST",
                    url: ApiClient.getUrl('LiveTv/ListingProviders', {
                        ValidateListings: true
                    }),
                    data: JSON.stringify(info),
                    contentType: "application/json"

                }).then(function (result) {

                    Dashboard.hideLoadingMsg();
                    if (options.showConfirmation !== false) {
                        Dashboard.processServerConfigurationUpdateResult();
                    }
                    Events.trigger(self, 'submitted');

                }, function () {
                    Dashboard.hideLoadingMsg();
                    Dashboard.alert({
                        message: Globalize.translate('ErrorAddingListingsToSchedulesDirect')
                    });
                });

            });
        }

        function getTunerName(providerId) {

            providerId = providerId.toLowerCase();

            switch (providerId) {

                case 'm3u':
                    return 'M3U Playlist';
                case 'hdhomerun':
                    return 'HDHomerun';
                case 'satip':
                    return 'DVB';
                default:
                    return 'Unknown';
            }
        }

        function refreshTunerDevices(page, providerInfo, devices) {

            var html = '';

            for (var i = 0, length = devices.length; i < length; i++) {

                var device = devices[i];

                html += '<div class="listItem">';

                var enabledTuners = providerInfo.EnableAllTuners || [];
                var isChecked = providerInfo.EnableAllTuners || enabledTuners.indexOf(device.Id) != -1;
                var checkedAttribute = isChecked ? ' checked' : '';
                html += '<label class="listItemCheckboxContainer"><input type="checkbox" is="emby-checkbox" class="chkTuner" data-id="' + device.Id + '" ' + checkedAttribute + '><span></span></label>';

                html += '<div class="listItemBody two-line">';
                html += '<div class="listItemBodyText">';
                html += device.FriendlyName || getTunerName(device.Type);
                html += '</div>';

                html += '<div class="listItemBodyText secondary">';
                html += device.Url;
                html += '</div>';
                html += '</div>';

                html += '</div>';
            }

            page.querySelector('.tunerList').innerHTML = html;
        }

        self.submit = function () {
            page.querySelector('.btnSubmitListings').click();
        };

        function onSelectPathClick(e) {
            var page = $(e.target).parents('.xmltvForm')[0];
            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    includeFiles: true,
                    callback: function (path) {

                        if (path) {
                            var txtPath = page.querySelector('.txtPath');
                            txtPath.value = path;
                            txtPath.focus();
                        }
                        picker.close();
                    }
                });
            });
        }

        self.init = function () {

            options = options || {};

            if (options.showCancelButton !== false) {
                page.querySelector('.btnCancel').classList.remove('hide');
            } else {
                page.querySelector('.btnCancel').classList.add('hide');
            }

            if (options.showSubmitButton !== false) {
                page.querySelector('.btnSubmitListings').classList.remove('hide');
            } else {
                page.querySelector('.btnSubmitListings').classList.add('hide');
            }

            page.querySelector('.premiereHelp').innerHTML = Globalize.translate('XmlTvPremiere', 24);

            $('form', page).on('submit', function () {
                submitListingsForm();
                return false;
            });

            page.querySelector('#btnSelectPath').addEventListener("click", onSelectPathClick);

            page.querySelector('.lnkPremiere').addEventListener('click', function (e) {
                registrationServices.showPremiereInfo();
                e.preventDefault();
            });

            page.querySelector('.chkAllTuners').addEventListener('change', function (e) {
                if (e.target.checked) {
                    page.querySelector('.selectTunersSection').classList.add('hide');
                } else {
                    page.querySelector('.selectTunersSection').classList.remove('hide');
                }
            });

            reload();
        };
    }
});