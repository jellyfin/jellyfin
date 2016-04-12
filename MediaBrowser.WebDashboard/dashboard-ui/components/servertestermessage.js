define(['appSettings'], function (appSettings) {

    function getReleases() {

        return fetch('https://api.github.com/repos/MediaBrowser/Emby/releases', {
            method: 'GET'

        }).then(function (response) {

            return response.json();

        }, function () {

            return [];

        }).then(function (releases) {

            var result = {};
            for (var i = 0, length = releases.length; i < length; i++) {

                var release = releases[i];
                if (release.prerelease) {
                    if (!result.beta && release.target_commitish == 'beta') {
                        result.beta = release;
                    }
                    if (!result.dev && release.target_commitish == 'dev') {
                        result.dev = release;
                    }
                }

                if (result.beta && result.dev) {
                    break;
                }
            }

            return result;

        });
    }

    function replaceAll(str, find, replace) {

        return str.split(find).join(replace);
    }

    function showInternal() {

        getReleases().then(function (releases) {

            require(['dialogHelper'], function (dialogHelper) {
                var dlg = dialogHelper.createDialog({
                    size: 'small',
                    removeOnClose: true,
                    autoFocus: false
                });

                dlg.classList.add('ui-body-b');
                dlg.classList.add('background-theme-b');

                var html = '';

                html += '<div class="dialogHeader">';
                html += '<paper-icon-button icon="arrow-back" class="btnCancel" tabindex="-1"></paper-icon-button>';
                html += '<div class="dialogHeaderTitle">';
                html += 'Emby';
                html += '</div>';
                html += '</div>';

                html += '<h1>Welcome Emby Tester!</h1>';

                html += '<p>If you\'re seeing this message, it\s because you\'re running a pre-release version of Emby Server. Thank you for being a part of the Emby pre-release testing process.</p>';

                html += '<p>Please take a moment to leave your testing feedback about this version in the <a target="_blank" href="https://emby.media/community">Emby Community.</a></p>';

                html += '<a target="_blank" href="https://emby.media/community" class="clearLink" style="display:block;"><paper-button raised class="accent block">Visit Emby Community</paper-button></a>';

                if (releases.beta) {
                    html += '<h1 style="margin-bottom:0;margin-top:1.5em;">Beta Release Notes</h1>';

                    html += '<div style="margin-top:0;">';
                    html += replaceAll((releases.beta.body || ''), '*', '<br/>');
                    html += '</div>';
                }

                if (releases.dev) {
                    html += '<h1 style="margin-bottom:0;margin-top:1.5em;">Dev Release Notes</h1>';

                    html += '<div style="margin-top:0;">';
                    html += replaceAll((releases.dev.body || ''), '*', '<br/>');
                    html += '</div>';
                }

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                dialogHelper.open(dlg);

                dlg.querySelector('.btnCancel', dlg).addEventListener('click', function () {

                    dialogHelper.close(dlg);
                });
            });
        });
    }

    function compareVersions(a, b) {

        // -1 a is smaller
        // 1 a is larger
        // 0 equal
        a = a.split('.');
        b = b.split('.');

        for (var i = 0, length = Math.max(a.length, b.length) ; i < length; i++) {
            var aVal = parseInt(a[i] || '0');
            var bVal = parseInt(b[i] || '0');

            if (aVal < bVal) {
                return -1;
            }

            if (aVal > bVal) {
                return 1;
            }
        }

        return 0;
    }

    function show(apiClient) {

        var key = 'servertestermessagetime';
        var lastShown = parseInt(appSettings.get(key) || '0');

        if ((new Date().getTime() - lastShown) < 259200000) {
            return;
        }

        appSettings.set(key, new Date().getTime());

        if (!lastShown) {
            // don't show the first time
            return;
        }

        apiClient.getPublicSystemInfo().then(function (info) {

            if (compareVersions(info.Version, '3.0.5913') == 1) {
                showInternal();
            }
        });
    }

    return {
        show: show
    };
});