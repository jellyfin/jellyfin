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

//(function () {

//    function useJqueryEvents(elems, eventName) {

//        eventName = eventName.type || eventName;

//        if (typeof eventName == 'string') {
//            if (eventName.indexOf('page') == 0) {
//                return true;
//            }
//            if (eventName.indexOf('loadercreate') == 0) {
//                return true;
//            }
//        } else {
//            return true;
//        }

//        //console.log('bean: ' + eventName);
//        return false;
//    }

//    $.fn.jTrigger = $.fn.trigger;
//    $.fn.jOn = $.fn.on;
//    $.fn.jOff = $.fn.off;

//    $.fn.off = function (eventName, selector, fn, ex1, ex2, ex3) {

//        if (arguments.length > 3 || useJqueryEvents(this, eventName)) {
//            this.jOff(eventName, selector, fn, ex1, ex2, ex3);
//            return this;
//        }

//        for (var i = 0, length = this.length; i < length; i++) {
//            bean.off(this[i], eventName, selector, fn);
//        }
//        return this;
//    };

//    $.fn.on = function (eventName, selector, fn, ex1, ex2, ex3) {

//        if (arguments.length > 3 || useJqueryEvents(this, eventName)) {
//            this.jOn(eventName, selector, fn, ex1, ex2, ex3);
//            return this;
//        }

//        for (var i = 0, length = this.length; i < length; i++) {
//            bean.on(this[i], eventName, selector, fn);
//        }
//        return this;
//    };

//    $.fn.trigger = function (eventName, params) {

//        if (useJqueryEvents(this, eventName)) {
//            this.jTrigger(eventName, params);
//            return this;
//        }

//        var i, length;

//        // Need to push an extra param to make the argument order consistent with jquery
//        var newParams = [];
//        newParams.push({});

//        if (params && params.length) {
//            for (i = 0, length = params.length; i < length; i++) {
//                newParams.push(params[i]);
//            }
//        }

//        for (i = 0, length = this.length; i < length; i++) {
//            bean.fire(this[i], eventName, newParams);
//        }
//        return this;
//    };

//})();