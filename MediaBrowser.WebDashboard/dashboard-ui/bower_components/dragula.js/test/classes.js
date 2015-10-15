'use strict';

var test = require('tape');
var classes = require('../classes');

test('classes exports the expected api', function (t) {
  t.equal(typeof classes.add, 'function', 'classes.add is a method');
  t.equal(typeof classes.rm, 'function', 'classes.rm is a method');
  t.end();
});

test('classes can add a class', function (t) {
  var el = document.createElement('div');
  classes.add(el, 'gu-foo');
  t.equal(el.className, 'gu-foo', 'setting a class works');
  t.end();
});

test('classes can add a class to an element that already has classes', function (t) {
  var el = document.createElement('div');
  el.className = 'bar';
  classes.add(el, 'gu-foo');
  t.equal(el.className, 'bar gu-foo', 'appending a class works');
  t.end();
});

test('classes.add is a no-op if class already is in element', function (t) {
  var el = document.createElement('div');
  el.className = 'gu-foo';
  classes.add(el, 'gu-foo');
  t.equal(el.className, 'gu-foo', 'no-op as expected');
  t.end();
});

test('classes can remove a class', function (t) {
  var el = document.createElement('div');
  el.className = 'gu-foo';
  classes.rm(el, 'gu-foo');
  t.equal(el.className, '', 'removing a class works');
  t.end();
});

test('classes can remove a class from a list on the right', function (t) {
  var el = document.createElement('div');
  el.className = 'bar gu-foo';
  classes.rm(el, 'gu-foo');
  t.equal(el.className, 'bar', 'removing a class from the list works to the right');
  t.end();
});

test('classes can remove a class from a list on the left', function (t) {
  var el = document.createElement('div');
  el.className = 'gu-foo bar';
  classes.rm(el, 'gu-foo');
  t.equal(el.className, 'bar', 'removing a class from the list works to the left');
  t.end();
});

test('classes can remove a class from a list on the middle', function (t) {
  var el = document.createElement('div');
  el.className = 'foo gu-foo bar';
  classes.rm(el, 'gu-foo');
  t.equal(el.className, 'foo bar', 'removing a class from the list works to the middle');
  t.end();
});
