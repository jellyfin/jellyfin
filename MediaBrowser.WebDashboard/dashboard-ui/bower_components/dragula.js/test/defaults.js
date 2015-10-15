'use strict';

var test = require('tape');
var dragula = require('..');

test('drake has sensible default options', function (t) {
  var options = {};
  dragula(options);
  t.equal(typeof options.moves, 'function', 'options.moves defaults to a method');
  t.equal(typeof options.accepts, 'function', 'options.accepts defaults to a method');
  t.equal(typeof options.invalid, 'function', 'options.invalid defaults to a method');
  t.equal(typeof options.isContainer, 'function', 'options.isContainer defaults to a method');
  t.equal(options.copy, false, 'options.copy defaults to false');
  t.equal(options.revertOnSpill, false, 'options.revertOnSpill defaults to false');
  t.equal(options.removeOnSpill, false, 'options.removeOnSpill defaults to false');
  t.equal(options.direction, 'vertical', 'options.direction defaults to \'vertical\'');
  t.equal(options.mirrorContainer, document.body, 'options.mirrorContainer defaults to an document.body');
  t.end();
});
