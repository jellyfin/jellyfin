define([], function () {

    function loadImage(elem, url) {

        if (elem.tagName !== "IMG") {

            elem.style.backgroundImage = "url('" + url + "')";
            return Promise.resolve(elem);

        } else {
            elem.setAttribute("src", url);
            return Promise.resolve(elem);
        }
    }

    return {
        loadImage: loadImage
    };

});