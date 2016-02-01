define([], function () {

    function myStore(defaultObject) {

        var self = this;
        self.localData = {};

        var isDefaultAvailable;

        if (defaultObject) {
            try {
                defaultObject.setItem('_test', '0');
                defaultObject.removeItem('_test');
                isDefaultAvailable = true;
            } catch (e) {

            }
        }

        self.setItem = function (name, value) {

            if (isDefaultAvailable) {
                defaultObject.setItem(name, value);
            } else {
                self.localData[name] = value;
            }
        };

        self.getItem = function (name) {

            if (isDefaultAvailable) {
                return defaultObject.getItem(name);
            }

            return self.localData[name];
        };

        self.removeItem = function (name) {

            if (isDefaultAvailable) {
                defaultObject.removeItem(name);
            } else {
                self.localData[name] = null;
            }
        };
    }

    try {
        return new myStore(localStorage);
    } catch (err) {
        return new myStore();
    }
});