define([], function () {
    'use strict';

    function ResourceLockInstance() {
    }

    ResourceLockInstance.prototype.acquire = function () {
        this._isHeld = true;
    };

    ResourceLockInstance.prototype.isHeld = function () {
        return this._isHeld === true;
    };

    ResourceLockInstance.prototype.release = function () {
        this._isHeld = false;
    };

    return ResourceLockInstance;
});