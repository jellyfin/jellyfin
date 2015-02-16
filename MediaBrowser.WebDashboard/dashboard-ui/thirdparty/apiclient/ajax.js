(function (globalScope) {

    globalScope.AjaxApi = {

        param: function(params) {
            return jQuery.param(params);
        },

        ajax: function(request) {

            return jQuery.ajax(request);
        }

    };

})(window);