define(['browser', 'css!./emby-button'], function (browser) {

    var EmbyButtonPrototype = Object.create(HTMLButtonElement.prototype);

    function animateButton(e) {

        var btn = this;
        var div = document.createElement('div');

        div.classList.add('ripple-effect');

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

    function onKeyDown(e) {

        if (e.keyCode == 13) {
            animateButton.call(this, e);
        }
    }

    function onMouseDown(e) {

        if (e.button == 0) {
            animateButton.call(this, e);
        }
    }

    EmbyButtonPrototype.attachedCallback = function () {

        if (this.getAttribute('data-embybutton') == 'true') {
            return;
        }

        this.setAttribute('data-embybutton', 'true');

        this.addEventListener('keydown', onKeyDown);
        if (browser.safari) {
            this.addEventListener('click', animateButton);
        } else {
            this.addEventListener('mousedown', onMouseDown);
            //this.addEventListener('touchstart', animateButton);
        }
    };

    document.registerElement('emby-button', {
        prototype: EmbyButtonPrototype,
        extends: 'button'
    });
});