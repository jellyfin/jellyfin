(function ($, window, document) {

    function get(key, userId) {

        return localStorage.getItem(key + '-' + userId);
    }

    function set(key, userId, value) {

        localStorage.setItem(key + '-' + userId, value);
    }

    function localSettings() {

        var self = this;

        self.val = function (key, userId, value) {
            
            if (arguments.length < 3) {

                return get(key, userId);
            }
            
            set(key, userId, value);
        };
    }

    window.LocalSettings = new localSettings();

})(jQuery, window, document);