'use strict';

var test = require('tape');
var events = require('./lib/events');
var dragula = require('..');

test('remove does not throw when not dragging', function (t) {
  t.test('a single time', function once (st) {
    var drake = dragula();
    st.doesNotThrow(function () {
      drake.remove();
    }, 'dragula ignores a single call to drake.remove');
    st.end();
  });
  t.test('multiple times', function once (st) {
    var drake = dragula();
    st.doesNotThrow(function () {
      drake.remove();
      drake.remove();
      drake.remove();
      drake.remove();
    }, 'dragula ignores multiple calls to drake.remove');
    st.end();
  });
  t.end();
});

test('when dragging and remove gets called, element is removed', function (t) {
  var div = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div]);
  div.appendChild(item);
  document.body.appendChild(div);
  drake.start(item);
  drake.remove();
  t.equal(div.children.length, 0, 'item got removed from container');
  t.equal(drake.dragging, false, 'drake has stopped dragging');
  t.end();
});

test('when dragging and remove gets called, remove event is emitted', function (t) {
  var div = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div]);
  div.appendChild(item);
  document.body.appendChild(div);
  drake.start(item);
  drake.on('remove', remove);
  drake.on('dragend', dragend);
  drake.remove();
  t.plan(3);
  t.end();
  function dragend () {
    t.pass('dragend got called');
  }
  function remove (target, container) {
    t.equal(target, item, 'remove was invoked with item');
    t.equal(container, div, 'remove was invoked with container');
  }
});

test('when dragging a copy and remove gets called, cancel event is emitted', function (t) {
  var div = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div], { copy: true });
  div.appendChild(item);
  document.body.appendChild(div);
  events.raise(item, 'mousedown', { which: 0 });
  events.raise(item, 'mousemove', { which: 0 });
  drake.on('cancel', cancel);
  drake.on('dragend', dragend);
  drake.remove();
  t.plan(4);
  t.end();
  function dragend () {
    t.pass('dragend got called');
  }
  function cancel (target, container) {
    t.equal(target.className, 'gu-transit', 'cancel was invoked with item');
    t.notEqual(target, item, 'item is a copy and not the original');
    t.equal(container, null, 'cancel was invoked with container');
  }
});

test('when dragging a copy and remove gets called, cancel event is emitted', function (t) {
  var div = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div], { copy: true });
  div.appendChild(item);
  document.body.appendChild(div);
  events.raise(item, 'mousedown', { which: 0 });
  events.raise(item, 'mousemove', { which: 0 });
  drake.on('cancel', cancel);
  drake.on('dragend', dragend);
  drake.remove();
  t.plan(4);
  t.end();
  function dragend () {
    t.pass('dragend got called');
  }
  function cancel (target, container) {
    t.equal(target.className, 'gu-transit', 'cancel was invoked with item');
    t.notEqual(target, item, 'item is a copy and not the original');
    t.equal(container, null, 'cancel was invoked with container');
  }
});
