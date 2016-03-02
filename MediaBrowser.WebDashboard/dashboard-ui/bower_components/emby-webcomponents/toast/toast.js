define(['paper-toast'], function () {

    var toastId = 0;

    return function (options) {

        if (typeof options === 'string') {
            options = {
                text: options
            };
        }

        var elem = document.createElement("paper-toast");
        elem.setAttribute('text', options.text);
        elem.id = 'toast' + (toastId++);

        document.body.appendChild(elem);

        // This timeout is obviously messy but it's unclear how to determine when the webcomponent is ready for use
        // element onload never fires
        setTimeout(function () {
            elem.show();
        }, 300);
    };
});