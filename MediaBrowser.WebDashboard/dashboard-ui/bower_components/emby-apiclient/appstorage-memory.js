define([], function () {

    function myStore(defaultObject) {

        this.localData = {};
    }

    myStore.prototype.setItem = function (name, value) {
        this.localData[name] = value;
    };

    myStore.prototype.getItem = function (name) {
        return this.localData[name];
    };

    myStore.prototype.removeItem = function (name) {
        this.localData[name] = null;
    };

    return new myStore();
});