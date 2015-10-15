'use strict';

var test = require('tape');
var dragula = require('..');

test('cancel does not throw when not dragging', function (t) {
  t.test('a single time', function once (st) {
    var drake = dragula();
    st.doesNotThrow(function () {
      drake.cancel();
    }, 'dragula ignores a single call to drake.cancel');
    st.end();
  });
  t.test('multiple times', function once (st) {
    var drake = dragula();
    st.doesNotThrow(function () {
      drake.cancel();
      drake.cancel();
      drake.cancel();
      drake.cancel();
    }, 'dragula ignores multiple calls to drake.cancel');
    st.end();
  });
  t.end();
});

test('when dragging and cancel gets called, nothing happens', function (t) {
  var div = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div]);
  div.appendChild(item);
  document.body.appendChild(div);
  drake.start(item);
  drake.cancel();
  t.equal(div.children.length, 1, 'nothing happens');
  t.equal(drake.dragging, false, 'drake has stopped dragging');
  t.end();
});

test('when dragging and cancel gets called, cancel event is emitted', function (t) {
  var div = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div]);
  div.appendChild(item);
  document.body.appendChild(div);
  drake.start(item);
  drake.on('cancel', cancel);
  drake.on('dragend', dragend);
  drake.cancel();
  t.plan(3);
  t.end();
  function dragend () {
    t.pass('dragend got called');
  }
  function cancel (target, container) {
    t.equal(target, item, 'cancel was invoked with item');
    t.equal(container, div, 'cancel was invoked with container');
  }
});

test('when dragging a copy and cancel gets called, default does not revert', function (t) {
  var div = document.createElement('div');
  var div2 = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div, div2]);
  div.appendChild(item);
  document.body.appendChild(div);
  document.body.appendChild(div2);
  drake.start(item);
  div2.appendChild(item);
  drake.on('drop', drop);
  drake.on('dragend', dragend);
  drake.cancel();
  t.plan(4);
  t.end();
  function dragend () {
    t.pass('dragend got called');
  }
  function drop (target, parent, source) {
    t.equal(target, item, 'drop was invoked with item');
    t.equal(parent, div2, 'drop was invoked with final container');
    t.equal(source, div, 'drop was invoked with source container');
  }
});

test('when dragging a copy and cancel gets called, revert is executed', function (t) {
  var div = document.createElement('div');
  var div2 = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div, div2]);
  div.appendChild(item);
  document.body.appendChild(div);
  document.body.appendChild(div2);
  drake.start(item);
  div2.appendChild(item);
  drake.on('cancel', cancel);
  drake.on('dragend', dragend);
  drake.cancel(true);
  t.plan(3);
  t.end();
  function dragend () {
    t.pass('dragend got called');
  }
  function cancel (target, container) {
    t.equal(target, item, 'cancel was invoked with item');
    t.equal(container, div, 'cancel was invoked with container');
  }
});
