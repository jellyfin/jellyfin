"use strict";
window.queryString = {}, window.queryString.extract = function(maybeUrl) {
    return maybeUrl.split("?")[1] || ""
}, window.queryString.parse = function(str) {
    return "string" != typeof str ? {} : (str = str.trim().replace(/^(\?|#|&)/, ""), str ? str.split("&").reduce(function(ret, param) {
        var parts = param.replace(/\+/g, " ").split("="),
            key = parts[0],
            val = parts[1];
        return key = decodeURIComponent(key), val = void 0 === val ? null : decodeURIComponent(val), ret.hasOwnProperty(key) ? Array.isArray(ret[key]) ? ret[key].push(val) : ret[key] = [ret[key], val] : ret[key] = val, ret
    }, {}) : {})
}, window.queryString.stringify = function(obj) {
    return obj ? Object.keys(obj).sort().map(function(key) {
        var val = obj[key];
        return Array.isArray(val) ? val.sort().map(function(val2) {
            return encodeURIComponent(key) + "=" + encodeURIComponent(val2)
        }).join("&") : encodeURIComponent(key) + "=" + encodeURIComponent(val)
    }).join("&") : ""
};