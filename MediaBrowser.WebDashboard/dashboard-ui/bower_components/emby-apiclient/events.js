define([], function () {

    function getCallbacks(obj, name) {

        ensureCallbacks(obj, name);

        return obj._callbacks[name];
    }

    function ensureCallbacks(obj, name) {

        if (!obj) {
            throw new Error("obj cannot be null!");
        }

        obj._callbacks = obj._callbacks || {};

        if (!obj._callbacks[name]) {
            obj._callbacks[name] = [];
        }
    }

    return {

        on: function (obj, eventName, fn) {

            var list = getCallbacks(obj, eventName);

            if (list.indexOf(fn) == -1) {
                list.push(fn);
            }
        },

        off: function (obj, eventName, fn) {

            var list = getCallbacks(obj, eventName);
            obj._callbacks[name] = list.filter(function (i) {
                return i != fn;
            });
        },

        trigger: function (obj, eventName) {

            var eventObject = {
                type: eventName
            };

            var eventArgs = [];
            eventArgs.push(eventObject);

            var additionalArgs = arguments[2] || [];
            for (var i = 0, length = additionalArgs.length; i < length; i++) {
                eventArgs.push(additionalArgs[i]);
            }

            getCallbacks(obj, eventName).forEach(function (c) {
                c.apply(obj, eventArgs);
            });
        }
    };
});