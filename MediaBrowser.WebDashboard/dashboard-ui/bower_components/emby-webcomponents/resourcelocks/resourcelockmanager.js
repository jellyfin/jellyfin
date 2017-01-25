define([], function () {
    'use strict';

    function getRequirePromise(deps) {

        return new Promise(function (resolve, reject) {

            require(deps, resolve);
        });
    }

    function requestResourceLock(resource) {
        return getRequirePromise([resource]).then(function (factory) {
            return new factory();
        });
    }

    function request(type) {

        switch (type) {
        
            case 'wake':
                return requestResourceLock('wakeLock');
            case 'screen':
                return requestResourceLock('screenLock');
            case 'network':
                return requestResourceLock('networkLock');
            default:
                return Promise.reject();
        }
        return Promise.resolve(new ResourceLockInstance(type));
    }

    return {
        request: request
    };
});