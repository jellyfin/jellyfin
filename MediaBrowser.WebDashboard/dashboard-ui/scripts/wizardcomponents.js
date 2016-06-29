define([], function () {

    function goNext() {
        require(['scripts/wizardcontroller'], function (wizardcontroller) {
            wizardcontroller.navigateToService();
        });
    }

    function loadDownloadInfo(view) {

        var instructions = '';

        ApiClient.getSystemInfo().then(function (systemInfo) {

            if (systemInfo.OperatingSystem == 'Windows' && systemInfo.SystemArchitecture != 'Arm') {
                view.querySelector('.suggestedLocation').innerHTML = Globalize.translate('FFmpegSuggestedDownload', '<a target="_blank" href="https://ffmpeg.zeranoe.com/builds">https://ffmpeg.zeranoe.com</a>');

                if (systemInfo.SystemArchitecture == 'X86') {
                    instructions = 'Download 32-Bit Static';
                }
                else if (systemInfo.SystemArchitecture == 'X64') {
                    instructions = 'Download 64-Bit Static';
                }

                view.querySelector('.downloadInstructions').innerHTML = instructions;

            } else if (systemInfo.OperatingSystem == 'Linux' && systemInfo.SystemArchitecture != 'Arm') {

                view.querySelector('.suggestedLocation').innerHTML = Globalize.translate('FFmpegSuggestedDownload', '<a target="_blank" href="http://johnvansickle.com/ffmpeg">http://johnvansickle.com/ffmpeg</a>');
                if (systemInfo.SystemArchitecture == 'X86') {
                    instructions = 'Download x86 build';
                }
                else if (systemInfo.SystemArchitecture == 'X64') {
                    instructions = 'Download x86_64 build';
                }
                view.querySelector('.downloadInstructions').innerHTML = instructions;

            } else if (systemInfo.OperatingSystem == 'Osx' && systemInfo.SystemArchitecture == 'X64') {

                view.querySelector('.suggestedLocation').innerHTML = Globalize.translate('FFmpegSuggestedDownload', '<a target="_blank" href="http://evermeet.cx/ffmpeg">http://evermeet.cx/ffmpeg</a>');
                instructions = 'Download both ffmpeg and ffprobe, and extract them to the same folder.';
                view.querySelector('.downloadInstructions').innerHTML = instructions;

            } else {
                view.querySelector('.suggestedLocation').innerHTML = Globalize.translate('FFmpegSuggestedDownload', '<a target="_blank" href="http://ffmpeg.org">https://ffmpeg.org/download.html</a>');
                view.querySelector('.downloadInstructions').innerHTML = '';
            }
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
                    Path: form.querySelector('.txtEncoderPath').value
                }
            }).then(goNext, onSaveEncodingPathFailure);

            e.preventDefault();
            return false;
        });


        view.addEventListener('viewbeforeshow', function (e) {

            loadDownloadInfo(view);
        });
    };
});