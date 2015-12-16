(function (globalScope) {

    globalScope.Events = {

        on: function (obj, eventName, selector, fn) {

            jQuery(obj).on(eventName, selector, fn);
        },

        off: function (obj, eventName, selector, fn) {

            jQuery(obj).off(eventName, selector, fn);
        },

        trigger: function (obj, eventName, params) {
            jQuery(obj).trigger(eventName, params);
        }
    };

})(window);