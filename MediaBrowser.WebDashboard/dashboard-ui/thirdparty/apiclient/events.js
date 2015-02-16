(function (globalScope) {

    globalScope.Events = {

        on: function (obj, eventName, fn) {

            Logger.log('event.on ' + eventName);
            jQuery(obj).on(eventName, fn);
        },

        off: function (obj, eventName, fn) {

            Logger.log('event.off ' + eventName);
            jQuery(obj).off(eventName, fn);
        },

        trigger: function (obj, eventName, params) {
            Logger.log('event.trigger ' + eventName);
            jQuery(obj).trigger('eventName', params);
        }
    };

})(window); 