define(["browser", "css!./appfooter"], function(browser) {
    "use strict";

    function render(options) {
        var elem = document.createElement("div");
        return elem.classList.add("appfooter"), browser.chrome || elem.classList.add("appfooter-blurred"), document.body.appendChild(elem), elem
    }

    function appFooter(options) {
        var self = this;
        self.element = render(options), self.add = function(elem) {
            self.element.appendChild(elem)
        }, self.insert = function(elem) {
            "string" == typeof elem ? self.element.insertAdjacentHTML("afterbegin", elem) : self.element.insertBefore(elem, self.element.firstChild)
        }
    }
    return appFooter.prototype.destroy = function() {
        this.element = null
    }, appFooter
});