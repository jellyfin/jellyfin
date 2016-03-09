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

(function() {
  'use strict';

  /**
   * Class constructor for icon toggle MDL component.
   * Implements MDL component design pattern defined at:
   * https://github.com/jasonmayes/mdl-component-design-pattern
   *
   * @constructor
   * @param {HTMLElement} element The element that will be upgraded.
   */
  var MaterialIconToggle = function MaterialIconToggle(element) {
    this.element_ = element;

    // Initialize instance.
    this.init();
  };
  window['MaterialIconToggle'] = MaterialIconToggle;

  /**
   * Store constants in one place so they can be updated easily.
   *
   * @enum {string | number}
   * @private
   */
  MaterialIconToggle.prototype.Constant_ = {
    TINY_TIMEOUT: 0.001
  };

  /**
   * Store strings for class names defined by this component that are used in
   * JavaScript. This allows us to simply change it in one place should we
   * decide to modify at a later date.
   *
   * @enum {string}
   * @private
   */
  MaterialIconToggle.prototype.CssClasses_ = {
    INPUT: 'mdl-icon-toggle__input',
    JS_RIPPLE_EFFECT: 'mdl-js-ripple-effect',
    RIPPLE_IGNORE_EVENTS: 'mdl-js-ripple-effect--ignore-events',
    RIPPLE_CONTAINER: 'mdl-icon-toggle__ripple-container',
    RIPPLE_CENTER: 'mdl-ripple--center',
    RIPPLE: 'mdl-ripple',
    IS_FOCUSED: 'is-focused',
    IS_DISABLED: 'is-disabled',
    IS_CHECKED: 'is-checked'
  };

  /**
   * Handle change of state.
   *
   * @param {Event} event The event that fired.
   * @private
   */
  MaterialIconToggle.prototype.onChange_ = function(event) {
    this.updateClasses_();
  };

  /**
   * Handle focus of element.
   *
   * @param {Event} event The event that fired.
   * @private
   */
  MaterialIconToggle.prototype.onFocus_ = function(event) {
    this.element_.classList.add(this.CssClasses_.IS_FOCUSED);
  };

  /**
   * Handle lost focus of element.
   *
   * @param {Event} event The event that fired.
   * @private
   */
  MaterialIconToggle.prototype.onBlur_ = function(event) {
    this.element_.classList.remove(this.CssClasses_.IS_FOCUSED);
  };

  /**
   * Handle mouseup.
   *
   * @param {Event} event The event that fired.
   * @private
   */
  MaterialIconToggle.prototype.onMouseUp_ = function(event) {
    this.blur_();
  };

  /**
   * Handle class updates.
   *
   * @private
   */
  MaterialIconToggle.prototype.updateClasses_ = function() {
    this.checkDisabled();
    this.checkToggleState();
  };

  /**
   * Add blur.
   *
   * @private
   */
  MaterialIconToggle.prototype.blur_ = function() {
    // TODO: figure out why there's a focus event being fired after our blur,
    // so that we can avoid this hack.
    window.setTimeout(function() {
      this.inputElement_.blur();
    }.bind(this), /** @type {number} */ (this.Constant_.TINY_TIMEOUT));
  };

  // Public methods.

  /**
   * Check the inputs toggle state and update display.
   *
   * @public
   */
  MaterialIconToggle.prototype.checkToggleState = function() {
    if (this.inputElement_.checked) {
      this.element_.classList.add(this.CssClasses_.IS_CHECKED);
    } else {
      this.element_.classList.remove(this.CssClasses_.IS_CHECKED);
    }
  };
  MaterialIconToggle.prototype['checkToggleState'] =
      MaterialIconToggle.prototype.checkToggleState;

  /**
   * Check the inputs disabled state and update display.
   *
   * @public
   */
  MaterialIconToggle.prototype.checkDisabled = function() {
    if (this.inputElement_.disabled) {
      this.element_.classList.add(this.CssClasses_.IS_DISABLED);
    } else {
      this.element_.classList.remove(this.CssClasses_.IS_DISABLED);
    }
  };
  MaterialIconToggle.prototype['checkDisabled'] =
      MaterialIconToggle.prototype.checkDisabled;

  /**
   * Disable icon toggle.
   *
   * @public
   */
  MaterialIconToggle.prototype.disable = function() {
    this.inputElement_.disabled = true;
    this.updateClasses_();
  };
  MaterialIconToggle.prototype['disable'] =
      MaterialIconToggle.prototype.disable;

  /**
   * Enable icon toggle.
   *
   * @public
   */
  MaterialIconToggle.prototype.enable = function() {
    this.inputElement_.disabled = false;
    this.updateClasses_();
  };
  MaterialIconToggle.prototype['enable'] = MaterialIconToggle.prototype.enable;

  /**
   * Check icon toggle.
   *
   * @public
   */
  MaterialIconToggle.prototype.check = function() {
    this.inputElement_.checked = true;
    this.updateClasses_();
  };
  MaterialIconToggle.prototype['check'] = MaterialIconToggle.prototype.check;

  /**
   * Uncheck icon toggle.
   *
   * @public
   */
  MaterialIconToggle.prototype.uncheck = function() {
    this.inputElement_.checked = false;
    this.updateClasses_();
  };
  MaterialIconToggle.prototype['uncheck'] =
      MaterialIconToggle.prototype.uncheck;

  /**
   * Initialize element.
   */
  MaterialIconToggle.prototype.init = function() {

    if (this.element_) {
      this.inputElement_ =
          this.element_.querySelector('.' + this.CssClasses_.INPUT);

      if (this.element_.classList.contains(this.CssClasses_.JS_RIPPLE_EFFECT)) {
        this.element_.classList.add(this.CssClasses_.RIPPLE_IGNORE_EVENTS);
        this.rippleContainerElement_ = document.createElement('span');
        this.rippleContainerElement_.classList.add(this.CssClasses_.RIPPLE_CONTAINER);
        this.rippleContainerElement_.classList.add(this.CssClasses_.JS_RIPPLE_EFFECT);
        this.rippleContainerElement_.classList.add(this.CssClasses_.RIPPLE_CENTER);
        this.boundRippleMouseUp = this.onMouseUp_.bind(this);
        this.rippleContainerElement_.addEventListener('mouseup', this.boundRippleMouseUp);

        var ripple = document.createElement('span');
        ripple.classList.add(this.CssClasses_.RIPPLE);

        this.rippleContainerElement_.appendChild(ripple);
        this.element_.appendChild(this.rippleContainerElement_);
      }

      this.boundInputOnChange = this.onChange_.bind(this);
      this.boundInputOnFocus = this.onFocus_.bind(this);
      this.boundInputOnBlur = this.onBlur_.bind(this);
      this.boundElementOnMouseUp = this.onMouseUp_.bind(this);
      this.inputElement_.addEventListener('change', this.boundInputOnChange);
      this.inputElement_.addEventListener('focus', this.boundInputOnFocus);
      this.inputElement_.addEventListener('blur', this.boundInputOnBlur);
      this.element_.addEventListener('mouseup', this.boundElementOnMouseUp);

      this.updateClasses_();
      this.element_.classList.add('is-upgraded');
    }
  };

  // The component registers itself. It can assume componentHandler is available
  // in the global scope.
  componentHandler.register({
    constructor: MaterialIconToggle,
    classAsString: 'MaterialIconToggle',
    cssClass: 'mdl-js-icon-toggle',
    widget: true
  });
})();
