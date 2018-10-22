require(["apphost"], function(appHost) {
    "use strict";

    function throttle(key) {
        var time = times[key] || 0;
        return (new Date).getTime() - time >= 200
    }

    function resetThrottle(key) {
        times[key] = (new Date).getTime()
    }

    function allowInput() {
        return !(!isElectron && document.hidden) && "Minimized" !== appHost.getWindowState()
    }

    function raiseEvent(name, key, keyCode) {
        if (allowInput()) {
            var event = document.createEvent("Event");
            event.initEvent(name, !0, !0), event.key = key, event.keyCode = keyCode, (document.activeElement || document.body).dispatchEvent(event)
        }
    }

    function clickElement(elem) {
        allowInput() && elem.click()
    }

    function raiseKeyEvent(oldPressedState, newPressedState, key, keyCode, enableRepeatKeyDown, clickonKeyUp) {
        if (!0 === newPressedState) {
            var fire = !1;
            !1 === oldPressedState ? (fire = !0, resetThrottle(key)) : enableRepeatKeyDown && (fire = throttle(key)), fire && keyCode && raiseEvent("keydown", key, keyCode)
        } else !1 === newPressedState && !0 === oldPressedState && (resetThrottle(key), keyCode && raiseEvent("keyup", key, keyCode), clickonKeyUp && clickElement(document.activeElement || window))
    }

    function runInputLoop() {
        var gamepads;
        navigator.getGamepads ? gamepads = navigator.getGamepads() : navigator.webkitGetGamepads && (gamepads = navigator.webkitGetGamepads()), gamepads = gamepads || [];
        var i, j, len;
        for (i = 0, len = gamepads.length; i < len; i++) {
            var gamepad = gamepads[i];
            if (gamepad) {
                var axes = gamepad.axes,
                    leftStickX = axes[0],
                    leftStickY = axes[1];
                leftStickX > _THUMB_STICK_THRESHOLD ? _ButtonPressedState.setleftThumbstickRight(!0) : leftStickX < -_THUMB_STICK_THRESHOLD ? _ButtonPressedState.setleftThumbstickLeft(!0) : leftStickY < -_THUMB_STICK_THRESHOLD ? _ButtonPressedState.setleftThumbstickUp(!0) : leftStickY > _THUMB_STICK_THRESHOLD ? _ButtonPressedState.setleftThumbstickDown(!0) : (_ButtonPressedState.setleftThumbstickLeft(!1), _ButtonPressedState.setleftThumbstickRight(!1), _ButtonPressedState.setleftThumbstickUp(!1), _ButtonPressedState.setleftThumbstickDown(!1));
                var buttons = gamepad.buttons;
                for (j = 0, len = buttons.length; j < len; j++)
                    if (-1 !== ProcessedButtons.indexOf(j))
                        if (buttons[j].pressed) switch (j) {
                            case _GAMEPAD_DPAD_UP_BUTTON_INDEX:
                                _ButtonPressedState.setdPadUp(!0);
                                break;
                            case _GAMEPAD_DPAD_DOWN_BUTTON_INDEX:
                                _ButtonPressedState.setdPadDown(!0);
                                break;
                            case _GAMEPAD_DPAD_LEFT_BUTTON_INDEX:
                                _ButtonPressedState.setdPadLeft(!0);
                                break;
                            case _GAMEPAD_DPAD_RIGHT_BUTTON_INDEX:
                                _ButtonPressedState.setdPadRight(!0);
                                break;
                            case _GAMEPAD_A_BUTTON_INDEX:
                                _ButtonPressedState.setgamepadA(!0);
                                break;
                            case _GAMEPAD_B_BUTTON_INDEX:
                                _ButtonPressedState.setgamepadB(!0)
                        } else switch (j) {
                            case _GAMEPAD_DPAD_UP_BUTTON_INDEX:
                                _ButtonPressedState.getdPadUp() && _ButtonPressedState.setdPadUp(!1);
                                break;
                            case _GAMEPAD_DPAD_DOWN_BUTTON_INDEX:
                                _ButtonPressedState.getdPadDown() && _ButtonPressedState.setdPadDown(!1);
                                break;
                            case _GAMEPAD_DPAD_LEFT_BUTTON_INDEX:
                                _ButtonPressedState.getdPadLeft() && _ButtonPressedState.setdPadLeft(!1);
                                break;
                            case _GAMEPAD_DPAD_RIGHT_BUTTON_INDEX:
                                _ButtonPressedState.getdPadRight() && _ButtonPressedState.setdPadRight(!1);
                                break;
                            case _GAMEPAD_A_BUTTON_INDEX:
                                _ButtonPressedState.getgamepadA() && _ButtonPressedState.setgamepadA(!1);
                                break;
                            case _GAMEPAD_B_BUTTON_INDEX:
                                _ButtonPressedState.getgamepadB() && _ButtonPressedState.setgamepadB(!1)
                        }
            }
        }
        requestAnimationFrame(runInputLoop)
    }
    var _GAMEPAD_A_BUTTON_INDEX = 0,
        _GAMEPAD_B_BUTTON_INDEX = 1,
        _GAMEPAD_DPAD_UP_BUTTON_INDEX = 12,
        _GAMEPAD_DPAD_DOWN_BUTTON_INDEX = 13,
        _GAMEPAD_DPAD_LEFT_BUTTON_INDEX = 14,
        _GAMEPAD_DPAD_RIGHT_BUTTON_INDEX = 15,
        _THUMB_STICK_THRESHOLD = .75,
        _leftThumbstickUpPressed = !1,
        _leftThumbstickDownPressed = !1,
        _leftThumbstickLeftPressed = !1,
        _leftThumbstickRightPressed = !1,
        _dPadUpPressed = !1,
        _dPadDownPressed = !1,
        _dPadLeftPressed = !1,
        _dPadRightPressed = !1,
        _gamepadAPressed = !1,
        _gamepadBPressed = !1,
        ProcessedButtons = [_GAMEPAD_DPAD_UP_BUTTON_INDEX, _GAMEPAD_DPAD_DOWN_BUTTON_INDEX, _GAMEPAD_DPAD_LEFT_BUTTON_INDEX, _GAMEPAD_DPAD_RIGHT_BUTTON_INDEX, _GAMEPAD_A_BUTTON_INDEX, _GAMEPAD_B_BUTTON_INDEX],
        _ButtonPressedState = {};
    _ButtonPressedState.getgamepadA = function() {
        return _gamepadAPressed
    }, _ButtonPressedState.setgamepadA = function(newPressedState) {
        raiseKeyEvent(_gamepadAPressed, newPressedState, "GamepadA", 0, !1, !0), _gamepadAPressed = newPressedState
    }, _ButtonPressedState.getgamepadB = function() {
        return _gamepadBPressed
    }, _ButtonPressedState.setgamepadB = function(newPressedState) {
        raiseKeyEvent(_gamepadBPressed, newPressedState, "GamepadB", 27), _gamepadBPressed = newPressedState
    }, _ButtonPressedState.getleftThumbstickUp = function() {
        return _leftThumbstickUpPressed
    }, _ButtonPressedState.setleftThumbstickUp = function(newPressedState) {
        raiseKeyEvent(_leftThumbstickUpPressed, newPressedState, "GamepadLeftThumbStickUp", 38, !0), _leftThumbstickUpPressed = newPressedState
    }, _ButtonPressedState.getleftThumbstickDown = function() {
        return _leftThumbstickDownPressed
    }, _ButtonPressedState.setleftThumbstickDown = function(newPressedState) {
        raiseKeyEvent(_leftThumbstickDownPressed, newPressedState, "GamepadLeftThumbStickDown", 40, !0), _leftThumbstickDownPressed = newPressedState
    }, _ButtonPressedState.getleftThumbstickLeft = function() {
        return _leftThumbstickLeftPressed
    }, _ButtonPressedState.setleftThumbstickLeft = function(newPressedState) {
        raiseKeyEvent(_leftThumbstickLeftPressed, newPressedState, "GamepadLeftThumbStickLeft", 37, !0), _leftThumbstickLeftPressed = newPressedState
    }, _ButtonPressedState.getleftThumbstickRight = function() {
        return _leftThumbstickRightPressed
    }, _ButtonPressedState.setleftThumbstickRight = function(newPressedState) {
        raiseKeyEvent(_leftThumbstickRightPressed, newPressedState, "GamepadLeftThumbStickRight", 39, !0), _leftThumbstickRightPressed = newPressedState
    }, _ButtonPressedState.getdPadUp = function() {
        return _dPadUpPressed
    }, _ButtonPressedState.setdPadUp = function(newPressedState) {
        raiseKeyEvent(_dPadUpPressed, newPressedState, "GamepadDPadUp", 38, !0), _dPadUpPressed = newPressedState
    }, _ButtonPressedState.getdPadDown = function() {
        return _dPadDownPressed
    }, _ButtonPressedState.setdPadDown = function(newPressedState) {
        raiseKeyEvent(_dPadDownPressed, newPressedState, "GamepadDPadDown", 40, !0), _dPadDownPressed = newPressedState
    }, _ButtonPressedState.getdPadLeft = function() {
        return _dPadLeftPressed
    }, _ButtonPressedState.setdPadLeft = function(newPressedState) {
        raiseKeyEvent(_dPadLeftPressed, newPressedState, "GamepadDPadLeft", 37, !0), _dPadLeftPressed = newPressedState
    }, _ButtonPressedState.getdPadRight = function() {
        return _dPadRightPressed
    }, _ButtonPressedState.setdPadRight = function(newPressedState) {
        raiseKeyEvent(_dPadRightPressed, newPressedState, "GamepadDPadRight", 39, !0), _dPadRightPressed = newPressedState
    };
    var times = {},
        isElectron = -1 !== navigator.userAgent.toLowerCase().indexOf("electron");
    runInputLoop(), window.navigator && "string" == typeof window.navigator.gamepadInputEmulation && (window.navigator.gamepadInputEmulation = "gamepad")
});