'use strict';

function raise (el, type, options) {
  var o = options || {};
  var e = document.createEvent('Event');
  e.initEvent(type, true, true);
  Object.keys(o).forEach(apply);
  el.dispatchEvent(e);
  function apply (key) {
    e[key] = o[key];
  }
}

module.exports = {
  raise: raise
};
