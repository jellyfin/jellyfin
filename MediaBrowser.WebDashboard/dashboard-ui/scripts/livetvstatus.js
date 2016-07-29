define(['jQuery'], function ($) {

    function resetTuner(page, id) {

        var message = Globalize.translate('MessageConfirmResetTuner');

        require(['confirm'], function (confirm) {

            confirm(message, Globalize.translate('HeaderResetTuner')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.resetLiveTvTuner(id).then(function () {

                    Dashboard.hideLoadingMsg();

                    reload(page);
                });
            });
        });
    }

    function renderTuners(page, tuners) {

        var html = '';

        if (tuners.length) {
            html += '<div class="paperList">';

            for (var i = 0, length = tuners.length; i < length; i++) {

                var tuner = tuners[i];
                html += '<paper-icon-item>';

                html += '<paper-fab mini style="background:#52B54B;" icon="live-tv" item-icon></paper-fab>';

                html += '<paper-item-body two-line>';

                html += '<div>';
                html += tuner.Name;
                html += '</div>';

                html += '<div secondary>';
                html += tuner.SourceType;
                html += '</div>';

                html += '<div secondary>';
                if (tuner.Status == 'RecordingTv') {
                    if (tuner.ChannelName) {

                        html += '<a href="itemdetails.html?id=' + tuner.ChannelId + '">';
                        html += Globalize.translate('StatusRecordingProgram').replace('{0}', tuner.ChannelName);
                        html += '</a>';
                    } else {

                        html += Globalize.translate('StatusRecording');
                    }
                }
                else if (tuner.Status == 'LiveTv') {

                    if (tuner.ChannelName) {

                        html += '<a href="itemdetails.html?id=' + tuner.ChannelId + '">';
                        html += Globalize.translate('StatusWatchingProgram').replace('{0}', tuner.ChannelName);
                        html += '</a>';
                    } else {

                        html += Globalize.translate('StatusWatching');
                    }
                }
                else {
                    html += tuner.Status;
                }
                html += '</div>';

                html += '</paper-item-body>';

                if (tuner.CanReset) {
                    html += '<button type="button" is="paper-icon-button-light" data-tunerid="' + tuner.Id + '" title="' + Globalize.translate('ButtonResetTuner') + '" class="btnResetTuner"><iron-icon icon="refresh"></iron-icon></button>';
                }

                html += '</paper-icon-item>';
            }

            html += '</div>';
        }

        if (tuners.length) {
            page.querySelector('.tunerSection').classList.remove('hide');
        } else {
            page.querySelector('.tunerSection').classList.add('hide');
        }

        var elem = $('.tunerList', page).html(html);

        $('.btnResetTuner', elem).on('click', function () {

            var id = this.getAttribute('data-tunerid');

            resetTuner(page, id);
        });
    }

    function getServiceHtml(service) {

        var html = '';
        html += '<div>';

        var serviceUrl = service.HomePageUrl || '#';

        html += '<p><a href="' + serviceUrl + '" target="_blank">' + service.Name + '</a></p>';

        var versionHtml = service.Version || 'Unknown';

        if (service.HasUpdateAvailable) {
            versionHtml += ' <a style="margin-left: .25em;" href="' + serviceUrl + '" target="_blank">' + Globalize.translate('LiveTvUpdateAvailable') + '</a>';
        }
        else {
            versionHtml += '<img src="css/images/checkmarkgreen.png" style="height: 17px; margin-left: 10px; margin-right: 0; position: relative; top: 5px; border-radius:3px;" /> ' + Globalize.translate('LabelVersionUpToDate');
        }

        html += '<p>' + versionHtml + '</p>';

        var status = service.Status;

        if (service.Status == 'Ok') {

            status = '<span style="color:green;">' + status + '</span>';
        } else {

            if (service.StatusMessage) {
                status += ' (' + service.StatusMessage + ')';
            }

            status = '<span style="color:red;">' + status + '</span>';
        }

        html += '<p>' + Globalize.translate('ValueStatus', status) + '</p>';

        html += '</div>';

        return html;
    }

    function loadPage(page, liveTvInfo) {

        if (liveTvInfo.IsEnabled) {

            $('.liveTvStatusContent', page).show();

        } else {
            $('.liveTvStatusContent', page).hide();
        }

        var servicesToDisplay = liveTvInfo.Services.filter(function (s) {

            return s.IsVisible;

        });

        if (servicesToDisplay.length) {
            $('.servicesSection', page).show();
        } else {
            $('.servicesSection', page).hide();
        }

        $('.servicesList', page).html(servicesToDisplay.map(getServiceHtml).join(''));

        var tuners = [];
        for (var i = 0, length = liveTvInfo.Services.length; i < length; i++) {

            for (var j = 0, numTuners = liveTvInfo.Services[i].Tuners.length; j < numTuners; j++) {
                tuners.push(liveTvInfo.Services[i].Tuners[j]);
            }
        }

        renderTuners(page, tuners);

        ApiClient.getNamedConfiguration("livetv").then(function (config) {

            renderDevices(page, config.TunerHosts);
            renderProviders(page, config.ListingProviders);
        });

        Dashboard.hideLoadingMsg();
    }

    function renderDevices(page, devices) {

        var html = '';

        if (devices.length) {
            html += '<div class="paperList">';

            for (var i = 0, length = devices.length; i < length; i++) {

                var device = devices[i];

                var href = 'livetvtunerprovider-' + device.Type + '.html?id=' + device.Id;

                html += '<paper-icon-item>';

                html += '<paper-fab mini style="background:#52B54B;" icon="live-tv" item-icon></paper-fab>';

                html += '<paper-item-body two-line>';
                html += '<a class="clearLink" href="' + href + '">';
                html += '<div>';
                html += device.FriendlyName || getTunerName(device.Type);
                html += '</div>';

                html += '<div secondary>';
                html += device.Url;
                html += '</div>';
                html += '</a>';
                html += '</paper-item-body>';

                html += '<button type="button" is="paper-icon-button-light" class="btnDeleteDevice" data-id="' + device.Id + '" title="' + Globalize.translate('ButtonDelete') + '"><iron-icon icon="delete"></iron-icon></button>';
                html += '</paper-icon-item>';
            }

            html += '</div>';
        }

        var elem = $('.devicesList', page).html(html);

        $('.btnDeleteDevice', elem).on('click', function () {

            var id = this.getAttribute('data-id');

            deleteDevice(page, id);
        });
    }

    function deleteDevice(page, id) {

        var message = Globalize.translate('MessageConfirmDeleteTunerDevice');

        require(['confirm'], function (confirm) {

            confirm(message, Globalize.translate('HeaderDeleteDevice')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl('LiveTv/TunerHosts', {
                        Id: id
                    })

                }).then(function () {

                    reload(page);
                });
            });
        });
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvInfo().then(function (liveTvInfo) {

            loadPage(page, liveTvInfo);

        }, function () {

            loadPage(page, {
                Services: [],
                IsEnabled: true
            });
        });
    }

    function submitAddDeviceForm(page) {

        page.querySelector('.dlgAddDevice').close();
        Dashboard.showLoadingMsg();

        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl('LiveTv/TunerHosts'),
            data: JSON.stringify({
                Type: $('#selectTunerDeviceType', page).val(),
                Url: $('#txtDevicePath', page).val()
            }),
            contentType: "application/json"

        }).then(function () {

            reload(page);

        }, function () {
            Dashboard.alert({
                message: Globalize.translate('ErrorAddingTunerDevice')
            });
        });

    }

    function renderProviders(page, providers) {

        var html = '';

        if (providers.length) {
            html += '<div class="paperList">';

            for (var i = 0, length = providers.length; i < length; i++) {

                var provider = providers[i];
                html += '<paper-icon-item>';

                html += '<paper-fab mini style="background:#52B54B;" icon="dvr" item-icon></paper-fab>';

                html += '<paper-item-body two-line>';

                html += '<a class="clearLink" href="' + getProviderConfigurationUrl(provider.Type) + '&id=' + provider.Id + '">';

                html += '<div>';
                html += getProviderName(provider.Type);
                html += '</div>';

                html += '</a>';
                html += '</paper-item-body>';
                html += '<button type="button" is="paper-icon-button-light" class="btnOptions" data-id="' + provider.Id + '"><iron-icon icon="more-vert"></iron-icon></button>';
                html += '</paper-icon-item>';
            }

            html += '</div>';
        }

        var elem = $('.providerList', page).html(html);

        $('.btnOptions', elem).on('click', function () {

            var id = this.getAttribute('data-id');

            showProviderOptions(page, id, this);
        });
    }

    function showProviderOptions(page, providerId, button) {

        var items = [];

        items.push({
            name: Globalize.translate('ButtonDelete'),
            id: 'delete'
        });

        items.push({
            name: Globalize.translate('MapChannels'),
            id: 'map'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: items,
                positionTo: button

            }).then(function (id) {

                switch (id) {

                    case 'delete':
                        deleteProvider(page, providerId);
                        break;
                    case 'map':
                        mapChannels(page, providerId);
                        break;
                    default:
                        break;
                }
            });

        });
    }

    function mapChannels(page, providerId) {

        require(['components/channelmapper/channelmapper'], function (channelmapper) {
            new channelmapper({
                serverId: ApiClient.serverInfo().Id,
                providerId: providerId
            }).show();
        });
    }

    function deleteProvider(page, id) {

        var message = Globalize.translate('MessageConfirmDeleteGuideProvider');

        require(['confirm'], function (confirm) {

            confirm(message, Globalize.translate('HeaderDeleteProvider')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl('LiveTv/ListingProviders', {
                        Id: id
                    })

                }).then(function () {

                    reload(page);

                }, function () {

                    reload(page);
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

    function getProviderName(providerId) {

        providerId = providerId.toLowerCase();

        switch (providerId) {

            case 'schedulesdirect':
                return 'Schedules Direct';
            case 'xmltv':
                return 'Xml TV';
            case 'emby':
                return 'Emby Guide';
            default:
                return 'Unknown';
        }
    }

    function getProviderConfigurationUrl(providerId) {

        providerId = providerId.toLowerCase();

        switch (providerId) {

            case 'xmltv':
                return 'livetvguideprovider.html?type=xmltv';
            case 'schedulesdirect':
                return 'livetvguideprovider.html?type=schedulesdirect';
            case 'emby':
                return 'livetvguideprovider.html?type=emby';
            default:
                break;
        }
    }

    function addProvider(button) {

        var menuItems = [];

        menuItems.push({
            name: 'Schedules Direct',
            id: 'SchedulesDirect'
        });

        //menuItems.push({
        //    name: 'Emby Guide',
        //    id: 'emby'
        //});

        menuItems.push({
            name: 'Xml TV',
            id: 'xmltv'
        });

        menuItems.push({
            name: Globalize.translate('ButtonOther'),
            id: 'other'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: button,
                callback: function (id) {

                    if (id == 'other') {
                        Dashboard.alert({
                            message: Globalize.translate('ForAdditionalLiveTvOptions')
                        });
                    } else {
                        Dashboard.navigate(getProviderConfigurationUrl(id));
                    }
                }
            });

        });
    }

    function addDevice(button) {

        var menuItems = [];

        //menuItems.push({
        //    name: getTunerName('satip'),
        //    id: 'satip'
        //});

        menuItems.push({
            name: 'HDHomerun',
            id: 'hdhomerun'
        });

        menuItems.push({
            name: getTunerName('m3u'),
            id: 'm3u'
        });

        menuItems.push({
            name: Globalize.translate('ButtonOther'),
            id: 'other'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: button,
                callback: function (id) {

                    if (id == 'other') {
                        Dashboard.alert({
                            message: Globalize.translate('ForAdditionalLiveTvOptions')
                        });
                    } else {
                        Dashboard.navigate('livetvtunerprovider-' + id + '.html');
                    }
                }
            });

        });
    }

    function getTabs() {
        return [
        {
            href: 'livetvstatus.html',
            name: Globalize.translate('TabDevices')
        },
         {
             href: 'livetvsettings.html',
             name: Globalize.translate('TabSettings')
         },
         {
             href: 'appservices.html?context=livetv',
             name: Globalize.translate('TabServices')
         }];
    }

    $(document).on('pageinit', "#liveTvStatusPage", function () {

        var page = this;

        $('.btnAddDevice', page).on('click', function () {
            addDevice(this);
        });

        $('.formAddDevice', page).on('submit', function () {
            submitAddDeviceForm(page);
            return false;
        });

        $('.btnAddProvider', page).on('click', function () {
            addProvider(this);
        });

    }).on('pageshow', "#liveTvStatusPage", function () {

        LibraryMenu.setTabs('livetvadmin', 0, getTabs);
        var page = this;

        reload(page);

        // on here
        $('.btnRefresh', page).taskButton({
            mode: 'on',
            progressElem: page.querySelector('.refreshGuideProgress'),
            taskKey: 'RefreshGuide'
        });

    }).on('pagehide', "#liveTvStatusPage", function () {

        var page = this;

        // off here
        $('.btnRefreshGuide', page).taskButton({
            mode: 'off'
        });

    });

});
