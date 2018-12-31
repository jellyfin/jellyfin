define(['globalize', 'connectionManager', 'require', 'loading', 'apphost', 'dom', 'recordingHelper', 'events', 'registrationServices', 'paper-icon-button-light', 'emby-button', 'css!./recordingfields'], function (globalize, connectionManager, require, loading, appHost, dom, recordingHelper, events, registrationServices) {
    'use strict';

    function onRecordingButtonClick(e) {

        var item = this.item;

        if (item) {

            var serverId = item.ServerId;
            var programId = item.Id;
            var timerId = item.TimerId;
            var timerStatus = item.Status;
            var seriesTimerId = item.SeriesTimerId;

            var instance = this;

            recordingHelper.toggleRecording(serverId, programId, timerId, timerStatus, seriesTimerId).then(function () {
                instance.refresh(serverId, programId);
            });
        }
    }

    function RecordingButton(options) {
        this.options = options;

        if (options.item) {
            this.refreshItem(options.item);
        } else if (options.itemId && options.serverId) {
            this.refresh(options.itemId, options.serverId);
        }
        var button = options.button;
        button.querySelector('i').innerHTML = '&#xE061;';

        var clickFn = onRecordingButtonClick.bind(this);
        this.clickFn = clickFn;

        dom.addEventListener(button, 'click', clickFn, {
            passive: true
        });
    }

    function getIndicatorIcon(item) {

        var status;

        if (item.Type === 'SeriesTimer') {
            return '&#xE062;';
        }
        else if (item.TimerId || item.SeriesTimerId) {

            status = item.Status || 'Cancelled';
        }
        else if (item.Type === 'Timer') {

            status = item.Status;
        }
        else {
            return '&#xE061;';
        }

        if (item.SeriesTimerId) {

            if (status !== 'Cancelled') {
                return '&#xE062;';
            }
        }

        return '&#xE061;';
    }

    RecordingButton.prototype.refresh = function (serverId, itemId) {

        var apiClient = connectionManager.getApiClient(serverId);
        var self = this;
        apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(function (item) {
            self.refreshItem(item);
        });
    };

    RecordingButton.prototype.refreshItem = function (item) {

        var options = this.options;
        var button = options.button;
        this.item = item;
        button.querySelector('i').innerHTML = getIndicatorIcon(item);

        if (item.TimerId && (item.Status || 'Cancelled') !== 'Cancelled') {
            button.classList.add('recordingIcon-active');
        } else {
            button.classList.remove('recordingIcon-active');
        }
    };

    RecordingButton.prototype.destroy = function () {

        var options = this.options;

        if (options) {
            var button = options.button;

            var clickFn = this.clickFn;

            if (clickFn) {
                dom.removeEventListener(button, 'click', clickFn, {
                    passive: true
                });
            }
        }

        this.options = null;
        this.item = null;
    };

    return RecordingButton;
});