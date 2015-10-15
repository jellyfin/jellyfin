'use strict';

var test = require('tape');
var dragula = require('..');

test('drake can be instantiated without throwing', function (t) {
  t.doesNotThrow(drakeFactory, 'calling dragula() without arguments does not throw');
  t.end();
  function drakeFactory () {
    return dragula();
  }
});

test('drake has expected api properties', function (t) {
  var drake = dragula();
  t.ok(drake, 'drake is not null');
  t.equal(typeof drake, 'object', 'drake is an object');
  t.ok(Array.isArray(drake.containers), 'drake.containers is an array');
  t.equal(typeof drake.start, 'function', 'drake.start is a method');
  t.equal(typeof drake.end, 'function', 'drake.end is a method');
  t.equal(typeof drake.cancel, 'function', 'drake.cancel is a method');
  t.equal(typeof drake.remove, 'function', 'drake.remove is a method');
  t.equal(typeof drake.destroy, 'function', 'drake.destroy is a method');
  t.equal(typeof drake.dragging, 'boolean', 'drake.dragging is a boolean');
  t.equal(drake.dragging, false, 'drake.dragging is initialized as false');
  t.end();
});
