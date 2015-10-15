'use strict';

var test = require('tape');
var dragula = require('..');

test('destroy does not throw when not dragging, destroyed, or whatever', function (t) {
  t.test('a single time', function once (st) {
    var drake = dragula();
    st.doesNotThrow(function () {
      drake.destroy();
    }, 'dragula bites into a single call to drake.destroy');
    st.end();
  });
  t.test('multiple times', function once (st) {
    var drake = dragula();
    st.doesNotThrow(function () {
      drake.destroy();
      drake.destroy();
      drake.destroy();
      drake.destroy();
    }, 'dragula bites into multiple calls to drake.destroy');
    st.end();
  });
  t.end();
});

test('when dragging and destroy gets called, nothing happens', function (t) {
  var div = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div]);
  div.appendChild(item);
  document.body.appendChild(div);
  drake.start(item);
  drake.destroy();
  t.equal(div.children.length, 1, 'nothing happens');
  t.equal(drake.dragging, false, 'drake has stopped dragging');
  t.end();
});

test('when dragging and destroy gets called, dragend event is emitted gracefully', function (t) {
  var div = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div]);
  div.appendChild(item);
  document.body.appendChild(div);
  drake.start(item);
  drake.on('dragend', dragend);
  drake.destroy();
  t.plan(1);
  t.end();
  function dragend () {
    t.pass('dragend got called');
  }
});

test('when dragging a copy and destroy gets called, default does not revert', function (t) {
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
  drake.destroy();
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

test('when dragging a copy and destroy gets called, revert is executed', function (t) {
  var div = document.createElement('div');
  var div2 = document.createElement('div');
  var item = document.createElement('div');
  var drake = dragula([div, div2], { revertOnSpill: true });
  div.appendChild(item);
  document.body.appendChild(div);
  document.body.appendChild(div2);
  drake.start(item);
  div2.appendChild(item);
  drake.on('cancel', cancel);
  drake.on('dragend', dragend);
  drake.destroy();
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
