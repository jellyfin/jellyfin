define(['browser', 'dom', 'css!./emby-button', 'registerElement'], function (browser, dom) {
    'use strict';

    var EmbyButtonPrototype = Object.create(HTMLButtonElement.prototype);

    function animateButtonInternal(e, btn) {

        var div = document.createElement('div');

        for (var i = 0, length = btn.classList.length; i < length; i++) {
            div.classList.add(btn.classList[i] + '-ripple-effect');
        }

        var offsetX = e.offsetX || 0;
        var offsetY = e.offsetY || 0;

        if (offsetX > 0 && offsetY > 0) {
            div.style.left = offsetX + 'px';
            div.style.top = offsetY + 'px';
        }

        var firstChild = btn.firstChild;
        if (firstChild) {
            btn.insertBefore(div, btn.firstChild);
        } else {
            btn.appendChild(div);
        }

        div.addEventListener("animationend", function () {
            div.parentNode.removeChild(div);
        }, false);
    }

    function animateButton(e, btn) {

        requestAnimationFrame(function () {
            animateButtonInternal(e, btn);
        });
    }

    function onKeyDown(e) {

        if (e.keyCode === 13) {
            animateButton(e, this);
        }
    }

    function onMouseDown(e) {

        if (e.button === 0) {
            animateButton(e, this);
        }
    }

    function onClick(e) {

        animateButton(e, this);
    }

    function enableAnimation() {
        if (browser.tv) {
            // too slow
            return false;
        }
        return true;
    }

    EmbyButtonPrototype.createdCallback = function () {

        if (this.classList.contains('emby-button')) {
            return;
        }

        this.classList.add('emby-button');

        if (browser.safari || browser.firefox || browser.noFlex) {
            this.classList.add('emby-button-noflex');
        }

        if (enableAnimation()) {
            dom.addEventListener(this, 'keydown', onKeyDown, {
                passive: true
            });
            if (browser.safari) {
                dom.addEventListener(this, 'click', onClick, {
                    passive: true
                });
            } else {
                dom.addEventListener(this, 'mousedown', onMouseDown, {
                    passive: true
                });
                //this.addEventListener('touchstart', animateButton);
            }
        }
    };

    document.registerElement('emby-button', {
        prototype: EmbyButtonPrototype,
        extends: 'button'
    });
});