(function (globalScope) {

    globalScope.Events = {

        on: function (obj, eventName, selector, fn) {

            Logger.log('event.on ' + eventName);
            bean.on(obj, eventName, selector, fn);
        },

        off: function (obj, eventName, selector, fn) {

            Logger.log('event.off ' + eventName);
            bean.off(obj, eventName, selector);
        },

        trigger: function (obj, eventName, params) {

            Logger.log('event.trigger ' + eventName);

            // Need to push an extra param to make the argument order consistent with jquery
            var newParams = [];
            newParams.push({});

            if (params && params.length) {
                for (var i = 0, length = params.length; i < length; i++) {
                    newParams.push(params[i]);
                }
            }

            bean.fire(obj, eventName, newParams);
        }
    };

})(window);