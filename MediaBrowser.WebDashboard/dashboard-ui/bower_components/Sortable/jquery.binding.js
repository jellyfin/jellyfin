! function(factory) {
    "use strict";
    "function" == typeof define && define.amd ? define(["jquery"], factory) : factory(jQuery)
}(function($) {
    "use strict";
    $.fn.sortable = function(options) {
        var retVal, args = arguments;
        return this.each(function() {
            var $el = $(this),
                sortable = $el.data("sortable");
            if (sortable || !(options instanceof Object) && options || (sortable = new Sortable(this, options), $el.data("sortable", sortable)), sortable) {
                if ("widget" === options) return sortable;
                "destroy" === options ? (sortable.destroy(), $el.removeData("sortable")) : "function" == typeof sortable[options] ? retVal = sortable[options].apply(sortable, [].slice.call(args, 1)) : options in sortable.options && (retVal = sortable.option.apply(sortable, args))
            }
        }), void 0 === retVal ? this : retVal
    }
});