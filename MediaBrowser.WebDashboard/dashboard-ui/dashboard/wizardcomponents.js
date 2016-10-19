define([], function () {

    function goNext() {
        Dashboard.navigate('wizardagreement.html');
    }

    function loadDownloadInfo(view) {

        var instructions = '';

        ApiClient.getSystemInfo().then(function (systemInfo) {

            if (systemInfo.OperatingSystem == 'Windows') {
                view.querySelector('.fldSelectEncoderPathType').classList.add('hide');
            } else {
                view.querySelector('.fldSelectEncoderPathType').classList.remove('hide');
            }

            if (systemInfo.OperatingSystem == 'Windows' && systemInfo.SystemArchitecture != 'Arm') {

                view.querySelector('.suggestedLocation').innerHTML = Globalize.translate('FFmpegSuggestedDownload', '<a target="_blank" href="https://ffmpeg.zeranoe.com/builds">https://ffmpeg.zeranoe.com</a>');

                if (systemInfo.SystemArchitecture == 'X86') {
                    instructions = 'Download FFmpeg 32-Bit Static';
                }
                else if (systemInfo.SystemArchitecture == 'X64') {
                    instructions = 'Download FFmpeg 64-Bit Static';
                }

            } else if (systemInfo.OperatingSystem == 'Linux' && systemInfo.SystemArchitecture != 'Arm') {

                view.querySelector('.suggestedLocation').innerHTML = Globalize.translate('FFmpegSuggestedDownload', '<a target="_blank" href="http://johnvansickle.com/ffmpeg">http://johnvansickle.com/ffmpeg</a>');

                if (systemInfo.SystemArchitecture == 'X86') {
                    instructions = 'Download x86 build';
                }
                else if (systemInfo.SystemArchitecture == 'X64') {
                    instructions = 'Download x86_64 build';
                }

            } else if (systemInfo.OperatingSystem == 'Osx' && systemInfo.SystemArchitecture == 'X64') {

                view.querySelector('.suggestedLocation').innerHTML = Globalize.translate('FFmpegSuggestedDownload', '<a target="_blank" href="http://evermeet.cx/ffmpeg">http://evermeet.cx/ffmpeg</a>');
                instructions = 'Download both ffmpeg and ffprobe, and extract them to the same folder.';

            } else {
                view.querySelector('.suggestedLocation').innerHTML = Globalize.translate('FFmpegSuggestedDownload', '<a target="_blank" href="http://ffmpeg.org">https://ffmpeg.org/download.html</a>');
            }

            view.querySelector('.downloadInstructions').innerHTML = instructions;

            var selectEncoderPath = view.querySelector('#selectEncoderPath');
            selectEncoderPath.value = 'Custom';
            onSelectEncoderPathChange.call(selectEncoderPath);
        });
    }

    function onSaveEncodingPathFailure(response) {

        var msg = '';

        // This is a fallback that handles both 404 and 400 (no path entered)
        msg = Globalize.translate('FFmpegSavePathNotFound');

        require(['alert'], function (alert) {
            alert(msg);
        });
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function onSelectEncoderPathChange(e) {

        var page = parentWithClass(this, 'page');

        if (this.value == 'Custom') {
            page.querySelector('.fldEncoderPath').classList.remove('hide');
        } else {
            page.querySelector('.fldEncoderPath').classList.add('hide');
        }
    }

    return function (view, params) {

        view.querySelector('#btnSelectEncoderPath').addEventListener("click", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    includeFiles: true,
                    callback: function (path) {

                        if (path) {
                            view.querySelector('.txtEncoderPath').value = path;
                        }
                        picker.close();
                    }
                });
            });
        });

        view.querySelector('form').addEventListener('submit', function (e) {

            var form = this;

            ApiClient.ajax({
                url: ApiClient.getUrl('System/MediaEncoder/Path'),
                type: 'POST',
                data: {
                    Path: form.querySelector('.txtEncoderPath').value,
                    PathType: 'Custom'
                }
            }).then(goNext, onSaveEncodingPathFailure);

            e.preventDefault();
            return false;
        });

        view.querySelector('#selectEncoderPath').addEventListener('change', onSelectEncoderPathChange);

        view.addEventListener('viewbeforeshow', function (e) {

            loadDownloadInfo(view);
        });
    };
});