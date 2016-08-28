define(['css!./toast'], function () {

    function remove(elem) {

        setTimeout(function () {
            elem.parentNode.removeChild(elem);
        }, 300);
    }

    function animateRemove(elem) {

        setTimeout(function () {

            elem.classList.remove('toastVisible');
            remove(elem);

        }, 3300);
    }

    return function (options) {

        if (typeof options === 'string') {
            options = {
                text: options
            };
        }

        var elem = document.createElement("div");
        elem.classList.add('toast');
        elem.innerHTML = options.text;

        document.body.appendChild(elem);

        setTimeout(function () {
            elem.classList.add('toastVisible');

            animateRemove(elem);

        }, 300);
    };
});