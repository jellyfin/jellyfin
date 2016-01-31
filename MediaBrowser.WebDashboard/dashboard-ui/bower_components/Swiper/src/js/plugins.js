/*=========================
  Plugins API. Collect all and init all plugins
  ===========================*/
s._plugins = [];
for (var plugin in s.plugins) {
    var p = s.plugins[plugin](s, s.params[plugin]);
    if (p) s._plugins.push(p);
}
// Method to call all plugins event/method
s.callPlugins = function (eventName) {
    for (var i = 0; i < s._plugins.length; i++) {
        if (eventName in s._plugins[i]) {
            s._plugins[i][eventName](arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]);
        }
    }
};