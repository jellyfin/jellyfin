(function (window, store) {

    function getExternalPlayers() {
        return JSON.parse(store.getItem('externalplayers') || '[]');
    }

    function getUrl(player, item) {

        return 'vlc://http://www.google.com';

    }

    function getCodecLimits() {

        return {

            maxVideoAudioChannels: 6,
            maxAudioChannels: 2,
            maxVideoLevel: 50,
            maxWidth: 1920,
            maxHeight: 1080,
            maxSampleRate: 48000

        };
    }

    function canDirectStream(mediaType, mediaSource, maxBitrate) {

        // If bitrate is unknown don't direct stream
        if (!mediaSource.Bitrate || mediaSource.Bitrate > maxBitrate) {
            return false;
        }

        var codecLimits = getCodecLimits();

        if (mediaType == "Audio") {

            return true;
        }
        else if (mediaType == "Video") {

            var videoStream = mediaSource.MediaStreams.filter(function (s) {

                return s.Type == 'Video';

            })[0];

            if (!videoStream) {
                return false;
            }

            return mediaSource.Protocol == 'File';
        }

        throw new Error('Unrecognized MediaType');
    }

    function canPlayAudioStreamDirect(audioStream, isVideo) {

        var audioCodec = (audioStream.Codec || '').toLowerCase().replace('-', '');

        if (audioCodec.indexOf('aac') == -1 &&
            audioCodec.indexOf('mp3') == -1 &&
            audioCodec.indexOf('mpeg') == -1) {

            return false;
        }

        var codecLimits = getCodecLimits();

        var maxChannels = isVideo ? codecLimits.maxVideoAudioChannels : codecLimits.maxAudioChannels;

        if (!audioStream.Channels || audioStream.Channels > maxChannels) {
            return false;
        }

        if (!audioStream.SampleRate || audioStream.SampleRate > codecLimits.maxSampleRate) {
            return false;
        }

        var bitrate = audioStream.BitRate;
        if (!bitrate) {
            return false;
        }

        if (isVideo) {

            if (audioCodec.indexOf('aac') != -1 && bitrate > 768000) {
                return false;
            }
            if (audioCodec.indexOf('mp3') != -1 || audioCodec.indexOf('mpeg') != -1) {
                if (bitrate > 320000) {
                    return false;
                }
            }

        } else {
            if (bitrate > 320000) {
                return false;
            }
        }

        return true;
    }

    function isSupportedCodec(mediaType, mediaSource) {

        if (mediaType == "Audio") {
            return false;
        }
        else if (mediaType == "Video") {

            return mediaSource.MediaStreams.filter(function (m) {

                return m.Type == "Video" && (m.Codec || '').toLowerCase() == 'h264';

            }).length > 0;
        }

        throw new Error('Unrecognized MediaType');
    }

    function getStreamByIndex(streams, type, index) {
        return streams.filter(function (s) {

            return s.Type == type && s.Index == index;

        })[0];
    }

    function getMediaSourceInfo(item, maxBitrate, mediaSourceId, audioStreamIndex, subtitleStreamIndex) {

        var sources = item.MediaSources.filter(function (m) {

            m.audioStream = mediaSourceId == m.Id && audioStreamIndex != null ?
                getStreamByIndex(m.MediaStreams, 'Audio', audioStreamIndex) :
                getStreamByIndex(m.MediaStreams, 'Audio', m.DefaultAudioStreamIndex);

            if (item.MediaType == "Audio" && !m.audioStream) {
                m.audioStream = m.MediaStreams.filter(function (s) {
                    return s.Type == 'Audio';
                })[0];
            }

            m.subtitleStream = mediaSourceId == m.Id && subtitleStreamIndex != null ?
                getStreamByIndex(m.MediaStreams, 'Subtitle', subtitleStreamIndex) :
                getStreamByIndex(m.MediaStreams, 'Subtitle', m.DefaultSubtitleStreamIndex);

            return !mediaSourceId || m.Id == mediaSourceId;

        });

        // Find first one that can be direct streamed
        var source = sources.filter(function (m) {

            var audioStream = m.audioStream;

            if (!audioStream || !canPlayAudioStreamDirect(audioStream, item.MediaType == 'Video')) {
                return false;
            }

            if (m.subtitleStream) {
                return false;
            }

            return canDirectStream(item.MediaType, m, maxBitrate, audioStream);

        })[0];

        if (source) {
            return {
                mediaSource: source,
                isStatic: true,
                streamContainer: source.Container
            };
        }

        // Find first one with supported codec
        source = sources.filter(function (m) {

            return isSupportedCodec(item.MediaType, m);

        })[0];

        source = source || sources[0];

        var container = item.MediaType == 'Audio' ? 'mp3' : 'm3u8';

        // Default to first one
        return {
            mediaSource: source,
            isStatic: false,
            streamContainer: container
        };
    }

    function getStreamInfo(serverAddress, deviceId, item, startPositionTicks, maxBitrate, mediaSourceId, audioStreamIndex, subtitleStreamIndex) {

        var mediaSourceInfo = getMediaSourceInfo(item, maxBitrate, mediaSourceId, audioStreamIndex, subtitleStreamIndex);

        var url = getStreamUrl(serverAddress, deviceId, item.MediaType, item.Id, mediaSourceInfo, startPositionTicks, maxBitrate);

        if (mediaSourceInfo.subtitleStream) {
            url += "&SubtitleStreamIndex=" + mediaSourceInfo.Index;
        }

        mediaSourceInfo.url = url;

        return mediaSourceInfo;
    }

    function getStreamUrl(serverAddress, deviceId, mediaType, itemId, mediaSourceInfo, startPositionTicks, maxBitrate) {

        var url;

        var codecLimits = getCodecLimits();

        if (mediaType == 'Audio') {

            url = serverAddress + '/mediabrowser/audio/' + itemId + '/stream.' + mediaSourceInfo.streamContainer;

            url += '?mediasourceid=' + mediaSourceInfo.mediaSource.Id;

            if (mediaSourceInfo.isStatic) {
                url += '&static=true';

            } else {

                url += '&maxaudiochannels=' + codecLimits.maxAudioChannels;

                if (startPositionTicks) {
                    url += '&startTimeTicks=' + startPositionTicks.toString();
                }

                if (maxBitrate) {
                    url += '&audiobitrate=' + Math.min(maxBitrate, 320000).toString();
                }

                url += '&deviceId=' + deviceId;
            }

            return url;

        }
        else if (mediaType == 'Video') {

            if (mediaSourceInfo.isStatic) {
                url = serverAddress + '/mediabrowser/videos/' + itemId + '/stream.' + mediaSourceInfo.streamContainer + '?static=true';
            }
            else {
                url = serverAddress + '/mediabrowser/videos/' + itemId + '/stream.' + mediaSourceInfo.streamContainer + '?static=false';
            }

            url += '&maxaudiochannels=' + codecLimits.maxVideoAudioChannels;

            if (maxBitrate) {

                var audioRate = 320000;
                url += '&audiobitrate=' + audioRate.toString();
                url += '&videobitrate=' + (maxBitrate - audioRate).toString();
            }

            url += '&profile=high';
            url += '&level=41';

            url += '&maxwidth=' + codecLimits.maxWidth;
            url += '&maxheight=' + codecLimits.maxHeight;

            url += '&videoCodec=h264';
            url += '&audioCodec=aac';

            url += '&mediasourceid=' + mediaSourceInfo.mediaSource.Id;
            url += '&deviceId=' + deviceId;

            return url;
        }

        throw new Error('Unrecognized MediaType');
    }

    function getVideoUrl(item) {

        var maxBitrate = parseInt(store.getItem('preferredVideoBitrate') || '') || 1500000;

        var info = getStreamInfo(ApiClient.serverAddress(), ApiClient.deviceId(), item, null, maxBitrate);

        return info.url;
    }

    function getPlayerUrl(item, player) {

        return player.scheme.replace('{0}', getVideoUrl(item));
    }

    function showPostPlayMenu(item, userId) {

        $('.externalPlayerPostPlayFlyout').popup("close").remove();

        var html = '<div data-role="popup" class="externalPlayerPostPlayFlyout" data-history="false" data-theme="a" data-dismissible="false">';

        html += '<ul data-role="listview" style="min-width: 220px;">';
        html += '<li data-role="list-divider" style="padding: 1em;text-align:center;">' + Globalize.translate('HeaderExternalPlayerPlayback') + '</li>';
        html += '</ul>';

        html += '<div style="padding:1em;">';

        if (item.RunTimeTicks) {
            html += '<div>';
            html += '<label for="chkMarkWatched">' + Globalize.translate('OptionMarkWatched') + '</label>';
            html += '<input type="checkbox" id="chkMarkWatched" checked="checked" />';
            html += '<div class="fieldDescription">' + Globalize.translate('OptionMarkWatchedHelp') + '</div>';
            html += '</div>';

            html += '<br/>';
        }

        html += '<button type="button" class="btnDone" data-theme="b" data-icon="check">' + Globalize.translate('ButtonImDone') + '</button>';

        html += '</div>';

        html += '</div>';

        $(document.body).append(html);

        var elem = $('.externalPlayerPostPlayFlyout').popup({}).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        });

        $('.btnDone', elem).on('click', function () {

            $('.externalPlayerPostPlayFlyout').popup("close").remove();

            ApiClient.stopActiveEncodings();

            if ($('#chkMarkWatched', elem).checked()) {

                ApiClient.markPlayed(userId, item.Id, new Date());

            }
        });

    }

    function closePlayMenu() {
        $('.externalPlayerFlyout').popup("close").remove();
    }

    function showMenuForItem(item, userId) {

        closePlayMenu();

        var html = '<div data-role="popup" class="externalPlayerFlyout" data-history="false" data-theme="a">';

        html += '<ul data-role="listview" style="min-width: 200px;">';
        html += '<li data-role="list-divider" style="padding: 1em;">' + Globalize.translate('HeaderSelectExternalPlayer') + '</li>';
        html += '</ul>';

        html += '<div style="padding:1em;">';

        html += getExternalPlayers().map(function (p) {

            return '<a href="' + getPlayerUrl(item, p) + '" data-role="button" data-icon="play" class="btnExternalPlayer">' + p.name + '</a>';

        }).join('');

        html += '</div>';

        html += '</div>';

        $(document.body).append(html);

        var elem = $('.externalPlayerFlyout').popup({}).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        });

        $('.btnExternalPlayer', elem).on('click', function () {

            closePlayMenu();

            setTimeout(function () {

                showPostPlayMenu(item, userId);
            }, 500);
        });
    }

    function showPlayMenu(itemId) {

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getItem(userId, itemId).done(function (item) {

            setTimeout(function () {

                showMenuForItem(item, userId);
            }, 500);
        });
    }

    window.ExternalPlayer = {

        getUrl: getUrl,
        getExternalPlayers: getExternalPlayers,
        showMenu: showPlayMenu
    };

})(window, window.store);