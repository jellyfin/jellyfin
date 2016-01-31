/*=========================
  Events/Callbacks/Plugins Emitter
  ===========================*/
function normalizeEventName (eventName) {
    if (eventName.indexOf('on') !== 0) {
        if (eventName[0] !== eventName[0].toUpperCase()) {
            eventName = 'on' + eventName[0].toUpperCase() + eventName.substring(1);
        }
        else {
            eventName = 'on' + eventName;
        }
    }
    return eventName;
}
s.emitterEventListeners = {

};
s.emit = function (eventName) {
    // Trigger callbacks
    if (s.params[eventName]) {
        s.params[eventName](arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]);
    }
    var i;
    // Trigger events
    if (s.emitterEventListeners[eventName]) {
        for (i = 0; i < s.emitterEventListeners[eventName].length; i++) {
            s.emitterEventListeners[eventName][i](arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]);
        }
    }
    // Trigger plugins
    if (s.callPlugins) s.callPlugins(eventName, arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]);
};
s.on = function (eventName, handler) {
    eventName = normalizeEventName(eventName);
    if (!s.emitterEventListeners[eventName]) s.emitterEventListeners[eventName] = [];
    s.emitterEventListeners[eventName].push(handler);
    return s;
};
s.off = function (eventName, handler) {
    var i;
    eventName = normalizeEventName(eventName);
    if (typeof handler === 'undefined') {
        // Remove all handlers for such event
        s.emitterEventListeners[eventName] = [];
        return s;
    }
    if (!s.emitterEventListeners[eventName] || s.emitterEventListeners[eventName].length === 0) return;
    for (i = 0; i < s.emitterEventListeners[eventName].length; i++) {
        if(s.emitterEventListeners[eventName][i] === handler) s.emitterEventListeners[eventName].splice(i, 1);
    }
    return s;
};
s.once = function (eventName, handler) {
    eventName = normalizeEventName(eventName);
    var _handler = function () {
        handler(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4]);
        s.off(eventName, _handler);
    };
    s.on(eventName, _handler);
    return s;
};