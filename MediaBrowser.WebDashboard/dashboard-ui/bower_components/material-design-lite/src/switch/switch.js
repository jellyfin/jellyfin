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
   * Class constructor for Checkbox MDL component.
   * Implements MDL component design pattern defined at:
   * https://github.com/jasonmayes/mdl-component-design-pattern
   *
   * @constructor
   * @param {HTMLElement} element The element that will be upgraded.
   */
  var MaterialSwitch = function MaterialSwitch(element) {
    this.element_ = element;

    // Initialize instance.
    this.init();
  };
  window['MaterialSwitch'] = MaterialSwitch;

  /**
   * Store constants in one place so they can be updated easily.
   *
   * @enum {string | number}
   * @private
   */
  MaterialSwitch.prototype.Constant_ = {
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
  MaterialSwitch.prototype.CssClasses_ = {
    INPUT: 'mdl-switch__input',
    TRACK: 'mdl-switch__track',
    THUMB: 'mdl-switch__thumb',
    FOCUS_HELPER: 'mdl-switch__focus-helper',
    RIPPLE_EFFECT: 'mdl-js-ripple-effect',
    RIPPLE_IGNORE_EVENTS: 'mdl-js-ripple-effect--ignore-events',
    RIPPLE_CONTAINER: 'mdl-switch__ripple-container',
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
  MaterialSwitch.prototype.onChange_ = function(event) {
    this.updateClasses_();
  };

  /**
   * Handle focus of element.
   *
   * @param {Event} event The event that fired.
   * @private
   */
  MaterialSwitch.prototype.onFocus_ = function(event) {
    this.element_.classList.add(this.CssClasses_.IS_FOCUSED);
  };

  /**
   * Handle lost focus of element.
   *
   * @param {Event} event The event that fired.
   * @private
   */
  MaterialSwitch.prototype.onBlur_ = function(event) {
    this.element_.classList.remove(this.CssClasses_.IS_FOCUSED);
  };

  /**
   * Handle mouseup.
   *
   * @param {Event} event The event that fired.
   * @private
   */
  MaterialSwitch.prototype.onMouseUp_ = function(event) {
    this.blur_();
  };

  /**
   * Handle class updates.
   *
   * @private
   */
  MaterialSwitch.prototype.updateClasses_ = function() {
    this.checkDisabled();
    this.checkToggleState();
  };

  /**
   * Add blur.
   *
   * @private
   */
  MaterialSwitch.prototype.blur_ = function() {
    // TODO: figure out why there's a focus event being fired after our blur,
    // so that we can avoid this hack.
    window.setTimeout(function() {
      this.inputElement_.blur();
    }.bind(this), /** @type {number} */ (this.Constant_.TINY_TIMEOUT));
  };

  // Public methods.

  /**
   * Check the components disabled state.
   *
   * @public
   */
  MaterialSwitch.prototype.checkDisabled = function() {
    if (this.inputElement_.disabled) {
      this.element_.classList.add(this.CssClasses_.IS_DISABLED);
    } else {
      this.element_.classList.remove(this.CssClasses_.IS_DISABLED);
    }
  };
  MaterialSwitch.prototype['checkDisabled'] =
      MaterialSwitch.prototype.checkDisabled;

  /**
   * Check the components toggled state.
   *
   * @public
   */
  MaterialSwitch.prototype.checkToggleState = function() {
    if (this.inputElement_.checked) {
      this.element_.classList.add(this.CssClasses_.IS_CHECKED);
    } else {
      this.element_.classList.remove(this.CssClasses_.IS_CHECKED);
    }
  };
  MaterialSwitch.prototype['checkToggleState'] =
      MaterialSwitch.prototype.checkToggleState;

  /**
   * Disable switch.
   *
   * @public
   */
  MaterialSwitch.prototype.disable = function() {
    this.inputElement_.disabled = true;
    this.updateClasses_();
  };
  MaterialSwitch.prototype['disable'] = MaterialSwitch.prototype.disable;

  /**
   * Enable switch.
   *
   * @public
   */
  MaterialSwitch.prototype.enable = function() {
    this.inputElement_.disabled = false;
    this.updateClasses_();
  };
  MaterialSwitch.prototype['enable'] = MaterialSwitch.prototype.enable;

  /**
   * Activate switch.
   *
   * @public
   */
  MaterialSwitch.prototype.on = function() {
    this.inputElement_.checked = true;
    this.updateClasses_();
  };
  MaterialSwitch.prototype['on'] = MaterialSwitch.prototype.on;

  /**
   * Deactivate switch.
   *
   * @public
   */
  MaterialSwitch.prototype.off = function() {
    this.inputElement_.checked = false;
    this.updateClasses_();
  };
  MaterialSwitch.prototype['off'] = MaterialSwitch.prototype.off;

  /**
   * Initialize element.
   */
  MaterialSwitch.prototype.init = function() {
    if (this.element_) {
      this.inputElement_ = this.element_.querySelector('.' +
          this.CssClasses_.INPUT);

      var track = document.createElement('div');
      track.classList.add(this.CssClasses_.TRACK);

      var thumb = document.createElement('div');
      thumb.classList.add(this.CssClasses_.THUMB);

      var focusHelper = document.createElement('span');
      focusHelper.classList.add(this.CssClasses_.FOCUS_HELPER);

      thumb.appendChild(focusHelper);

      this.element_.appendChild(track);
      this.element_.appendChild(thumb);

      this.boundMouseUpHandler = this.onMouseUp_.bind(this);

      if (this.element_.classList.contains(
          this.CssClasses_.RIPPLE_EFFECT)) {
        this.element_.classList.add(
            this.CssClasses_.RIPPLE_IGNORE_EVENTS);
        this.rippleContainerElement_ = document.createElement('span');
        this.rippleContainerElement_.classList.add(
            this.CssClasses_.RIPPLE_CONTAINER);
        this.rippleContainerElement_.classList.add(this.CssClasses_.RIPPLE_EFFECT);
        this.rippleContainerElement_.classList.add(this.CssClasses_.RIPPLE_CENTER);
        this.rippleContainerElement_.addEventListener('mouseup', this.boundMouseUpHandler);

        var ripple = document.createElement('span');
        ripple.classList.add(this.CssClasses_.RIPPLE);

        this.rippleContainerElement_.appendChild(ripple);
        this.element_.appendChild(this.rippleContainerElement_);
      }

      this.boundChangeHandler = this.onChange_.bind(this);
      this.boundFocusHandler = this.onFocus_.bind(this);
      this.boundBlurHandler = this.onBlur_.bind(this);

      this.inputElement_.addEventListener('change', this.boundChangeHandler);
      this.inputElement_.addEventListener('focus', this.boundFocusHandler);
      this.inputElement_.addEventListener('blur', this.boundBlurHandler);
      this.element_.addEventListener('mouseup', this.boundMouseUpHandler);

      this.updateClasses_();
      this.element_.classList.add('is-upgraded');
    }
  };

  // The component registers itself. It can assume componentHandler is available
  // in the global scope.
  componentHandler.register({
    constructor: MaterialSwitch,
    classAsString: 'MaterialSwitch',
    cssClass: 'mdl-js-switch',
    widget: true
  });
})();
