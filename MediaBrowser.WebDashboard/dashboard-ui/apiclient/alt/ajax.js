(function (globalScope, angular) {

    globalScope.HttpClient = {

        param: function(params) {
            return serialize(params);
        },

        send: function(options) {
            var request = getAngularRequest(options),
                defer = globalScope.DeferredBuilder.Deferred();

            request.then(function(results) {
              defer.resolve(results.data);
            }, function() {
              defer.reject();
            });

            return defer.promise();
        }

    };

    // Code from: http://stackoverflow.com/questions/1714786/querystring-encoding-of-a-javascript-object
    function serialize (obj, prefix) {
      var str = [];
      for(var p in obj) {
        if (obj.hasOwnProperty(p)) {
          var k = prefix ? prefix + "[" + p + "]" : p, v = obj[p];
          str.push(typeof v == "object" ?
            serialize(v, k) :
            encodeURIComponent(k) + "=" + encodeURIComponent(v));
        }
      }
      return str.join("&");
    }

    var $http = angular.injector(['ng']).get('$http');

    function getAngularRequest(jParams) {
      var optionTransforms = [],
          promiseTransforms = [],
          options = {},
          // paramMap houses the param transforms in one of the following formats:
          //  string - This means there is a direct mapping from jQuery option to Angular option, but allows for a different option name
          //  function - This means some logic is required in applying this option to the Angular request. Functions should add functions
          //             to the optionTransforms or promiseTransforms arrays, which will be executed after direct mappings are complete.
          paramMap = {
            'accepts': undefined,
            'async': undefined,
            'beforeSend': undefined,
            'cache': undefined,
            'complete': undefined,
            'contents': undefined,
            'contentType': function(val) {
              optionTransforms.push(function(opt) {
                opt.headers = opt.headers || {};
                opt.headers['Content-Type'] = val;
                return opt;
              });
            },
            'context': undefined,
            'converters': undefined,
            'crossDomain': undefined,
            'data': 'data',
            'dataFilter': undefined,
            'dataType': 'responseType',
            'error': undefined,
            'global': undefined,
            'headers': 'headers',
            'ifModified': undefined,
            'isLocal': undefined,
            'jsonp': undefined,
            'jsonpCallback': undefined,
            'mimeType': undefined,
            'password': undefined,
            'processData': undefined,
            'scriptCharset': undefined,
            'statusCode': undefined,
            'success': undefined,
            'timeout': 'timeout',
            'traditional': undefined,
            'type': 'method',
            'url': 'url',
            'username': undefined,
            'xhr': undefined,
            'xhrFields': undefined,
          };

      // Iterate through each key in the jQuery format options object
      for (var key in jParams) {
        if (!paramMap[key]) {
          // This parameter hasn't been implemented in the paramMap object
          Logger.log('ERROR: ajax option property "' + key + '" not implemented by HttpClient.');
          continue;
        }

        if (typeof paramMap[key] === 'string') {
          // Direct mapping between two properties
          options[paramMap[key]] = jParams[key];
          continue;
        }

        if (typeof paramMap[key] === 'function') {
          // Extra logic required. Execute the function with the jQuery option as the only function argument
          paramMap[key](jParams[key]);
        }
      }

      // Iterate through any optionTransforms functions and execute them with the options object as argument
      while (optionTransforms.length > 0) {
        options = optionTransforms.pop()(options);
      }

      // Create the Angular http request (returns the request's promise object)
      var promise = $http(options);

      // Iterate through any promiseTransforms functions and execute them with the promise as argument.
      while (promiseTransforms.length > 0) {
        promiseTransforms.pop()(promise);
      }

      return promise;
    }

})(window, angular);