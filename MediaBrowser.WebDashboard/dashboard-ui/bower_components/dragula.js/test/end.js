'use strict';

var test = require('tape');
var dragula = require('..');

test('end does not throw when not dragging', function (t) {
  t.test('a single time', function once (st) {
    var drake = dragula();
    st.doesNotThrow(function () {
      drake.end();
    }, 'dragula ignores a single call to drake.end');
    st.end();
  });
  t.test('multiple times', function once (st) {
    var drake = dragula();
    st.doesNotThrow(function () {
      drake.end();
      drake.end();
      drake.end();
      drake.end();
    }, 'dragula ignores multiple calls to drake.end');
    st.end();
  });
  t.end();
});

test('when already dragging, .end() ends (cancels) previous drag', function (t) {
  var div = document.createElement('div');
  var item1 = document.createElement('div');
  var item2 = document.createElement('div');
  var drake = dragula([div]);
  div.appendChild(item1);
  div.appendChild(item2);
  document.body.appendChild(div);
  drake.start(item1);
  drake.on('dragend', end);
  drake.on('cancel', cancel);
  drake.end();
  t.plan(4);
  t.equal(drake.dragging, false, 'final state is: drake is not dragging');
  t.end();
  function end (item) {
    t.equal(item, item1, 'dragend invoked with correct item');
  }
  function cancel (item, source) {
    t.equal(item, item1, 'cancel invoked with correct item');
    t.equal(source, div, 'cancel invoked with correct source');
  }
});

test('when already dragged, ends (drops) previous drag', function (t) {
  var div = document.createElement('div');
  var div2 = document.createElement('div');
  var item1 = document.createElement('div');
  var item2 = document.createElement('div');
  var drake = dragula([div, div2]);
  div.appendChild(item1);
  div.appendChild(item2);
  document.body.appendChild(div);
  document.body.appendChild(div2);
  drake.start(item1);
  div2.appendChild(item1);
  drake.on('dragend', end);
  drake.on('drop', drop);
  drake.end();
  t.plan(5);
  t.equal(drake.dragging, false, 'final state is: drake is not dragging');
  t.end();
  function end (item) {
    t.equal(item, item1, 'dragend invoked with correct item');
  }
  function drop (item, target, source) {
    t.equal(item, item1, 'drop invoked with correct item');
    t.equal(source, div, 'drop invoked with correct source');
    t.equal(target, div2, 'drop invoked with correct target');
  }
});
