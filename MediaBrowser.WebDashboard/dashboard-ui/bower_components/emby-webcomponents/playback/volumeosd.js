define(['events', 'playbackManager', 'dom', 'browser', 'css!./iconosd', 'material-icons'], function (events, playbackManager, dom, browser) {
    'use strict';

    var currentPlayer;
    var osdElement;
    var iconElement;
    var progressElement;

    var enableAnimation;

    function getOsdElementHtml() {
        var html = '';

        html += '<i class="md-icon iconOsdIcon">&#xE050;</i>';

        html += '<div class="iconOsdProgressOuter"><div class="iconOsdProgressInner"></div></div>';

        return html;
    }

    function ensureOsdElement() {

        var elem = osdElement;
        if (!elem) {

            enableAnimation = browser.supportsCssAnimation();

            elem = document.createElement('div');
            elem.classList.add('hide');
            elem.classList.add('iconOsd');
            elem.classList.add('iconOsd-hidden');
            elem.classList.add('volumeOsd');
            elem.innerHTML = getOsdElementHtml();

            iconElement = elem.querySelector('i');
            progressElement = elem.querySelector('.iconOsdProgressInner');

            document.body.appendChild(elem);
            osdElement = elem;
        }
    }

    function onHideComplete() {
        this.classList.add('hide');
    }

    var hideTimeout;
    function showOsd() {

        clearHideTimeout();

        var elem = osdElement;

        dom.removeEventListener(elem, dom.whichTransitionEvent(), onHideComplete, {
            once: true
        });

        elem.classList.remove('hide');

        // trigger reflow
        void elem.offsetWidth;

        requestAnimationFrame(function () {
            elem.classList.remove('iconOsd-hidden');

            hideTimeout = setTimeout(hideOsd, 3000);
        });
    }

    function clearHideTimeout() {
        if (hideTimeout) {
            clearTimeout(hideTimeout);
            hideTimeout = null;
        }
    }

    function hideOsd() {

        clearHideTimeout();

        var elem = osdElement;
        if (elem) {

            if (enableAnimation) {
                // trigger reflow
                void elem.offsetWidth;

                requestAnimationFrame(function () {
                    elem.classList.add('iconOsd-hidden');

                    dom.addEventListener(elem, dom.whichTransitionEvent(), onHideComplete, {
                        once: true
                    });
                });
            } else {
                onHideComplete.call(elem);
            }
        }
    }

    function updatePlayerVolumeState(isMuted, volume) {

        if (iconElement) {
            iconElement.innerHTML = isMuted ? '&#xE04F;' : '&#xE050;';
        }
        if (progressElement) {
            progressElement.style.width = (volume || 0) + '%';
        }
    }

    function releaseCurrentPlayer() {

        var player = currentPlayer;

        if (player) {
            events.off(player, 'volumechange', onVolumeChanged);
            events.off(player, 'playbackstop', hideOsd);
            currentPlayer = null;
        }
    }

    function onVolumeChanged(e) {

        var player = this;

        ensureOsdElement();

        updatePlayerVolumeState(player.isMuted(), player.getVolume());

        showOsd();
    }

    function bindToPlayer(player) {

        if (player === currentPlayer) {
            return;
        }

        releaseCurrentPlayer();

        currentPlayer = player;

        if (!player) {
            return;
        }

        hideOsd();
        events.on(player, 'volumechange', onVolumeChanged);
        events.on(player, 'playbackstop', hideOsd);
    }

    events.on(playbackManager, 'playerchange', function () {
        bindToPlayer(playbackManager.getCurrentPlayer());
    });

    bindToPlayer(playbackManager.getCurrentPlayer());

});