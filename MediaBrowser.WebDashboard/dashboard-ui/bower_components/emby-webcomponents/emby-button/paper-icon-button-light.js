define(['browser', 'dom', 'css!./emby-button', 'registerElement'], function (browser, dom) {

    var EmbyButtonPrototype = Object.create(HTMLButtonElement.prototype);

    function enableAnimation() {
        if (browser.tv) {
            // too slow
            return false;
        }
        return true;
    }

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

        btn.appendChild(div);

        div.addEventListener("animationend", function () {
            div.parentNode.removeChild(div);
        }, false);
    }

    function animateButton(e) {

        var btn = this;
        requestAnimationFrame(function () {
            animateButtonInternal(e, btn);
        });
    }

    function onKeyDown(e) {

        if (e.keyCode == 13) {
            animateButton.call(this, e);
        }
    }

    EmbyButtonPrototype.createdCallback = function () {

        if (this.classList.contains('paper-icon-button-light')) {
            return;
        }

        this.classList.add('paper-icon-button-light');

        if (enableAnimation()) {
            dom.addEventListener(this, 'keydown', onKeyDown, {
                passive: true
            });
            dom.addEventListener(this, 'click', animateButton, {
                passive: true
            });
        }
    };

    document.registerElement('paper-icon-button-light', {
        prototype: EmbyButtonPrototype,
        extends: 'button'
    });
});