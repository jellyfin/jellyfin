// #      The MIT License (MIT)
// #
// #      Copyright (c) 2016 Microsoft. All rights reserved.
// #
// #      Permission is hereby granted, free of charge, to any person obtaining a copy
// #      of this software and associated documentation files (the "Software"), to deal
// #      in the Software without restriction, including without limitation the rights
// #      to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// #      copies of the Software, and to permit persons to whom the Software is
// #      furnished to do so, subject to the following conditions:
// #
// #      The above copyright notice and this permission notice shall be included in
// #      all copies or substantial portions of the Software.
// #
// #      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// #      IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// #      FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// #      AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// #      LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// #      OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// #      THE SOFTWARE.
(function () {
    "use strict";

    var _GAMEPAD_A_BUTTON_INDEX = 0,
    _GAMEPAD_B_BUTTON_INDEX = 1,
    _GAMEPAD_DPAD_UP_BUTTON_INDEX = 12,
    _GAMEPAD_DPAD_DOWN_BUTTON_INDEX = 13,
    _GAMEPAD_DPAD_LEFT_BUTTON_INDEX = 14,
    _GAMEPAD_DPAD_RIGHT_BUTTON_INDEX = 15,
    _GAMEPAD_A_KEY = "GamepadA",
    _GAMEPAD_B_KEY = "GamepadB",
    _GAMEPAD_DPAD_UP_KEY = "GamepadDPadUp",
    _GAMEPAD_DPAD_DOWN_KEY = "GamepadDPadDown",
    _GAMEPAD_DPAD_LEFT_KEY = "GamepadDPadLeft",
    _GAMEPAD_DPAD_RIGHT_KEY = "GamepadDPadRight",
    _GAMEPAD_LEFT_THUMBSTICK_UP_KEY = "GamepadLeftThumbStickUp",
    _GAMEPAD_LEFT_THUMBSTICK_DOWN_KEY = "GamepadLeftThumbStickDown",
    _GAMEPAD_LEFT_THUMBSTICK_LEFT_KEY = "GamepadLeftThumbStickLeft",
    _GAMEPAD_LEFT_THUMBSTICK_RIGHT_KEY = "GamepadLeftThumbStickRight",
    _GAMEPAD_A_KEYCODE = 0,
    _GAMEPAD_B_KEYCODE = 27,
    _GAMEPAD_DPAD_UP_KEYCODE = 38,
    _GAMEPAD_DPAD_DOWN_KEYCODE = 40,
    _GAMEPAD_DPAD_LEFT_KEYCODE = 37,
    _GAMEPAD_DPAD_RIGHT_KEYCODE = 39,
    _GAMEPAD_LEFT_THUMBSTICK_UP_KEYCODE = 38,
    _GAMEPAD_LEFT_THUMBSTICK_DOWN_KEYCODE = 40,
    _GAMEPAD_LEFT_THUMBSTICK_LEFT_KEYCODE = 37,
    _GAMEPAD_LEFT_THUMBSTICK_RIGHT_KEYCODE = 39,
    _THUMB_STICK_THRESHOLD = 0.75;

    var _leftThumbstickUpPressed = false,
    _leftThumbstickDownPressed = false,
    _leftThumbstickLeftPressed = false,
    _leftThumbstickRightPressed = false,
    _dPadUpPressed = false,
    _dPadDownPressed = false,
    _dPadLeftPressed = false,
    _dPadRightPressed = false,
    _gamepadAPressed = false,
    _gamepadBPressed = false;

    // The set of buttons on the gamepad we listen for.
    var ProcessedButtons = [
        _GAMEPAD_DPAD_UP_BUTTON_INDEX,
        _GAMEPAD_DPAD_DOWN_BUTTON_INDEX,
        _GAMEPAD_DPAD_LEFT_BUTTON_INDEX,
        _GAMEPAD_DPAD_RIGHT_BUTTON_INDEX,
        _GAMEPAD_A_BUTTON_INDEX,
        _GAMEPAD_B_BUTTON_INDEX
    ];

    var _ButtonPressedState = {};
    _ButtonPressedState.getgamepadA = function () {
        return _gamepadAPressed;
    };

    _ButtonPressedState.setgamepadA = function (newPressedState) {
        raiseKeyEvent(_gamepadAPressed, newPressedState, _GAMEPAD_A_KEY, _GAMEPAD_A_KEYCODE, false, true);
        _gamepadAPressed = newPressedState;
    };

    _ButtonPressedState.getgamepadB = function () {
        return _gamepadBPressed;
    };

    _ButtonPressedState.setgamepadB = function (newPressedState) {
        raiseKeyEvent(_gamepadBPressed, newPressedState, _GAMEPAD_B_KEY, _GAMEPAD_B_KEYCODE);
        _gamepadBPressed = newPressedState;
    };

    _ButtonPressedState.getleftThumbstickUp = function () {
        return _leftThumbstickUpPressed;
    };

    _ButtonPressedState.setleftThumbstickUp = function (newPressedState) {
        raiseKeyEvent(_leftThumbstickUpPressed, newPressedState, _GAMEPAD_LEFT_THUMBSTICK_UP_KEY, _GAMEPAD_LEFT_THUMBSTICK_UP_KEYCODE, true);
        _leftThumbstickUpPressed = newPressedState;
    };

    _ButtonPressedState.getleftThumbstickDown = function () {
        return _leftThumbstickDownPressed;
    };

    _ButtonPressedState.setleftThumbstickDown = function (newPressedState) {
        raiseKeyEvent(_leftThumbstickDownPressed, newPressedState, _GAMEPAD_LEFT_THUMBSTICK_DOWN_KEY, _GAMEPAD_LEFT_THUMBSTICK_DOWN_KEYCODE, true);
        _leftThumbstickDownPressed = newPressedState;
    };

    _ButtonPressedState.getleftThumbstickLeft = function () {
        return _leftThumbstickLeftPressed;
    };

    _ButtonPressedState.setleftThumbstickLeft = function (newPressedState) {
        raiseKeyEvent(_leftThumbstickLeftPressed, newPressedState, _GAMEPAD_LEFT_THUMBSTICK_LEFT_KEY, _GAMEPAD_LEFT_THUMBSTICK_LEFT_KEYCODE, true);
        _leftThumbstickLeftPressed = newPressedState;
    };

    _ButtonPressedState.getleftThumbstickRight = function () {
        return _leftThumbstickRightPressed;
    };

    _ButtonPressedState.setleftThumbstickRight = function (newPressedState) {
        raiseKeyEvent(_leftThumbstickRightPressed, newPressedState, _GAMEPAD_LEFT_THUMBSTICK_RIGHT_KEY, _GAMEPAD_LEFT_THUMBSTICK_RIGHT_KEYCODE, true);
        _leftThumbstickRightPressed = newPressedState;
    };

    _ButtonPressedState.getdPadUp = function () {
        return _dPadUpPressed;
    };

    _ButtonPressedState.setdPadUp = function (newPressedState) {
        raiseKeyEvent(_dPadUpPressed, newPressedState, _GAMEPAD_DPAD_UP_KEY, _GAMEPAD_DPAD_UP_KEYCODE, true);
        _dPadUpPressed = newPressedState;
    };

    _ButtonPressedState.getdPadDown = function () {
        return _dPadDownPressed;
    };

    _ButtonPressedState.setdPadDown = function (newPressedState) {
        raiseKeyEvent(_dPadDownPressed, newPressedState, _GAMEPAD_DPAD_DOWN_KEY, _GAMEPAD_DPAD_DOWN_KEYCODE, true);
        _dPadDownPressed = newPressedState;
    };

    _ButtonPressedState.getdPadLeft = function () {
        return _dPadLeftPressed;
    };

    _ButtonPressedState.setdPadLeft = function (newPressedState) {
        raiseKeyEvent(_dPadLeftPressed, newPressedState, _GAMEPAD_DPAD_LEFT_KEY, _GAMEPAD_DPAD_LEFT_KEYCODE, true);
        _dPadLeftPressed = newPressedState;
    };

    _ButtonPressedState.getdPadRight = function () {
        return _dPadRightPressed;
    };

    _ButtonPressedState.setdPadRight = function (newPressedState) {
        raiseKeyEvent(_dPadRightPressed, newPressedState, _GAMEPAD_DPAD_RIGHT_KEY, _GAMEPAD_DPAD_RIGHT_KEYCODE, true);
        _dPadRightPressed = newPressedState;
    };

    var times = {};

    function throttle(key) {
        var time = times[key] || 0;
        var now = new Date().getTime();

        if ((now - time) >= 200) {
            //times[key] = now;
            return true;
        }

        return false;
    }

    function resetThrottle(key) {
        times[key] = new Date().getTime();
    }

    function raiseEvent(name, key, keyCode) {
        var event = document.createEvent('Event');
        event.initEvent(name, true, true);
        event.key = key;
        event.keyCode = keyCode;
        (document.activeElement || document.body).dispatchEvent(event);
    }

    function raiseKeyEvent(oldPressedState, newPressedState, key, keyCode, enableRepeatKeyDown, clickonKeyUp) {

        // No-op if oldPressedState === newPressedState
        if (newPressedState === true) {

            // button down
            var fire = false;

            // always fire if this is the initial down press
            if (oldPressedState === false) {
                fire = true;
                resetThrottle(key);
            } else if (enableRepeatKeyDown) {
                fire = throttle(key);
            }

            if (fire && keyCode) {
                raiseEvent("keydown", key, keyCode);
            }

        } else if (newPressedState === false && oldPressedState === true) {

            resetThrottle(key);

            // button up
            if (keyCode) {
                raiseEvent("keyup", key, keyCode);
            }
            if (clickonKeyUp) {
                (document.activeElement || window).click();
            }
        }
    }

    function runInputLoop() {
        // Get the latest gamepad state.
        var gamepads;
        if (navigator.getGamepads) {
            gamepads = navigator.getGamepads();
        } else if (navigator.webkitGetGamepads) {
            gamepads = navigator.webkitGetGamepads();
        }
        gamepads = gamepads || [];
        var i, j, len;
        for (i = 0, len = gamepads.length; i < len; i++) {
            var gamepad = gamepads[i];
            if (gamepad) {
                // Iterate through the axes
                var axes = gamepad.axes;
                var leftStickX = axes[0];
                var leftStickY = axes[1];
                if (leftStickX > _THUMB_STICK_THRESHOLD) { // Right
                    _ButtonPressedState.setleftThumbstickRight(true);
                } else if (leftStickX < -_THUMB_STICK_THRESHOLD) { // Left
                    _ButtonPressedState.setleftThumbstickLeft(true);
                } else if (leftStickY < -_THUMB_STICK_THRESHOLD) { // Up
                    _ButtonPressedState.setleftThumbstickUp(true);
                } else if (leftStickY > _THUMB_STICK_THRESHOLD) { // Down
                    _ButtonPressedState.setleftThumbstickDown(true);
                } else {
                    _ButtonPressedState.setleftThumbstickLeft(false);
                    _ButtonPressedState.setleftThumbstickRight(false);
                    _ButtonPressedState.setleftThumbstickUp(false);
                    _ButtonPressedState.setleftThumbstickDown(false);
                }
                // Iterate through the buttons to see if Left thumbstick, DPad, A and B are pressed.
                var buttons = gamepad.buttons;
                for (j = 0, len = buttons.length; j < len; j++) {
                    if (ProcessedButtons.indexOf(j) !== -1) {

                        if (buttons[j].pressed) {
                            switch (j) {
                                case _GAMEPAD_DPAD_UP_BUTTON_INDEX:
                                    _ButtonPressedState.setdPadUp(true);
                                    break;
                                case _GAMEPAD_DPAD_DOWN_BUTTON_INDEX:
                                    _ButtonPressedState.setdPadDown(true);
                                    break;
                                case _GAMEPAD_DPAD_LEFT_BUTTON_INDEX:
                                    _ButtonPressedState.setdPadLeft(true);
                                    break;
                                case _GAMEPAD_DPAD_RIGHT_BUTTON_INDEX:
                                    _ButtonPressedState.setdPadRight(true);
                                    break;
                                case _GAMEPAD_A_BUTTON_INDEX:
                                    _ButtonPressedState.setgamepadA(true);
                                    break;
                                case _GAMEPAD_B_BUTTON_INDEX:
                                    _ButtonPressedState.setgamepadB(true);
                                    break;
                                default:
                                    // No-op
                                    break;
                            }
                        } else {
                            switch (j) {
                                case _GAMEPAD_DPAD_UP_BUTTON_INDEX:
                                    if (_ButtonPressedState.getdPadUp()) {
                                        _ButtonPressedState.setdPadUp(false);
                                    }
                                    break;
                                case _GAMEPAD_DPAD_DOWN_BUTTON_INDEX:
                                    if (_ButtonPressedState.getdPadDown()) {
                                        _ButtonPressedState.setdPadDown(false);
                                    }
                                    break;
                                case _GAMEPAD_DPAD_LEFT_BUTTON_INDEX:
                                    if (_ButtonPressedState.getdPadLeft()) {
                                        _ButtonPressedState.setdPadLeft(false);
                                    }
                                    break;
                                case _GAMEPAD_DPAD_RIGHT_BUTTON_INDEX:
                                    if (_ButtonPressedState.getdPadRight()) {
                                        _ButtonPressedState.setdPadRight(false);
                                    }
                                    break;
                                case _GAMEPAD_A_BUTTON_INDEX:
                                    if (_ButtonPressedState.getgamepadA()) {
                                        _ButtonPressedState.setgamepadA(false);
                                    }
                                    break;
                                case _GAMEPAD_B_BUTTON_INDEX:
                                    if (_ButtonPressedState.getgamepadB()) {
                                        _ButtonPressedState.setgamepadB(false);
                                    }
                                    break;
                                default:
                                    // No-op
                                    break;
                            }
                        }
                    }
                }
            }
        }
        // Schedule the next one
        requestAnimationFrame(runInputLoop);
    }

    runInputLoop();

    // The gamepadInputEmulation is a string property that exists in JavaScript UWAs and in WebViews in UWAs.
    // It won't exist in Win8.1 style apps or browsers.
    if (window.navigator && typeof window.navigator.gamepadInputEmulation === "string") {
        // We want the gamepad to provide gamepad VK keyboard events rather than moving a
        // mouse like cursor. Set to "keyboard", the gamepad will provide such keyboard events
        // and provide input to the DOM navigator.getGamepads API.
        window.navigator.gamepadInputEmulation = "gamepad";
    }

})();