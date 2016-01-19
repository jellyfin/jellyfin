define([], function () {

    function loadImage(elem, url) {

        if (elem.tagName !== "IMG") {

            var tmp = new Image();

            tmp.onload = function () {
                elem.style.backgroundImage = "url('" + url + "')";
            };
            tmp.src = url;

        } else {
            elem.setAttribute("src", url);
        }

        return Promise.resolve(elem);
    }

    return {
        loadImage: loadImage
    };

});