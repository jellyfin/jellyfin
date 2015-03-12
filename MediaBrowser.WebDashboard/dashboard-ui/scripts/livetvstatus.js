(function ($, document, window) {

    function resetTuner(page, id) {

        var message = Globalize.translate('MessageConfirmResetTuner');

        Dashboard.confirm(message, Globalize.translate('HeaderResetTuner'), function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                ApiClient.resetLiveTvTuner(id).done(function () {

                    Dashboard.hideLoadingMsg();

                    reload(page);
                });
            }
        });
    }

    function renderTuners(page, tuners) {

        var html = '';

        for (var i = 0, length = tuners.length; i < length; i++) {

            var tuner = tuners[i];

            html += '<tr>';

            html += '<td>';
            html += tuner.Name;
            html += '</td>';

            html += '<td>';
            html += tuner.SourceType;
            html += '</td>';

            html += '<td>';

            if (tuner.Status == 'RecordingTv') {
                if (tuner.ChannelName) {

                    html += '<a href="livetvchannel.html?id=' + tuner.ChannelId + '">';
                    html += Globalize.translate('StatusRecordingProgram').replace('{0}', tuner.ChannelName);
                    html += '</a>';
                } else {

                    html += Globalize.translate('StatusRecording');
                }
            }
            else if (tuner.Status == 'LiveTv') {

                if (tuner.ChannelName) {

                    html += '<a href="livetvchannel.html?id=' + tuner.ChannelId + '">';
                    html += Globalize.translate('StatusWatchingProgram').replace('{0}', tuner.ChannelName);
                    html += '</a>';
                } else {

                    html += Globalize.translate('StatusWatching');
                }
            }
            else {
                html += tuner.Status;
            }
            html += '</td>';

            html += '<td>';

            if (tuner.ProgramName) {
                html += tuner.ProgramName;
            }

            html += '</td>';

            html += '<td>';
            html += tuner.Clients.join('<br/>');
            html += '</td>';

            html += '<td>';
            html += '<button data-tunerid="' + tuner.Id + '" type="button" data-inline="true" data-icon="refresh" data-mini="true" data-iconpos="notext" class="btnResetTuner organizerButton" title="' + Globalize.translate('ButtonResetTuner') + '">' + Globalize.translate('ButtonResetTuner') + '</button>';
            html += '</td>';

            html += '</tr>';
        }

        var elem = $('.tunersResultBody', page).html(html).parents('.tblTuners').table("refresh").trigger('create');

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
            $('.noLiveTvServices', page).hide();

        } else {
            $('.liveTvStatusContent', page).hide();
            $('.noLiveTvServices', page).show();
        }

        $('.servicesList', page).html(liveTvInfo.Services.map(getServiceHtml).join('')).trigger('create');

        var tuners = [];
        for (var i = 0, length = liveTvInfo.Services.length; i < length; i++) {

            for (var j = 0, numTuners = liveTvInfo.Services[i].Tuners.length; j < numTuners; j++) {
                tuners.push(liveTvInfo.Services[i].Tuners[j]);
            }
        }

        renderTuners(page, tuners);

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvInfo().done(function (liveTvInfo) {

            loadPage(page, liveTvInfo);

        });
    }

    $(document).on('pageshow', "#liveTvStatusPage", function () {

        var page = this;

        reload(page);

        // on here
        $('.btnRefreshGuide', page).taskButton({
            mode: 'on',
            progressElem: $('.refreshGuideProgress', page),
            lastResultElem: $('.lastRefreshGuideResult', page),
            taskKey: 'RefreshGuide'
        });

    }).on('pagehide', "#liveTvStatusPage", function () {

        var page = this;

        // off here
        $('.btnRefreshGuide', page).taskButton({
            mode: 'off'
        });

    });

})(jQuery, document, window);
