define(['css!./emby-button'], function (layoutManager, browser) {

    var EmbyButtonPrototype = Object.create(HTMLButtonElement.prototype);

    function animateButton(e) {

        var div = document.createElement('div');

        div.classList.add('ripple-effect');

        var offsetX = e.offsetX || 0;
        var offsetY = e.offsetY || 0;

        if (offsetX > 0 && offsetY > 0) {
            div.style.left = offsetX + 'px';
            div.style.top = offsetY + 'px';
        }

        this.appendChild(div);

        setTimeout(function () {
            div.parentNode.removeChild(div);
        }, 2000);
    }

    function onKeyDown(e) {

        if (e.keyCode == 13) {
            animateButton.call(this, e);
        }
    }

    EmbyButtonPrototype.attachedCallback = function () {

        if (this.getAttribute('data-embybutton') == 'true') {
            return;
        }

        this.setAttribute('data-embybutton', 'true');

        this.addEventListener('keydown', onKeyDown);
        this.addEventListener('mousedown', animateButton);
        //this.addEventListener('click', animateButton);
    };

    document.registerElement('emby-button', {
        prototype: EmbyButtonPrototype,
        extends: 'button'
    });
});