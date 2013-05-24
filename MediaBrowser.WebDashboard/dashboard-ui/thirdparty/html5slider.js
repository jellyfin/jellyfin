/*
html5slider - a JS implementation of <input type=range> for Firefox 16 and up
https://github.com/fryn/html5slider

Copyright (c) 2010-2013 Frank Yan, <http://frankyan.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

(function() {

// test for native support
var test = document.createElement('input');
try {
  test.type = 'range';
  if (test.type == 'range')
    return;
} catch (e) {
  return;
}

// test for required property support
test.style.background = 'linear-gradient(red, red)';
if (!test.style.backgroundImage || !('MozAppearance' in test.style))
  return;

var scale;
var isMac = navigator.platform == 'MacIntel';
var thumb = {
  radius: isMac ? 9 : 6,
  width: isMac ? 22 : 12,
  height: isMac ? 16 : 20
};
var track = 'linear-gradient(transparent ' + (isMac ?
  '6px, #999 6px, #999 7px, #ccc 8px, #bbb 9px, #bbb 10px, transparent 10px' :
  '9px, #999 9px, #bbb 10px, #fff 11px, transparent 11px') +
  ', transparent)';
var styles = {
  'min-width': thumb.width + 'px',
  'min-height': thumb.height + 'px',
  'max-height': thumb.height + 'px',
  padding: '0 0 ' + (isMac ? '2px' : '1px'),
  border: 0,
  'border-radius': 0,
  cursor: 'default',
  'text-indent': '-999999px' // -moz-user-select: none; breaks mouse capture
};
var options = {
  attributes: true,
  attributeFilter: ['min', 'max', 'step', 'value']
};
var onInput = document.createEvent('HTMLEvents');
onInput.initEvent('input', true, false);
var onChange = document.createEvent('HTMLEvents');
onChange.initEvent('change', true, false);

if (document.readyState == 'loading')
  document.addEventListener('DOMContentLoaded', initialize, true);
else
  initialize();
addEventListener('pageshow', recreate, true);

function initialize() {
  // create initial sliders
  recreate();
  // create sliders on-the-fly
  new MutationObserver(function(mutations) {
    mutations.forEach(function(mutation) {
      if (mutation.addedNodes)
        Array.forEach(mutation.addedNodes, function(node) {
          if (!(node instanceof Element))
            ;
          else if (node.childElementCount)
            Array.forEach(node.querySelectorAll('input[type=range]'), check);
          else if (node.mozMatchesSelector('input[type=range]'))
            check(node);
        });
    });
  }).observe(document, { childList: true, subtree: true });
}

function recreate() {
  Array.forEach(document.querySelectorAll('input[type=range]'), check);
}

function check(input) {
  if (input.type != 'range')
    transform(input);
}

function transform(slider) {

  var isValueSet, areAttrsSet, isUI, isClick, prevValue, rawValue, prevX;
  var min, max, step, range, value = slider.value;

  // lazily create shared slider affordance
  if (!scale) {
    scale = document.body.appendChild(document.createElement('hr'));
    style(scale, {
      '-moz-appearance': isMac ? 'scale-horizontal' : 'scalethumb-horizontal',
      display: 'block',
      visibility: 'visible',
      opacity: 1,
      position: 'fixed',
      top: '-999999px'
    });
    document.mozSetImageElement('__sliderthumb__', scale);
  }

  // reimplement value and type properties
  var getValue = function() { return '' + value; };
  var setValue = function setValue(val) {
    value = '' + val;
    isValueSet = true;
    draw();
    delete slider.value;
    slider.value = value;
    slider.__defineGetter__('value', getValue);
    slider.__defineSetter__('value', setValue);
  };
  slider.__defineGetter__('value', getValue);
  slider.__defineSetter__('value', setValue);
  Object.defineProperty(slider, 'type', {
    get: function() { return 'range'; }
  });

  // sync properties with attributes
  ['min', 'max', 'step'].forEach(function(name) {
    if (slider.hasAttribute(name))
      areAttrsSet = true;
    Object.defineProperty(slider, name, {
      get: function() {
        return this.hasAttribute(name) ? this.getAttribute(name) : '';
      },
      set: function(val) {
        val === null ?
          this.removeAttribute(name) :
          this.setAttribute(name, val);
      }
    });
  });

  // initialize slider
  slider.readOnly = true;
  style(slider, styles);
  update();

  new MutationObserver(function(mutations) {
    mutations.forEach(function(mutation) {
      if (mutation.attributeName != 'value') {
        update();
        areAttrsSet = true;
      }
      // note that value attribute only sets initial value
      else if (!isValueSet) {
        value = slider.getAttribute('value');
        draw();
      }
    });
  }).observe(slider, options);

  slider.addEventListener('mousedown', onDragStart, true);
  slider.addEventListener('keydown', onKeyDown, true);
  slider.addEventListener('focus', onFocus, true);
  slider.addEventListener('blur', onBlur, true);

  function onDragStart(e) {
    isClick = true;
    setTimeout(function() { isClick = false; }, 0);
    if (e.button || !range)
      return;
    var width = parseFloat(getComputedStyle(this).width);
    var multiplier = (width - thumb.width) / range;
    if (!multiplier)
      return;
    // distance between click and center of thumb
    var dev = e.clientX - this.getBoundingClientRect().left - thumb.width / 2 -
              (value - min) * multiplier;
    // if click was not on thumb, move thumb to click location
    if (Math.abs(dev) > thumb.radius) {
      isUI = true;
      this.value -= -dev / multiplier;
    }
    rawValue = value;
    prevX = e.clientX;
    this.addEventListener('mousemove', onDrag, true);
    this.addEventListener('mouseup', onDragEnd, true);
  }

  function onDrag(e) {
    var width = parseFloat(getComputedStyle(this).width);
    var multiplier = (width - thumb.width) / range;
    if (!multiplier)
      return;
    rawValue += (e.clientX - prevX) / multiplier;
    prevX = e.clientX;
    isUI = true;
    this.value = rawValue;
  }

  function onDragEnd() {
    this.removeEventListener('mousemove', onDrag, true);
    this.removeEventListener('mouseup', onDragEnd, true);
    slider.dispatchEvent(onInput);
    slider.dispatchEvent(onChange);
  }

  function onKeyDown(e) {
    if (e.keyCode > 36 && e.keyCode < 41) { // 37-40: left, up, right, down
      onFocus.call(this);
      isUI = true;
      this.value = value + (e.keyCode == 38 || e.keyCode == 39 ? step : -step);
    }
  }

  function onFocus() {
    if (!isClick)
      this.style.boxShadow = !isMac ? '0 0 0 2px #fb0' :
        'inset 0 0 20px rgba(0,127,255,.1), 0 0 1px rgba(0,127,255,.4)';
  }

  function onBlur() {
    this.style.boxShadow = '';
  }

  // determines whether value is valid number in attribute form
  function isAttrNum(value) {
    return !isNaN(value) && +value == parseFloat(value);
  }

  // validates min, max, and step attributes and redraws
  function update() {
    min = isAttrNum(slider.min) ? +slider.min : 0;
    max = isAttrNum(slider.max) ? +slider.max : 100;
    if (max < min)
      max = min > 100 ? min : 100;
    step = isAttrNum(slider.step) && slider.step > 0 ? +slider.step : 1;
    range = max - min;
    draw(true);
  }

  // recalculates value property
  function calc() {
    if (!isValueSet && !areAttrsSet)
      value = slider.getAttribute('value');
    if (!isAttrNum(value))
      value = (min + max) / 2;;
    // snap to step intervals (WebKit sometimes does not - bug?)
    value = Math.round((value - min) / step) * step + min;
    if (value < min)
      value = min;
    else if (value > max)
      value = min + ~~(range / step) * step;
  }

  // renders slider using CSS background ;)
  function draw(attrsModified) {
    calc();
    var wasUI = isUI;
    isUI = false;
    if (wasUI && value != prevValue)
      slider.dispatchEvent(onInput);
    if (!attrsModified && value == prevValue)
      return;
    prevValue = value;
    var position = range ? (value - min) / range * 100 : 0;
    var bg = '-moz-element(#__sliderthumb__) ' + position + '% no-repeat, ';
    style(slider, { background: bg + track });
  }

}

function style(element, styles) {
  for (var prop in styles)
    element.style.setProperty(prop, styles[prop], 'important');
}

})();
