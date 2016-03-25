/**
 * @license
 * Copyright 2015 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * Class constructor for Animation MDL component.
 * Implements MDL component design pattern defined at:
 * https://github.com/jasonmayes/mdl-component-design-pattern
 * @param {HTMLElement} element The element that will be upgraded.
 */
function DemoAnimation(element) {
  'use strict';

  this.element_ = element;
  this.position_ = this.Constant_.STARTING_POSITION;
  this.movable_ = this.element_.querySelector('.' + this.CssClasses_.MOVABLE);
  // Initialize instance.
  this.init();
}

/**
 * Store strings for class names defined by this component that are used in
 * JavaScript. This allows us to simply change it in one place should we
 * decide to modify at a later date.
 * @enum {string}
 * @private
 */
DemoAnimation.prototype.CssClasses_ = {
  MOVABLE: 'demo-animation__movable',
  POSITION_PREFIX: 'demo-animation--position-',
  FAST_OUT_SLOW_IN: 'mdl-animation--fast-out-slow-in',
  LINEAR_OUT_SLOW_IN: 'mdl-animation--linear-out-slow-in',
  FAST_OUT_LINEAR_IN: 'mdl-animation--fast-out-linear-in'
};

/**
 * Store constants in one place so they can be updated easily.
 * @enum {string | number}
 * @private
 */
DemoAnimation.prototype.Constant_ = {
  STARTING_POSITION: 0,
  // Which animation to use for which state. Check demo.css for an explanation.
  ANIMATIONS: [
    DemoAnimation.prototype.CssClasses_.FAST_OUT_LINEAR_IN,
    DemoAnimation.prototype.CssClasses_.LINEAR_OUT_SLOW_IN,
    DemoAnimation.prototype.CssClasses_.FAST_OUT_SLOW_IN,
    DemoAnimation.prototype.CssClasses_.FAST_OUT_LINEAR_IN,
    DemoAnimation.prototype.CssClasses_.LINEAR_OUT_SLOW_IN,
    DemoAnimation.prototype.CssClasses_.FAST_OUT_SLOW_IN
  ]
};

/**
 * Handle click of element.
 * @param {Event} event The event that fired.
 * @private
 */
DemoAnimation.prototype.handleClick_ = function(event) {
  'use strict';

  this.movable_.classList.remove(this.CssClasses_.POSITION_PREFIX +
      this.position_);
  this.movable_.classList.remove(this.Constant_.ANIMATIONS[this.position_]);

  this.position_++;
  if (this.position_ > 5) {
    this.position_ = 0;
  }

  this.movable_.classList.add(this.Constant_.ANIMATIONS[this.position_]);
  this.movable_.classList.add(this.CssClasses_.POSITION_PREFIX +
      this.position_);
};

/**
 * Initialize element.
 */
DemoAnimation.prototype.init = function() {
  'use strict';

  if (this.element_) {
    if (!this.movable_) {
      console.error('Was expecting to find an element with class name ' +
          this.CssClasses_.MOVABLE + ' inside of: ', this.element_);
      return;
    }

    this.element_.addEventListener('click', this.handleClick_.bind(this));
  }
};

// The component registers itself. It can assume componentHandler is available
// in the global scope.
componentHandler.register({
  constructor: DemoAnimation,
  classAsString: 'DemoAnimation',
  cssClass: 'demo-js-animation'
});
