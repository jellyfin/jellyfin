(function (globalScope) {

    globalScope.AjaxApi = {

        param: function (params) {
            return jQuery.param(params);
        },

        ajax: function (request) {

            request.timeout = request.timeout || 30000;

            try {
                return jQuery.ajax(request);
            } catch (err) {
                var deferred = DeferredBuilder.Deferred();
                deferred.reject();
                return deferred.promise();
            }
        }

    };

})(window);