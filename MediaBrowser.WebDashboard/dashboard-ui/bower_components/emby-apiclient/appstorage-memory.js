define([], function () {
    'use strict';

    function MyStore(defaultObject) {

        this.localData = {};
    }

    MyStore.prototype.setItem = function (name, value) {
        this.localData[name] = value;
    };

    MyStore.prototype.getItem = function (name) {
        return this.localData[name];
    };

    MyStore.prototype.removeItem = function (name) {
        this.localData[name] = null;
    };

    return new MyStore();
});