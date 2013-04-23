/*!
Video.js - HTML5 Video Player
Version 3.2.0

LGPL v3 LICENSE INFO
This file is part of Video.js. Copyright 2011 Zencoder, Inc.

Video.js is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Video.js is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with Video.js.  If not, see <http://www.gnu.org/licenses/>.
*/

// Self-executing function to prevent global vars and help with minification
;(function(window, undefined){
  var document = window.document;// HTML5 Shiv. Must be in <head> to support older browsers.
document.createElement("video");document.createElement("audio");

var VideoJS = function(id, addOptions, ready){
  var tag; // Element of ID

  // Allow for element or ID to be passed in
  // String ID
  if (typeof id == "string") {

    // Adjust for jQuery ID syntax
    if (id.indexOf("#") === 0) {
      id = id.slice(1);
    }

    // If a player instance has already been created for this ID return it.
    if (_V_.players[id]) {
      return _V_.players[id];

    // Otherwise get element for ID
    } else {
      tag = _V_.el(id)
    }

  // ID is a media element
  } else {
    tag = id;
  }

  // Check for a useable element
  if (!tag || !tag.nodeName) { // re: nodeName, could be a box div also
    throw new TypeError("The element or ID supplied is not valid. (VideoJS)"); // Returns
  }

  // Element may have a player attr referring to an already created player instance.
  // If not, set up a new player and return the instance.
  return tag.player || new _V_.Player(tag, addOptions, ready);
},

// Shortcut
_V_ = VideoJS,

// CDN Version. Used to target right flash swf.
CDN_VERSION = "3.2";

VideoJS.players = {};

VideoJS.options = {

  // Default order of fallback technology
  techOrder: ["html5","flash"],
  // techOrder: ["flash","html5"],

  html5: {},
  flash: { swf: "http://vjs.zencdn.net/c/video-js.swf" },

  // Default of web browser is 300x150. Should rely on source width/height.
  width: "auto",
  height: "auto",
  
  // defaultVolume: 0.85,
  defaultVolume: 0.00, // The freakin seaguls are driving me crazy!

  // Included control sets
  components: {
    "posterImage": {},
    "textTrackDisplay": {},
    "loadingSpinner": {},
    "bigPlayButton": {},
    "controlBar": {}
  }

  // components: [
  //   "poster",
  //   "loadingSpinner",
  //   "bigPlayButton",
  //   { name: "controlBar", options: {
  //     components: [
  //       "playToggle",
  //       "fullscreenToggle",
  //       "currentTimeDisplay",
  //       "timeDivider",
  //       "durationDisplay",
  //       "remainingTimeDisplay",
  //       { name: "progressControl", options: {
  //         components: [
  //           { name: "seekBar", options: {
  //             components: [
  //               "loadProgressBar",
  //               "playProgressBar",
  //               "seekHandle"
  //             ]}
  //           }
  //         ]}
  //       },
  //       { name: "volumeControl", options: {
  //         components: [
  //           { name: "volumeBar", options: {
  //             components: [
  //               "volumeLevel",
  //               "volumeHandle"
  //             ]}
  //           }
  //         ]}
  //       },
  //       "muteToggle"
  //     ]
  //   }},
  //   "subtitlesDisplay"/*, "replay"*/
  // ]
};

// Set CDN Version of swf
if (CDN_VERSION != "GENERATED_CDN_VSN") {
  _V_.options.flash.swf = "http://vjs.zencdn.net/"+CDN_VERSION+"/video-js.swf"
}_V_.merge = function(obj1, obj2, safe){
  // Make sure second object exists
  if (!obj2) { obj2 = {}; };

  for (var attrname in obj2){
    if (obj2.hasOwnProperty(attrname) && (!safe || !obj1.hasOwnProperty(attrname))) { obj1[attrname]=obj2[attrname]; }
  }
  return obj1;
};
_V_.extend = function(obj){ this.merge(this, obj, true); };

_V_.extend({
  tech: {}, // Holder for playback technology settings
  controlSets: {}, // Holder for control set definitions

  // Device Checks
  isIE: function(){ return !+"\v1"; },
  isFF: function(){ return !!_V_.ua.match("Firefox") },
  isIPad: function(){ return navigator.userAgent.match(/iPad/i) !== null; },
  isIPhone: function(){ return navigator.userAgent.match(/iPhone/i) !== null; },
  isIOS: function(){ return VideoJS.isIPhone() || VideoJS.isIPad(); },
  iOSVersion: function() {
    var match = navigator.userAgent.match(/OS (\d+)_/i);
    if (match && match[1]) { return match[1]; }
  },
  isAndroid: function(){ return navigator.userAgent.match(/Android.*AppleWebKit/i) !== null; },
  androidVersion: function() {
    var match = navigator.userAgent.match(/Android (\d+)\./i);
    if (match && match[1]) { return match[1]; }
  },

  testVid: document.createElement("video"),
  ua: navigator.userAgent,
  support: {},

  each: function(arr, fn){
    if (!arr || arr.length === 0) { return; }
    for (var i=0,j=arr.length; i<j; i++) {
      fn.call(this, arr[i], i);
    }
  },

  eachProp: function(obj, fn){
    if (!obj) { return; }
    for (var name in obj) {
      if (obj.hasOwnProperty(name)) {
        fn.call(this, name, obj[name]);
      }
    }
  },

  el: function(id){ return document.getElementById(id); },
  createElement: function(tagName, attributes){
    var el = document.createElement(tagName),
        attrname;
    for (attrname in attributes){
      if (attributes.hasOwnProperty(attrname)) {
        if (attrname.indexOf("-") !== -1) {
          el.setAttribute(attrname, attributes[attrname]);
        } else {
          el[attrname] = attributes[attrname];
        }
      }
    }
    return el;
  },

  insertFirst: function(node, parent){
    if (parent.firstChild) {
      parent.insertBefore(node, parent.firstChild);
    } else {
      parent.appendChild(node);
    }
  },

  addClass: function(element, classToAdd){
    if ((" "+element.className+" ").indexOf(" "+classToAdd+" ") == -1) {
      element.className = element.className === "" ? classToAdd : element.className + " " + classToAdd;
    }
  },

  removeClass: function(element, classToRemove){
    if (element.className.indexOf(classToRemove) == -1) { return; }
    var classNames = element.className.split(" ");
    classNames.splice(classNames.indexOf(classToRemove),1);
    element.className = classNames.join(" ");
  },
  
  remove: function(item, array){
    if (!array) return;
    var i = array.indexOf(item);
    if (i != -1) { 
      return array.splice(i, 1)
    };
  },

  // Attempt to block the ability to select text while dragging controls
  blockTextSelection: function(){
    document.body.focus();
    document.onselectstart = function () { return false; };
  },
  // Turn off text selection blocking
  unblockTextSelection: function(){ document.onselectstart = function () { return true; }; },

  // Return seconds as H:MM:SS or M:SS
  // Supplying a guide (in seconds) will include enough leading zeros to cover the length of the guide
  formatTime: function(seconds, guide) {
    guide = guide || seconds; // Default to using seconds as guide
    var s = Math.floor(seconds % 60),
        m = Math.floor(seconds / 60 % 60),
        h = Math.floor(seconds / 3600),
        gm = Math.floor(guide / 60 % 60),
        gh = Math.floor(guide / 3600);

    // Check if we need to show hours
    h = (h > 0 || gh > 0) ? h + ":" : "";

    // If hours are showing, we may need to add a leading zero.
    // Always show at least one digit of minutes.
    m = (((h || gm >= 10) && m < 10) ? "0" + m : m) + ":";

    // Check if leading zero is need for seconds
    s = (s < 10) ? "0" + s : s;

    return h + m + s;
  },

  uc: function(string){
    return string.charAt(0).toUpperCase() + string.slice(1);
  },

  // Return the relative horizonal position of an event as a value from 0-1
  getRelativePosition: function(x, relativeElement){
    return Math.max(0, Math.min(1, (x - _V_.findPosX(relativeElement)) / relativeElement.offsetWidth));
  },
  
  getComputedStyleValue: function(element, style){
    return window.getComputedStyle(element, null).getPropertyValue(style);
  },

  trim: function(string){ return string.toString().replace(/^\s+/, "").replace(/\s+$/, ""); },
  round: function(num, dec) {
    if (!dec) { dec = 0; }
    return Math.round(num*Math.pow(10,dec))/Math.pow(10,dec);
  },

  isEmpty: function(object) {
    for (var prop in object) {
      return false;
    }
    return true;
  },

  // Mimic HTML5 TimeRange Spec.
  createTimeRange: function(start, end){
    return {
      length: 1,
      start: function() { return start; },
      end: function() { return end; }
    };
  },

  /* Element Data Store. Allows for binding data to an element without putting it directly on the element.
     Ex. Event listneres are stored here.
     (also from jsninja.com)
  ================================================================================ */
  cache: {}, // Where the data is stored
  guid: 1, // Unique ID for the element
  expando: "vdata" + (new Date).getTime(), // Unique attribute to store element's guid in

  // Returns the cache object where data for the element is stored
  getData: function(elem){
    var id = elem[_V_.expando];
    if (!id) {
      id = elem[_V_.expando] = _V_.guid++;
      _V_.cache[id] = {};
    }
    return _V_.cache[id];
  },
  // Delete data for the element from the cache and the guid attr from element
  removeData: function(elem){
    var id = elem[_V_.expando];
    if (!id) { return; }
    // Remove all stored data
    delete _V_.cache[id];
    // Remove the expando property from the DOM node
    try {
      delete elem[_V_.expando];
    } catch(e) {
      if (elem.removeAttribute) {
        elem.removeAttribute(_V_.expando);
      } else {
        // IE doesn't appear to support removeAttribute on the document element
        elem[_V_.expando] = null;
      }
    }
  },

  /* Proxy (a.k.a Bind or Context). A simple method for changing the context of a function
     It also stores a unique id on the function so it can be easily removed from events
  ================================================================================ */
  proxy: function(context, fn, uid) {
    // Make sure the function has a unique ID
    if (!fn.guid) { fn.guid = _V_.guid++; }

    // Create the new function that changes the context
    var ret = function() {
      return fn.apply(context, arguments);
    }

    // Allow for the ability to individualize this function
    // Needed in the case where multiple objects might share the same prototype
    // IF both items add an event listener with the same function, then you try to remove just one
    // it will remove both because they both have the same guid.
    // when using this, you need to use the proxy method when you remove the listener as well.
    ret.guid = (uid) ? uid + "_" + fn.guid : fn.guid;

    return ret;
  },

  get: function(url, onSuccess, onError){
    // if (netscape.security.PrivilegeManager.enablePrivilege) {
    //   netscape.security.PrivilegeManager.enablePrivilege("UniversalBrowserRead");
    // }

    var local = (url.indexOf("file:") == 0 || (window.location.href.indexOf("file:") == 0 && url.indexOf("http:") == -1));

    if (typeof XMLHttpRequest == "undefined") {
      XMLHttpRequest = function () {
        try { return new ActiveXObject("Msxml2.XMLHTTP.6.0"); } catch (e) {}
        try { return new ActiveXObject("Msxml2.XMLHTTP.3.0"); } catch (f) {}
        try { return new ActiveXObject("Msxml2.XMLHTTP"); } catch (g) {}
        throw new Error("This browser does not support XMLHttpRequest.");
      };
    }

    var request = new XMLHttpRequest();

    try {
      request.open("GET", url);
    } catch(e) {
      _V_.log("VideoJS XMLHttpRequest (open)", e);
      // onError(e);
      return false;
    }

    request.onreadystatechange = _V_.proxy(this, function() {
      if (request.readyState == 4) {
        if (request.status == 200 || local && request.status == 0) {
          onSuccess(request.responseText);
        } else {
          if (onError) {
            onError();
          }
        }
      }
    });

    try {
      request.send();
    } catch(e) {
      _V_.log("VideoJS XMLHttpRequest (send)", e);
      if (onError) {
        onError(e);
      }
    }
  },

  /* Local Storage
  ================================================================================ */
  setLocalStorage: function(key, value){
    // IE was throwing errors referencing the var anywhere without this
    var localStorage = window.localStorage || false;
    if (!localStorage) { return; }
    try {
      localStorage[key] = value;
    } catch(e) {
      if (e.code == 22 || e.code == 1014) { // Webkit == 22 / Firefox == 1014
        _V_.log("LocalStorage Full (VideoJS)", e);
      } else {
        _V_.log("LocalStorage Error (VideoJS)", e);
      }
    }
  },

  // Get abosolute version of relative URL. Used to tell flash correct URL.
  // http://stackoverflow.com/questions/470832/getting-an-absolute-url-from-a-relative-one-ie6-issue
  getAbsoluteURL: function(url){

    // Check if absolute URL
    if (!url.match(/^https?:\/\//)) {
      // Convert to absolute URL. Flash hosted off-site needs an absolute URL.
      url = _V_.createElement('div', {
        innerHTML: '<a href="'+url+'">x</a>'
      }).firstChild.href;
    }

    return url;
  }

});

// usage: log('inside coolFunc', this, arguments);
// paulirish.com/2009/log-a-lightweight-wrapper-for-consolelog/
_V_.log = function(){
  _V_.log.history = _V_.log.history || [];// store logs to an array for reference
  _V_.log.history.push(arguments);
  if(window.console) {
    arguments.callee = arguments.callee.caller;
    var newarr = [].slice.call(arguments);
    (typeof console.log === 'object' ? _V_.log.apply.call(console.log, console, newarr) : console.log.apply(console, newarr));
  }
};

// make it safe to use console.log always
(function(b){function c(){}for(var d="assert,count,debug,dir,dirxml,error,exception,group,groupCollapsed,groupEnd,info,log,timeStamp,profile,profileEnd,time,timeEnd,trace,warn".split(","),a;a=d.pop();){b[a]=b[a]||c}})((function(){try
{console.log();return window.console;}catch(err){return window.console={};}})());

// Offset Left
// getBoundingClientRect technique from John Resig http://ejohn.org/blog/getboundingclientrect-is-awesome/
if ("getBoundingClientRect" in document.documentElement) {
  _V_.findPosX = function(el) {
    var box;

    try {
      box = el.getBoundingClientRect();
    } catch(e) {}

    if (!box) { return 0; }

    var docEl = document.documentElement,
        body = document.body,
        clientLeft = docEl.clientLeft || body.clientLeft || 0,
        scrollLeft = window.pageXOffset || body.scrollLeft,
        left = box.left + scrollLeft - clientLeft;

    return left;
  };
} else {
  _V_.findPosX = function(el) {
    var curleft = el.offsetLeft;
    // _V_.log(obj.className, obj.offsetLeft)
    while(el = obj.offsetParent) {
      if (el.className.indexOf("video-js") == -1) {
        // _V_.log(el.offsetParent, "OFFSETLEFT", el.offsetLeft)
        // _V_.log("-webkit-full-screen", el.webkitMatchesSelector("-webkit-full-screen"));
        // _V_.log("-webkit-full-screen", el.querySelectorAll(".video-js:-webkit-full-screen"));
      } else {
      }
      curleft += el.offsetLeft;
    }
    return curleft;
  };
}// Using John Resig's Class implementation http://ejohn.org/blog/simple-javascript-inheritance/
// (function(){var initializing=false, fnTest=/xyz/.test(function(){xyz;}) ? /\b_super\b/ : /.*/; _V_.Class = function(){}; _V_.Class.extend = function(prop) { var _super = this.prototype; initializing = true; var prototype = new this(); initializing = false; for (var name in prop) { prototype[name] = typeof prop[name] == "function" && typeof _super[name] == "function" && fnTest.test(prop[name]) ? (function(name, fn){ return function() { var tmp = this._super; this._super = _super[name]; var ret = fn.apply(this, arguments); this._super = tmp; return ret; }; })(name, prop[name]) : prop[name]; } function Class() { if ( !initializing && this.init ) this.init.apply(this, arguments); } Class.prototype = prototype; Class.constructor = Class; Class.extend = arguments.callee; return Class;};})();
(function(){
  var initializing = false, fnTest = /xyz/.test(function(){xyz;}) ? /\b_super\b/ : /.*/;
  _V_.Class = function(){};
  _V_.Class.extend = function(prop) {
    var _super = this.prototype;
    initializing = true;
    var prototype = new this();
    initializing = false;
    for (var name in prop) {
      prototype[name] = typeof prop[name] == "function" &&
        typeof _super[name] == "function" && fnTest.test(prop[name]) ?
        (function(name, fn){
          return function() {
            var tmp = this._super;
            this._super = _super[name];
            var ret = fn.apply(this, arguments);
            this._super = tmp;
            return ret;
          };
        })(name, prop[name]) :
        prop[name];
    }
    function Class() {
      if ( !initializing && this.init ) {
        return this.init.apply(this, arguments);

      // Attempting to recreate accessing function form of class.
      } else if (!initializing) {
        return arguments.callee.prototype.init()
      }
    }
    Class.prototype = prototype;
    Class.constructor = Class;
    Class.extend = arguments.callee;
    return Class;
  };
})();

/* Player Component- Base class for all UI objects
================================================================================ */
_V_.Component = _V_.Class.extend({

  init: function(player, options){
    this.player = player;

    // Allow for overridding default component options
    options = this.options = _V_.merge(this.options || {}, options);

    // Create element if one wasn't provided in options
    if (options.el) {
      this.el = options.el;
    } else {
      this.el = this.createElement();
    }

    // Add any components in options
    this.initComponents();
  },

  destroy: function(){},

  createElement: function(type, attrs){
    return _V_.createElement(type || "div", attrs);
  },

  buildCSSClass: function(){
    // Child classes can include a function that does:
    // return "CLASS NAME" + this._super();
    return "";
  },

  initComponents: function(){
    var options = this.options;
    if (options && options.components) {
      // Loop through components and add them to the player
      this.eachProp(options.components, function(name, opts){

        // Allow waiting to add components until a specific event is called
        var tempAdd = this.proxy(function(){
          // Set property name on player. Could cause conflicts with other prop names, but it's worth making refs easy.
          this[name] = this.addComponent(name, opts);
        });

        if (opts.loadEvent) {
          this.one(opts.loadEvent, tempAdd)
        } else {
          tempAdd();
        }
      });
    }
  },

  // Add child components to this component.
  // Will generate a new child component and then append child component's element to this component's element.
  // Takes either the name of the UI component class, or an object that contains a name, UI Class, and options.
  addComponent: function(name, options){
    var component, componentClass;

    // If string, create new component with options
    if (typeof name == "string") {

      // Make sure options is at least an empty object to protect against errors
      options = options || {};

      // Assume name of set is a lowercased name of the UI Class (PlayButton, etc.)
      componentClass = options.componentClass || _V_.uc(name);

      // Create a new object & element for this controls set
      // If there's no .player, this is a player
      component = new _V_[componentClass](this.player || this, options);

    } else {
      component = name;
    }

    // Add the UI object's element to the container div (box)
    this.el.appendChild(component.el);

    // Return so it can stored on parent object if desired.
    return component;
  },

  removeComponent: function(component){
    this.el.removeChild(component.el);
  },

  /* Display
  ================================================================================ */
  show: function(){
    this.el.style.display = "block";
  },

  hide: function(){
    this.el.style.display = "none";
  },
  
  fadeIn: function(){
    this.removeClass("vjs-fade-out");
    this.addClass("vjs-fade-in");
  },

  fadeOut: function(){
    this.removeClass("vjs-fade-in");
    this.addClass("vjs-fade-out");
  },

  lockShowing: function(){
    var style = this.el.style;
    style.display = "block";
    style.opacity = 1;
    style.visiblity = "visible";
  },

  unlockShowing: function(){
    var style = this.el.style;
    style.display = "";
    style.opacity = "";
    style.visiblity = "";
  },

  addClass: function(classToAdd){
    _V_.addClass(this.el, classToAdd);
  },

  removeClass: function(classToRemove){
    _V_.removeClass(this.el, classToRemove);
  },

  /* Events
  ================================================================================ */
  addEvent: function(type, fn, uid){
    return _V_.addEvent(this.el, type, _V_.proxy(this, fn));
  },
  removeEvent: function(type, fn){
    return _V_.removeEvent(this.el, type, fn);
  },
  triggerEvent: function(type, e){
    return _V_.triggerEvent(this.el, type, e);
  },
  one: function(type, fn) {
    _V_.one(this.el, type, _V_.proxy(this, fn));
  },

  /* Ready - Trigger functions when component is ready
  ================================================================================ */
  ready: function(fn){
    if (!fn) return this;

    if (this.isReady) {
      fn.call(this);
    } else {
      if (this.readyQueue === undefined) {
        this.readyQueue = [];
      }
      this.readyQueue.push(fn);
    }

    return this;
  },

  triggerReady: function(){
    this.isReady = true;
    if (this.readyQueue && this.readyQueue.length > 0) {
      // Call all functions in ready queue
      this.each(this.readyQueue, function(fn){
        fn.call(this);
      });

      // Reset Ready Queue
      this.readyQueue = [];

      // Allow for using event listeners also, in case you want to do something everytime a source is ready.
      this.triggerEvent("ready");
    }
  },

  /* Utility
  ================================================================================ */
  each: function(arr, fn){ _V_.each.call(this, arr, fn); },

  eachProp: function(obj, fn){ _V_.eachProp.call(this, obj, fn); },

  extend: function(obj){ _V_.merge(this, obj) },

  // More easily attach 'this' to functions
  proxy: function(fn, uid){  return _V_.proxy(this, fn, uid); }

});/* Control - Base class for all control elements
================================================================================ */
_V_.Control = _V_.Component.extend({

  buildCSSClass: function(){
    return "vjs-control " + this._super();
  }

});

/* Control Bar
================================================================================ */
_V_.ControlBar = _V_.Component.extend({

  options: {
    loadEvent: "play",
    components: {
      "playToggle": {},
      "fullscreenToggle": {},
      "currentTimeDisplay": {},
      "timeDivider": {},
      "durationDisplay": {},
      "remainingTimeDisplay": {},
      "progressControl": {},
      "volumeControl": {},
      "muteToggle": {}
    }
  },

  init: function(player, options){
    this._super(player, options);

    player.addEvent("play", this.proxy(function(){
      this.fadeIn();
      this.player.addEvent("mouseover", this.proxy(this.fadeIn));
      this.player.addEvent("mouseout", this.proxy(this.fadeOut));
    }));

  },

  createElement: function(){
    return _V_.createElement("div", {
      className: "vjs-controls"
    });
  },

  fadeIn: function(){
    this._super();
    this.player.triggerEvent("controlsvisible");
  },

  fadeOut: function(){
    this._super();
    this.player.triggerEvent("controlshidden");
  },

  lockShowing: function(){
    this.el.style.opacity = "1";
  }

});

/* Button - Base class for all buttons
================================================================================ */
_V_.Button = _V_.Control.extend({

  init: function(player, options){
    this._super(player, options);

    this.addEvent("click", this.onClick);
    this.addEvent("focus", this.onFocus);
    this.addEvent("blur", this.onBlur);
  },

  createElement: function(type, attrs){
    // Add standard Aria and Tabindex info
    attrs = _V_.merge({
      className: this.buildCSSClass(),
      innerHTML: '<div><span class="vjs-control-text">' + (this.buttonText || "Need Text") + '</span></div>',
      role: "button",
      tabIndex: 0
    }, attrs);

    return this._super(type, attrs);
  },

  // Click - Override with specific functionality for button
  onClick: function(){},

  // Focus - Add keyboard functionality to element
  onFocus: function(){
    _V_.addEvent(document, "keyup", _V_.proxy(this, this.onKeyPress));
  },

  // KeyPress (document level) - Trigger click when keys are pressed
  onKeyPress: function(event){
    // Check for space bar (32) or enter (13) keys
    if (event.which == 32 || event.which == 13) {
      event.preventDefault();
      this.onClick();
    }
  },

  // Blur - Remove keyboard triggers
  onBlur: function(){
    _V_.removeEvent(document, "keyup", _V_.proxy(this, this.onKeyPress));
  }

});

/* Play Button
================================================================================ */
_V_.PlayButton = _V_.Button.extend({

  buttonText: "Play",

  buildCSSClass: function(){
    return "vjs-play-button " + this._super();
  },

  onClick: function(){
    this.player.play();
  }

});

/* Pause Button
================================================================================ */
_V_.PauseButton = _V_.Button.extend({

  buttonText: "Pause",

  buildCSSClass: function(){
    return "vjs-pause-button " + this._super();
  },

  onClick: function(){
    this.player.pause();
  }

});

/* Play Toggle - Play or Pause Media
================================================================================ */
_V_.PlayToggle = _V_.Button.extend({

  buttonText: "Play",

  init: function(player, options){
    this._super(player, options);

    player.addEvent("play", _V_.proxy(this, this.onPlay));
    player.addEvent("pause", _V_.proxy(this, this.onPause));
  },

  buildCSSClass: function(){
    return "vjs-play-control " + this._super();
  },

  // OnClick - Toggle between play and pause
  onClick: function(){
    if (this.player.paused()) {
      this.player.play();
    } else {
      this.player.pause();
    }
  },

  // OnPlay - Add the vjs-playing class to the element so it can change appearance
  onPlay: function(){
    _V_.removeClass(this.el, "vjs-paused");
    _V_.addClass(this.el, "vjs-playing");
  },

  // OnPause - Add the vjs-paused class to the element so it can change appearance
  onPause: function(){
    _V_.removeClass(this.el, "vjs-playing");
    _V_.addClass(this.el, "vjs-paused");
  }

});


/* Fullscreen Toggle Behaviors
================================================================================ */
_V_.FullscreenToggle = _V_.Button.extend({

  buttonText: "Fullscreen",

  buildCSSClass: function(){
    return "vjs-fullscreen-control " + this._super();
  },

  onClick: function(){
    if (!this.player.isFullScreen) {
      this.player.requestFullScreen();
    } else {
      this.player.cancelFullScreen();
    }
  }

});

/* Big Play Button
================================================================================ */
_V_.BigPlayButton = _V_.Button.extend({
  init: function(player, options){
    this._super(player, options);

    player.addEvent("play", _V_.proxy(this, this.hide));
    player.addEvent("ended", _V_.proxy(this, this.show));
  },

  createElement: function(){
    return this._super("div", {
      className: "vjs-big-play-button",
      innerHTML: "<span></span>"
    });
  },

  onClick: function(){
    // Go back to the beginning if big play button is showing at the end.
    // Have to check for current time otherwise it might throw a 'not ready' error.
    if(this.player.currentTime()) {
      this.player.currentTime(0);
    }
    this.player.play();
  }
});

/* Loading Spinner
================================================================================ */
_V_.LoadingSpinner = _V_.Component.extend({
  init: function(player, options){
    this._super(player, options);

    player.addEvent("canplay", _V_.proxy(this, this.hide));
    player.addEvent("canplaythrough", _V_.proxy(this, this.hide));
    player.addEvent("playing", _V_.proxy(this, this.hide));

    player.addEvent("seeking", _V_.proxy(this, this.show));
    player.addEvent("error", _V_.proxy(this, this.show));

    // Not showing spinner on stalled any more. Browsers may stall and then not trigger any events that would remove the spinner.
    // Checked in Chrome 16 and Safari 5.1.2. http://help.videojs.com/discussions/problems/883-why-is-the-download-progress-showing
    // player.addEvent("stalled", _V_.proxy(this, this.show));

    player.addEvent("waiting", _V_.proxy(this, this.show));
  },

  createElement: function(){

    var classNameSpinner, innerHtmlSpinner;

    if ( typeof this.player.el.style.WebkitBorderRadius == "string"
         || typeof this.player.el.style.MozBorderRadius == "string"
         || typeof this.player.el.style.KhtmlBorderRadius == "string"
         || typeof this.player.el.style.borderRadius == "string")
      {
        classNameSpinner = "vjs-loading-spinner";
        innerHtmlSpinner = "<div class='ball1'></div><div class='ball2'></div><div class='ball3'></div><div class='ball4'></div><div class='ball5'></div><div class='ball6'></div><div class='ball7'></div><div class='ball8'></div>";
      } else {
        classNameSpinner = "vjs-loading-spinner-fallback";
        innerHtmlSpinner = "";
      }

    return this._super("div", {
      className: classNameSpinner,
      innerHTML: innerHtmlSpinner
    });
  }
});

/* Time
================================================================================ */
_V_.CurrentTimeDisplay = _V_.Component.extend({

  init: function(player, options){
    this._super(player, options);

    player.addEvent("timeupdate", _V_.proxy(this, this.updateContent));
  },

  createElement: function(){
    var el = this._super("div", {
      className: "vjs-current-time vjs-time-controls vjs-control"
    });

    this.content = _V_.createElement("div", {
      className: "vjs-current-time-display",
      innerHTML: '0:00'
    });

    el.appendChild(_V_.createElement("div").appendChild(this.content));
    return el;
  },

  updateContent: function(){
    // Allows for smooth scrubbing, when player can't keep up.
    var time = (this.player.scrubbing) ? this.player.values.currentTime : this.player.currentTime();
    this.content.innerHTML = _V_.formatTime(time, this.player.duration());
  }

});

_V_.DurationDisplay = _V_.Component.extend({

  init: function(player, options){
    this._super(player, options);

    player.addEvent("timeupdate", _V_.proxy(this, this.updateContent));
  },

  createElement: function(){
    var el = this._super("div", {
      className: "vjs-duration vjs-time-controls vjs-control"
    });

    this.content = _V_.createElement("div", {
      className: "vjs-duration-display",
      innerHTML: '0:00'
    });

    el.appendChild(_V_.createElement("div").appendChild(this.content));
    return el;
  },

  updateContent: function(){
    if (this.player.duration()) { this.content.innerHTML = _V_.formatTime(this.player.duration()); }
  }

});

// Time Separator (Not used in main skin, but still available, and could be used as a 'spare element')
_V_.TimeDivider = _V_.Component.extend({

  createElement: function(){
    return this._super("div", {
      className: "vjs-time-divider",
      innerHTML: '<div><span>/</span></div>'
    });
  }

});

_V_.RemainingTimeDisplay = _V_.Component.extend({

  init: function(player, options){
    this._super(player, options);

    player.addEvent("timeupdate", _V_.proxy(this, this.updateContent));
  },

  createElement: function(){
    var el = this._super("div", {
      className: "vjs-remaining-time vjs-time-controls vjs-control"
    });

    this.content = _V_.createElement("div", {
      className: "vjs-remaining-time-display",
      innerHTML: '-0:00'
    });

    el.appendChild(_V_.createElement("div").appendChild(this.content));
    return el;
  },

  updateContent: function(){
    if (this.player.duration()) { this.content.innerHTML = "-"+_V_.formatTime(this.player.remainingTime()); }

    // Allows for smooth scrubbing, when player can't keep up.
    // var time = (this.player.scrubbing) ? this.player.values.currentTime : this.player.currentTime();
    // this.content.innerHTML = _V_.formatTime(time, this.player.duration());
  }

});

/* Slider - Parent for seek bar and volume slider
================================================================================ */
_V_.Slider = _V_.Component.extend({

  init: function(player, options){
    this._super(player, options);

    player.addEvent(this.playerEvent, _V_.proxy(this, this.update));

    this.addEvent("mousedown", this.onMouseDown);
    this.addEvent("focus", this.onFocus);
    this.addEvent("blur", this.onBlur);

    this.player.addEvent("controlsvisible", this.proxy(this.update));

    // This is actually to fix the volume handle position. http://twitter.com/#!/gerritvanaaken/status/159046254519787520
    // this.player.one("timeupdate", this.proxy(this.update));

    this.update();
  },

  createElement: function(type, attrs) {
    attrs = _V_.merge({
      role: "slider",
      "aria-valuenow": 0,
      "aria-valuemin": 0,
      "aria-valuemax": 100,
      tabIndex: 0
    }, attrs);

    return this._super(type, attrs);
  },

  onMouseDown: function(event){
    event.preventDefault();
    _V_.blockTextSelection();

    _V_.addEvent(document, "mousemove", _V_.proxy(this, this.onMouseMove));
    _V_.addEvent(document, "mouseup", _V_.proxy(this, this.onMouseUp));

    this.onMouseMove(event);
  },

  onMouseUp: function(event) {
    _V_.unblockTextSelection();
    _V_.removeEvent(document, "mousemove", this.onMouseMove, false);
    _V_.removeEvent(document, "mouseup", this.onMouseUp, false);

    this.update();
  },

  update: function(){
    // If scrubbing, we could use a cached value to make the handle keep up with the user's mouse.
    // On HTML5 browsers scrubbing is really smooth, but some flash players are slow, so we might want to utilize this later.
    // var progress =  (this.player.scrubbing) ? this.player.values.currentTime / this.player.duration() : this.player.currentTime() / this.player.duration();

    var barProgress,
        progress = this.getPercent();
        handle = this.handle,
        bar = this.bar;

    // Protect against no duration and other division issues
    if (isNaN(progress)) { progress = 0; }

    barProgress = progress;

    // If there is a handle, we need to account for the handle in our calculation for progress bar
    // so that it doesn't fall short of or extend past the handle.
    if (handle) {

      var box = this.el,
          boxWidth = box.offsetWidth,

          handleWidth = handle.el.offsetWidth,

          // The width of the handle in percent of the containing box
          // In IE, widths may not be ready yet causing NaN
          handlePercent = (handleWidth) ? handleWidth / boxWidth : 0,

          // Get the adjusted size of the box, considering that the handle's center never touches the left or right side.
          // There is a margin of half the handle's width on both sides.
          boxAdjustedPercent = 1 - handlePercent;

          // Adjust the progress that we'll use to set widths to the new adjusted box width
          adjustedProgress = progress * boxAdjustedPercent,

          // The bar does reach the left side, so we need to account for this in the bar's width
          barProgress = adjustedProgress + (handlePercent / 2);

      // Move the handle from the left based on the adjected progress
      handle.el.style.left = _V_.round(adjustedProgress * 100, 2) + "%";
    }

    // Set the new bar width
    bar.el.style.width = _V_.round(barProgress * 100, 2) + "%";
  },

  calculateDistance: function(event){
    var box = this.el,
        boxX = _V_.findPosX(box),
        boxW = box.offsetWidth,
        handle = this.handle;

    if (handle) {
      var handleW = handle.el.offsetWidth;

      // Adjusted X and Width, so handle doesn't go outside the bar
      boxX = boxX + (handleW / 2);
      boxW = boxW - handleW;
    }

    // Percent that the click is through the adjusted area
    return Math.max(0, Math.min(1, (event.pageX - boxX) / boxW));
  },

  onFocus: function(event){
    _V_.addEvent(document, "keyup", _V_.proxy(this, this.onKeyPress));
  },

  onKeyPress: function(event){
    if (event.which == 37) { // Left Arrow
      event.preventDefault();
      this.stepBack();
    } else if (event.which == 39) { // Right Arrow
      event.preventDefault();
      this.stepForward();
    }
  },

  onBlur: function(event){
    _V_.removeEvent(document, "keyup", _V_.proxy(this, this.onKeyPress));
  }
});


/* Progress
================================================================================ */

// Progress Control: Seek, Load Progress, and Play Progress
_V_.ProgressControl = _V_.Component.extend({

  options: {
    components: {
      "seekBar": {}
    }
  },

  createElement: function(){
    return this._super("div", {
      className: "vjs-progress-control vjs-control"
    });
  }

});

// Seek Bar and holder for the progress bars
_V_.SeekBar = _V_.Slider.extend({

  options: {
    components: {
      "loadProgressBar": {},

      // Set property names to bar and handle to match with the parent Slider class is looking for
      "bar": { componentClass: "PlayProgressBar" },
      "handle": { componentClass: "SeekHandle" }
    }
  },

  playerEvent: "timeupdate",

  init: function(player, options){
    this._super(player, options);
  },

  createElement: function(){
    return this._super("div", {
      className: "vjs-progress-holder"
    });
  },

  getPercent: function(){
    return this.player.currentTime() / this.player.duration();
  },

  onMouseDown: function(event){
    this._super(event);

    this.player.scrubbing = true;

    this.videoWasPlaying = !this.player.paused();
    this.player.pause();
  },

  onMouseMove: function(event){
    var newTime = this.calculateDistance(event) * this.player.duration();

    // Don't let video end while scrubbing.
    if (newTime == this.player.duration()) { newTime = newTime - 0.1; }

    // Set new time (tell player to seek to new time)
    this.player.currentTime(newTime);
  },

  onMouseUp: function(event){
    this._super(event);

    this.player.scrubbing = false;
    if (this.videoWasPlaying) {
      this.player.play();
    }
  },

  stepForward: function(){
    this.player.currentTime(this.player.currentTime() + 1);
  },

  stepBack: function(){
    this.player.currentTime(this.player.currentTime() - 1);
  }

});

// Load Progress Bar
_V_.LoadProgressBar = _V_.Component.extend({

  init: function(player, options){
    this._super(player, options);
    player.addEvent("progress", _V_.proxy(this, this.update));
  },

  createElement: function(){
    return this._super("div", {
      className: "vjs-load-progress",
      innerHTML: '<span class="vjs-control-text">Loaded: 0%</span>'
    });
  },

  update: function(){
    if (this.el.style) { this.el.style.width = _V_.round(this.player.bufferedPercent() * 100, 2) + "%"; }
  }

});

// Play Progress Bar
_V_.PlayProgressBar = _V_.Component.extend({

  createElement: function(){
    return this._super("div", {
      className: "vjs-play-progress",
      innerHTML: '<span class="vjs-control-text">Progress: 0%</span>'
    });
  }

});

// Seek Handle
// SeekBar Behavior includes play progress bar, and seek handle
// Needed so it can determine seek position based on handle position/size
_V_.SeekHandle = _V_.Component.extend({

  createElement: function(){
    return this._super("div", {
      className: "vjs-seek-handle",
      innerHTML: '<span class="vjs-control-text">00:00</span>'
    });
  }

});

/* Volume Scrubber
================================================================================ */
// Progress Control: Seek, Load Progress, and Play Progress
_V_.VolumeControl = _V_.Component.extend({

  options: {
    components: {
      "volumeBar": {}
    }
  },

  createElement: function(){
    return this._super("div", {
      className: "vjs-volume-control vjs-control"
    });
  }

});

_V_.VolumeBar = _V_.Slider.extend({

  options: {
    components: {
      "bar": { componentClass: "VolumeLevel" },
      "handle": { componentClass: "VolumeHandle" }
    }
  },

  playerEvent: "volumechange",

  createElement: function(){
    return this._super("div", {
      className: "vjs-volume-bar"
    });
  },

  onMouseMove: function(event) {
    this.player.volume(this.calculateDistance(event));
  },

  getPercent: function(){
   return this.player.volume();
  },

  stepForward: function(){
    this.player.volume(this.player.volume() + 0.1);
  },

  stepBack: function(){
    this.player.volume(this.player.volume() - 0.1);
  }
});

_V_.VolumeLevel = _V_.Component.extend({

  createElement: function(){
    return this._super("div", {
      className: "vjs-volume-level",
      innerHTML: '<span class="vjs-control-text"></span>'
    });
  }

});

_V_.VolumeHandle = _V_.Component.extend({

  createElement: function(){
    return this._super("div", {
      className: "vjs-volume-handle",
      innerHTML: '<span class="vjs-control-text"></span>'
      // tabindex: 0,
      // role: "slider", "aria-valuenow": 0, "aria-valuemin": 0, "aria-valuemax": 100
    });
  }

});

_V_.MuteToggle = _V_.Button.extend({

  init: function(player, options){
    this._super(player, options);

    player.addEvent("volumechange", _V_.proxy(this, this.update));
  },

  createElement: function(){
    return this._super("div", {
      className: "vjs-mute-control vjs-control",
      innerHTML: '<div><span class="vjs-control-text">Mute</span></div>'
    });
  },

  onClick: function(event){
    this.player.muted( this.player.muted() ? false : true );
  },

  update: function(event){
    var vol = this.player.volume(),
        level = 3;

    if (vol == 0 || this.player.muted()) {
      level = 0;
    } else if (vol < 0.33) {
      level = 1;
    } else if (vol < 0.67) {
      level = 2;
    }

    /* TODO improve muted icon classes */
    _V_.each.call(this, [0,1,2,3], function(i){
      _V_.removeClass(this.el, "vjs-vol-"+i);
    });
    _V_.addClass(this.el, "vjs-vol-"+level);
  }

});


/* Poster Image
================================================================================ */
_V_.PosterImage = _V_.Button.extend({
  init: function(player, options){
    this._super(player, options);

    if (!this.player.options.poster) {
      this.hide();
    }

    player.addEvent("play", _V_.proxy(this, this.hide));
  },

  createElement: function(){
    return _V_.createElement("img", {
      className: "vjs-poster",
      src: this.player.options.poster,

      // Don't want poster to be tabbable.
      tabIndex: -1
    });
  },

  onClick: function(){
    this.player.play();
  }
});

/* Menu
================================================================================ */
// The base for text track and settings menu buttons.
_V_.Menu = _V_.Component.extend({

  init: function(player, options){
    this._super(player, options);
  },

  addItem: function(component){
    this.addComponent(component);
    component.addEvent("click", this.proxy(function(){
      this.unlockShowing();
    }));
  },

  createElement: function(){
    return this._super("ul", {
      className: "vjs-menu"
    });
  }

});

_V_.MenuItem = _V_.Button.extend({

  init: function(player, options){
    this._super(player, options);

    if (options.selected) {
      this.addClass("vjs-selected");
    }
  },

  createElement: function(type, attrs){
    return this._super("li", _V_.merge({
      className: "vjs-menu-item",
      innerHTML: this.options.label
    }, attrs));
  },

  onClick: function(){
    this.selected(true);
  },

  selected: function(selected){
    if (selected) {
      this.addClass("vjs-selected");
    } else {
      this.removeClass("vjs-selected")
    }
  }

});// ECMA-262 is the standard for javascript.
// The following methods are impelemented EXACTLY as described in the standard (according to Mozilla Docs), and do not override the default method if one exists.
// This may conflict with other libraries that modify the array prototype, but those libs should update to use the standard.

// [].indexOf
// https://developer.mozilla.org/en/JavaScript/Reference/Global_Objects/Array/indexOf
if (!Array.prototype.indexOf) {
    Array.prototype.indexOf = function (searchElement /*, fromIndex */ ) {
        "use strict";
        if (this === void 0 || this === null) {
            throw new TypeError();
        }
        var t = Object(this);
        var len = t.length >>> 0;
        if (len === 0) {
            return -1;
        }
        var n = 0;
        if (arguments.length > 0) {
            n = Number(arguments[1]);
            if (n !== n) { // shortcut for verifying if it's NaN
                n = 0;
            } else if (n !== 0 && n !== (1 / 0) && n !== -(1 / 0)) {
                n = (n > 0 || -1) * Math.floor(Math.abs(n));
            }
        }
        if (n >= len) {
            return -1;
        }
        var k = n >= 0 ? n : Math.max(len - Math.abs(n), 0);
        for (; k < len; k++) {
            if (k in t && t[k] === searchElement) {
                return k;
            }
        }
        return -1;
    }
}

// NOT NEEDED YET
// [].lastIndexOf
// https://developer.mozilla.org/en/JavaScript/Reference/Global_Objects/Array/lastIndexOf
// if (!Array.prototype.lastIndexOf)
// {
//   Array.prototype.lastIndexOf = function(searchElement /*, fromIndex*/)
//   {
//     "use strict";
// 
//     if (this === void 0 || this === null)
//       throw new TypeError();
// 
//     var t = Object(this);
//     var len = t.length >>> 0;
//     if (len === 0)
//       return -1;
// 
//     var n = len;
//     if (arguments.length > 1)
//     {
//       n = Number(arguments[1]);
//       if (n !== n)
//         n = 0;
//       else if (n !== 0 && n !== (1 / 0) && n !== -(1 / 0))
//         n = (n > 0 || -1) * Math.floor(Math.abs(n));
//     }
// 
//     var k = n >= 0
//           ? Math.min(n, len - 1)
//           : len - Math.abs(n);
// 
//     for (; k >= 0; k--)
//     {
//       if (k in t && t[k] === searchElement)
//         return k;
//     }
//     return -1;
//   };
// }


// NOT NEEDED YET
// Array forEach per ECMA standard https://developer.mozilla.org/en/JavaScript/Reference/Global_Objects/array/foreach
// Production steps of ECMA-262, Edition 5, 15.4.4.18
// Reference: http://es5.github.com/#x15.4.4.18
// if ( !Array.prototype.forEach ) {
// 
//   Array.prototype.forEach = function( callback, thisArg ) {
// 
//     var T, k;
// 
//     if ( this == null ) {
//       throw new TypeError( " this is null or not defined" );
//     }
// 
//     // 1. Let O be the result of calling ToObject passing the |this| value as the argument.
//     var O = Object(this);
// 
//     // 2. Let lenValue be the result of calling the Get internal method of O with the argument "length".
//     // 3. Let len be ToUint32(lenValue).
//     var len = O.length >>> 0;
// 
//     // 4. If IsCallable(callback) is false, throw a TypeError exception.
//     // See: http://es5.github.com/#x9.11
//     if ( {}.toString.call(callback) != "[object Function]" ) {
//       throw new TypeError( callback + " is not a function" );
//     }
// 
//     // 5. If thisArg was supplied, let T be thisArg; else let T be undefined.
//     if ( thisArg ) {
//       T = thisArg;
//     }
// 
//     // 6. Let k be 0
//     k = 0;
// 
//     // 7. Repeat, while k < len
//     while( k < len ) {
// 
//       var kValue;
// 
//       // a. Let Pk be ToString(k).
//       //   This is implicit for LHS operands of the in operator
//       // b. Let kPresent be the result of calling the HasProperty internal method of O with argument Pk.
//       //   This step can be combined with c
//       // c. If kPresent is true, then
//       if ( k in O ) {
// 
//         // i. Let kValue be the result of calling the Get internal method of O with argument Pk.
//         kValue = O[ Pk ];
// 
//         // ii. Call the Call internal method of callback with T as the this value and
//         // argument list containing kValue, k, and O.
//         callback.call( T, kValue, k, O );
//       }
//       // d. Increase k by 1.
//       k++;
//     }
//     // 8. return undefined
//   };
// }


// NOT NEEDED YET
// https://developer.mozilla.org/en/JavaScript/Reference/Global_Objects/Array/map
// Production steps of ECMA-262, Edition 5, 15.4.4.19
// Reference: http://es5.github.com/#x15.4.4.19
// if (!Array.prototype.map) {
//   Array.prototype.map = function(callback, thisArg) {
// 
//     var T, A, k;
// 
//     if (this == null) {
//       throw new TypeError(" this is null or not defined");
//     }
// 
//     // 1. Let O be the result of calling ToObject passing the |this| value as the argument.
//     var O = Object(this);
// 
//     // 2. Let lenValue be the result of calling the Get internal method of O with the argument "length".
//     // 3. Let len be ToUint32(lenValue).
//     var len = O.length >>> 0;
// 
//     // 4. If IsCallable(callback) is false, throw a TypeError exception.
//     // See: http://es5.github.com/#x9.11
//     if ({}.toString.call(callback) != "[object Function]") {
//       throw new TypeError(callback + " is not a function");
//     }
// 
//     // 5. If thisArg was supplied, let T be thisArg; else let T be undefined.
//     if (thisArg) {
//       T = thisArg;
//     }
// 
//     // 6. Let A be a new array created as if by the expression new Array(len) where Array is
//     // the standard built-in constructor with that name and len is the value of len.
//     A = new Array(len);
// 
//     // 7. Let k be 0
//     k = 0;
// 
//     // 8. Repeat, while k < len
//     while(k < len) {
// 
//       var kValue, mappedValue;
// 
//       // a. Let Pk be ToString(k).
//       //   This is implicit for LHS operands of the in operator
//       // b. Let kPresent be the result of calling the HasProperty internal method of O with argument Pk.
//       //   This step can be combined with c
//       // c. If kPresent is true, then
//       if (k in O) {
// 
//         // i. Let kValue be the result of calling the Get internal method of O with argument Pk.
//         kValue = O[ k ];
// 
//         // ii. Let mappedValue be the result of calling the Call internal method of callback
//         // with T as the this value and argument list containing kValue, k, and O.
//         mappedValue = callback.call(T, kValue, k, O);
// 
//         // iii. Call the DefineOwnProperty internal method of A with arguments
//         // Pk, Property Descriptor {Value: mappedValue, Writable: true, Enumerable: true, Configurable: true},
//         // and false.
// 
//         // In browsers that support Object.defineProperty, use the following:
//         // Object.defineProperty(A, Pk, { value: mappedValue, writable: true, enumerable: true, configurable: true });
// 
//         // For best browser support, use the following:
//         A[ k ] = mappedValue;
//       }
//       // d. Increase k by 1.
//       k++;
//     }
// 
//     // 9. return A
//     return A;
//   };      
// }
// Event System (J.Resig - Secrets of a JS Ninja http://jsninja.com/ [Go read it, really])
// (Book version isn't completely usable, so fixed some things and borrowed from jQuery where it's working)
// 
// This should work very similarly to jQuery's events, however it's based off the book version which isn't as
// robust as jquery's, so there's probably some differences.
// 
// When you add an event listener using _V_.addEvent, 
//   it stores the handler function in seperate cache object, 
//   and adds a generic handler to the element's event,
//   along with a unique id (guid) to the element.

_V_.extend({

  // Add an event listener to element
  // It stores the handler function in a separate cache object
  // and adds a generic handler to the element's event,
  // along with a unique id (guid) to the element.
  addEvent: function(elem, type, fn){
    var data = _V_.getData(elem), handlers;

    // We only need to generate one handler per element
    if (data && !data.handler) {
      // Our new meta-handler that fixes the event object and the context
      data.handler = function(event){
        event = _V_.fixEvent(event);
        var handlers = _V_.getData(elem).events[event.type];
        // Go through and call all the real bound handlers
        if (handlers) {
          
          // Copy handlers so if handlers are added/removed during the process it doesn't throw everything off.
          var handlersCopy = [];
          _V_.each(handlers, function(handler, i){
            handlersCopy[i] = handler;
          })
          
          for (var i = 0, l = handlersCopy.length; i < l; i++) {
            handlersCopy[i].call(elem, event);
          }
        }
      };
    }

    // We need a place to store all our event data
    if (!data.events) { data.events = {}; }

    // And a place to store the handlers for this event type
    handlers = data.events[type];

    if (!handlers) {
      handlers = data.events[ type ] = [];

      // Attach our meta-handler to the element, since one doesn't exist
      if (document.addEventListener) {
        elem.addEventListener(type, data.handler, false);
      } else if (document.attachEvent) {
        elem.attachEvent("on" + type, data.handler);
      }
    }

    if (!fn.guid) { fn.guid = _V_.guid++; }

    handlers.push(fn);
  },

  removeEvent: function(elem, type, fn) {
    var data = _V_.getData(elem), handlers;
    // If no events exist, nothing to unbind
    if (!data.events) { return; }

    // Are we removing all bound events?
    if (!type) {
      for (type in data.events) {
        _V_.cleanUpEvents(elem, type);
      }
      return;
    }

    // And a place to store the handlers for this event type
    handlers = data.events[type];

    // If no handlers exist, nothing to unbind
    if (!handlers) { return; }

    // See if we're only removing a single handler
    if (fn && fn.guid) {
      for (var i = 0; i < handlers.length; i++) {
        // We found a match (don't stop here, there could be a couple bound)
        if (handlers[i].guid === fn.guid) {
          // Remove the handler from the array of handlers
          handlers.splice(i--, 1);
        }
      }
    }

    _V_.cleanUpEvents(elem, type);
  },

  cleanUpEvents: function(elem, type) {
    var data = _V_.getData(elem);
    // Remove the events of a particular type if there are none left

    if (data.events[type].length === 0) {
      delete data.events[type];

      // Remove the meta-handler from the element
      if (document.removeEventListener) {
        elem.removeEventListener(type, data.handler, false);
      } else if (document.detachEvent) {
        elem.detachEvent("on" + type, data.handler);
      }
    }

    // Remove the events object if there are no types left
    if (_V_.isEmpty(data.events)) {
      delete data.events;
      delete data.handler;
    }

    // Finally remove the expando if there is no data left
    if (_V_.isEmpty(data)) {
      _V_.removeData(elem);
    }
  },

  fixEvent: function(event) {
    if (event[_V_.expando]) { return event; }
    // store a copy of the original event object
    // and "clone" to set read-only properties
    var originalEvent = event;
    event = new _V_.Event(originalEvent);

    for ( var i = _V_.Event.props.length, prop; i; ) {
      prop = _V_.Event.props[ --i ];
      event[prop] = originalEvent[prop];
    }

    // Fix target property, if necessary
    if (!event.target) { event.target = event.srcElement || document; }

    // check if target is a textnode (safari)
    if (event.target.nodeType === 3) { event.target = event.target.parentNode; }

    // Add relatedTarget, if necessary
    if (!event.relatedTarget && event.fromElement) {
      event.relatedTarget = event.fromElement === event.target ? event.toElement : event.fromElement;
    }

    // Calculate pageX/Y if missing and clientX/Y available
    if ( event.pageX == null && event.clientX != null ) {
      var eventDocument = event.target.ownerDocument || document,
        doc = eventDocument.documentElement,
        body = eventDocument.body;

      event.pageX = event.clientX + (doc && doc.scrollLeft || body && body.scrollLeft || 0) - (doc && doc.clientLeft || body && body.clientLeft || 0);
      event.pageY = event.clientY + (doc && doc.scrollTop  || body && body.scrollTop  || 0) - (doc && doc.clientTop  || body && body.clientTop  || 0);
    }

    // Add which for key events
    if (event.which == null && (event.charCode != null || event.keyCode != null)) {
      event.which = event.charCode != null ? event.charCode : event.keyCode;
    }

    // Add metaKey to non-Mac browsers (use ctrl for PC's and Meta for Macs)
    if ( !event.metaKey && event.ctrlKey ) {
      event.metaKey = event.ctrlKey;
    }

    // Add which for click: 1 === left; 2 === middle; 3 === right
    // Note: button is not normalized, so don't use it
    if ( !event.which && event.button !== undefined ) {
      event.which = (event.button & 1 ? 1 : ( event.button & 2 ? 3 : ( event.button & 4 ? 2 : 0 ) ));
    }

    return event;
  },

  triggerEvent: function(elem, event) {
    var data = _V_.getData(elem),
        parent = elem.parentNode || elem.ownerDocument,
        type = event.type || event,
        handler;

    if (data) { handler = data.handler }

    // Added in attion to book. Book code was broke.
    event = typeof event === "object" ?
      event[_V_.expando] ? 
        event :
        new _V_.Event(type, event) :
      new _V_.Event(type);

    event.type = type;
    if (handler) {
      handler.call(elem, event);
    }

    // Clean up the event in case it is being reused
    event.result = undefined;
    event.target = elem;

    // Bubble the event up the tree to the document,
    // Unless it's been explicitly stopped
    // if (parent && !event.isPropagationStopped()) {
    //   _V_.triggerEvent(parent, event);
    // 
    // // We're at the top document so trigger the default action
    // } else if (!parent && !event.isDefaultPrevented()) {
    //   // log(type);
    //   var targetData = _V_.getData(event.target);
    //   // log(targetData);
    //   var targetHandler = targetData.handler;
    //   // log("2");
    //   if (event.target[event.type]) {
    //     // Temporarily disable the bound handler,
    //     // don't want to execute it twice
    //     if (targetHandler) {
    //       targetData.handler = function(){};
    //     }
    // 
    //     // Trigger the native event (click, focus, blur)
    //     event.target[event.type]();
    // 
    //     // Restore the handler
    //     if (targetHandler) {
    //       targetData.handler = targetHandler;
    //     }
    //   }
    // }
  },
  
  one: function(elem, type, fn) {
    _V_.addEvent(elem, type, function(){
      _V_.removeEvent(elem, type, arguments.callee)
      fn.apply(this, arguments);
    });
  }
});

// Custom Event object for standardizing event objects between browsers.
_V_.Event = function(src, props){
  // Event object
  if (src && src.type) {
    this.originalEvent = src;
    this.type = src.type;

    // Events bubbling up the document may have been marked as prevented
    // by a handler lower down the tree; reflect the correct value.
    this.isDefaultPrevented = (src.defaultPrevented || src.returnValue === false ||
      src.getPreventDefault && src.getPreventDefault()) ? returnTrue : returnFalse;

  // Event type
  } else {
    this.type = src;
  }

  // Put explicitly provided properties onto the event object
  if (props) { _V_.merge(this, props); }

  this.timeStamp = (new Date).getTime();

  // Mark it as fixed
  this[_V_.expando] = true;
};

_V_.Event.prototype = {
  preventDefault: function() {
    this.isDefaultPrevented = returnTrue;

    var e = this.originalEvent;
    if (!e) { return; }

    // if preventDefault exists run it on the original event
    if (e.preventDefault) { 
      e.preventDefault();
    // otherwise set the returnValue property of the original event to false (IE)
    } else {
      e.returnValue = false;
    }
  },
  stopPropagation: function() {
    this.isPropagationStopped = returnTrue;

    var e = this.originalEvent;
    if (!e) { return; }
    // if stopPropagation exists run it on the original event
    if (e.stopPropagation) { e.stopPropagation(); }
    // otherwise set the cancelBubble property of the original event to true (IE)
    e.cancelBubble = true;
  },
  stopImmediatePropagation: function() {
    this.isImmediatePropagationStopped = returnTrue;
    this.stopPropagation();
  },
  isDefaultPrevented: returnFalse,
  isPropagationStopped: returnFalse,
  isImmediatePropagationStopped: returnFalse
};
_V_.Event.props = "altKey attrChange attrName bubbles button cancelable charCode clientX clientY ctrlKey currentTarget data detail eventPhase fromElement handler keyCode metaKey newValue offsetX offsetY pageX pageY prevValue relatedNode relatedTarget screenX screenY shiftKey srcElement target toElement view wheelDelta which".split(" ");

function returnTrue(){ return true; }
function returnFalse(){ return false; }

// Javascript JSON implementation
// (Parse Method Only)
// https://github.com/douglascrockford/JSON-js/blob/master/json2.js

var JSON;
if (!JSON) { JSON = {}; }

(function(){
  var cx = /[\u0000\u00ad\u0600-\u0604\u070f\u17b4\u17b5\u200c-\u200f\u2028-\u202f\u2060-\u206f\ufeff\ufff0-\uffff]/g;

  if (typeof JSON.parse !== 'function') {
      JSON.parse = function (text, reviver) {
          var j;

          function walk(holder, key) {
              var k, v, value = holder[key];
              if (value && typeof value === 'object') {
                  for (k in value) {
                      if (Object.prototype.hasOwnProperty.call(value, k)) {
                          v = walk(value, k);
                          if (v !== undefined) {
                              value[k] = v;
                          } else {
                              delete value[k];
                          }
                      }
                  }
              }
              return reviver.call(holder, key, value);
          }
          text = String(text);
          cx.lastIndex = 0;
          if (cx.test(text)) {
              text = text.replace(cx, function (a) {
                  return '\\u' +
                      ('0000' + a.charCodeAt(0).toString(16)).slice(-4);
              });
          }

          if (/^[\],:{}\s]*$/
                  .test(text.replace(/\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g, '@')
                      .replace(/"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g, ']')
                      .replace(/(?:^|:|,)(?:\s*\[)+/g, ''))) {

              j = eval('(' + text + ')');

              return typeof reviver === 'function' ?
                  walk({'': j}, '') : j;
          }

          throw new SyntaxError('JSON.parse');
      };
  }
}());
/* UI Component- Base class for all UI objects
================================================================================ */
_V_.Player = _V_.Component.extend({

  init: function(tag, addOptions, ready){

    this.tag = tag; // Store the original tag used to set options

    var el = this.el = _V_.createElement("div"), // Div to contain video and controls
        options = this.options = {},
        width = options.width = tag.getAttribute('width'),
        height = options.height = tag.getAttribute('height'),

        // Browsers default to 300x150 if there's no width/height or video size data.
        initWidth = width || 300,
        initHeight = height || 150;

    // Make player findable on elements
    tag.player = el.player = this;

    // Add callback to ready queue
    this.ready(ready);

    // Wrap video tag in div (el/box) container
    tag.parentNode.insertBefore(el, tag);
    el.appendChild(tag); // Breaks iPhone, fixed in HTML5 setup.

    // Give video tag properties to box
    el.id = this.id = tag.id; // ID will now reference box, not the video tag
    el.className = tag.className;
    // Update tag id/class for use as HTML5 playback tech
    tag.id += "_html5_api";
    tag.className = "vjs-tech";

    // Make player easily findable by ID
    _V_.players[el.id] = this;

    // Make box use width/height of tag, or default 300x150
    el.setAttribute("width", initWidth);
    el.setAttribute("height", initHeight);
    // Enforce with CSS since width/height attrs don't work on divs
    el.style.width = initWidth+"px";
    el.style.height = initHeight+"px";
    // Remove width/height attrs from tag so CSS can make it 100% width/height
    tag.removeAttribute("width");
    tag.removeAttribute("height");

    // Set Options
    _V_.merge(options, _V_.options); // Copy Global Defaults
    _V_.merge(options, this.getVideoTagSettings()); // Override with Video Tag Options
    _V_.merge(options, addOptions); // Override/extend with options from setup call

    // Store controls setting, and then remove immediately so native controls don't flash.
    tag.removeAttribute("controls");

    // Poster will be handled by a manual <img>
    tag.removeAttribute("poster");

    // Empty video tag sources and tracks so the built in player doesn't use them also.
    if (tag.hasChildNodes()) {
      for (var i=0,j=tag.childNodes;i<j.length;i++) {
        if (j[i].nodeName == "SOURCE" || j[i].nodeName == "TRACK") {
          tag.removeChild(j[i]);
        }
      }
    }

    // Cache for video property values.
    this.values = {};

    this.addClass("vjs-paused");

    this.addEvent("ended", this.onEnded);
    this.addEvent("play", this.onPlay);
    this.addEvent("pause", this.onPause);
    this.addEvent("progress", this.onProgress);
    this.addEvent("error", this.onError);

    // When the API is ready, loop through the components and add to the player.
    if (options.controls) {
      this.ready(function(){
        this.initComponents();
      });
    }

    // Tracks defined in tracks.js
    this.textTracks = [];
    if (options.tracks && options.tracks.length > 0) {
      this.addTextTracks(options.tracks);
    }

    // If there are no sources when the player is initialized,
    // load the first supported playback technology.
    if (!options.sources || options.sources.length == 0) {
      for (var i=0,j=options.techOrder; i<j.length; i++) {
        var techName = j[i],
            tech = _V_[techName];

        // Check if the browser supports this technology
        if (tech.isSupported()) {
          this.loadTech(techName);
          break;
        }
      }
    } else {
      // Loop through playback technologies (HTML5, Flash) and check for support. Then load the best source.
      // A few assumptions here:
      //   All playback technologies respect preload false.
      this.src(options.sources);
    }
  },

  // Cache for video property values.
  values: {},

  destroy: function(){
    // Ensure that tracking progress and time progress will stop and plater deleted
    this.stopTrackingProgress();
    this.stopTrackingCurrentTime();
    _V_.players[this.id] = null;
    delete _V_.players[this.id];
    this.tech.destroy();
    this.el.parentNode.removeChild(this.el);
  },

  createElement: function(type, options){},

  getVideoTagSettings: function(){
    var options = {
      sources: [],
      tracks: []
    };

    options.src = this.tag.getAttribute("src");
    options.controls = this.tag.getAttribute("controls") !== null;
    options.poster = this.tag.getAttribute("poster");
    options.preload = this.tag.getAttribute("preload");
    options.autoplay = this.tag.getAttribute("autoplay") !== null; // hasAttribute not IE <8 compatible
    options.loop = this.tag.getAttribute("loop") !== null;
    options.muted = this.tag.getAttribute("muted") !== null;

    if (this.tag.hasChildNodes()) {
      for (var c,i=0,j=this.tag.childNodes;i<j.length;i++) {
        c = j[i];
        if (c.nodeName == "SOURCE") {
          options.sources.push({
            src: c.getAttribute('src'),
            type: c.getAttribute('type'),
            media: c.getAttribute('media'),
            title: c.getAttribute('title')
          });
        }
        if (c.nodeName == "TRACK") {
          options.tracks.push({
            src: c.getAttribute("src"),
            kind: c.getAttribute("kind"),
            srclang: c.getAttribute("srclang"),
            label: c.getAttribute("label"),
            'default': c.getAttribute("default") !== null,
            title: c.getAttribute("title")
          });
        }
      }
    }
    return options;
  },

  /* PLayback Technology (tech)
  ================================================================================ */
  // Load/Create an instance of playback technlogy including element and API methods
  // And append playback element in player div.
  loadTech: function(techName, source){

    // Pause and remove current playback technology
    if (this.tech) {
      this.unloadTech();

    // If the first time loading, HTML5 tag will exist but won't be initialized
    // So we need to remove it if we're not loading HTML5
    } else if (techName != "html5" && this.tag) {
      this.el.removeChild(this.tag);
      this.tag = false;
    }

    this.techName = techName;

    // Turn off API access because we're loading a new tech that might load asynchronously
    this.isReady = false;

    var techReady = function(){
      this.player.triggerReady();

      // Manually track progress in cases where the browser/flash player doesn't report it.
      if (!this.support.progressEvent) {
        this.player.manualProgressOn();
      }

      // Manually track timeudpates in cases where the browser/flash player doesn't report it.
      if (!this.support.timeupdateEvent) {
        this.player.manualTimeUpdatesOn();
      }
    }

    // Grab tech-specific options from player options and add source and parent element to use.
    var techOptions = _V_.merge({ source: source, parentEl: this.el }, this.options[techName])

    if (source) {
      if (source.src == this.values.src && this.values.currentTime > 0) {
        techOptions.startTime = this.values.currentTime;
      }

      this.values.src = source.src;
    }

    // Initialize tech instance
    this.tech = new _V_[techName](this, techOptions);
    this.tech.ready(techReady);
  },

  unloadTech: function(){
    this.tech.destroy();

    // Turn off any manual progress or timeupdate tracking
    if (this.manualProgress) { this.manualProgressOff(); }

    if (this.manualTimeUpdates) { this.manualTimeUpdatesOff(); }

    this.tech = false;
  },

  // There's many issues around changing the size of a Flash (or other plugin) object.
  // First is a plugin reload issue in Firefox that has been around for 11 years: https://bugzilla.mozilla.org/show_bug.cgi?id=90268
  // Then with the new fullscreen API, Mozilla and webkit browsers will reload the flash object after going to fullscreen.
  // To get around this, we're unloading the tech, caching source and currentTime values, and reloading the tech once the plugin is resized.
  // reloadTech: function(betweenFn){
  //   _V_.log("unloadingTech")
  //   this.unloadTech();
  //   _V_.log("unloadedTech")
  //   if (betweenFn) { betweenFn.call(); }
  //   _V_.log("LoadingTech")
  //   this.loadTech(this.techName, { src: this.values.src })
  //   _V_.log("loadedTech")
  // },

  /* Fallbacks for unsupported event types
  ================================================================================ */
  // Manually trigger progress events based on changes to the buffered amount
  // Many flash players and older HTML5 browsers don't send progress or progress-like events
  manualProgressOn: function(){
    this.manualProgress = true;

    // Trigger progress watching when a source begins loading
    this.trackProgress();

    // Watch for a native progress event call on the tech element
    // In HTML5, some older versions don't support the progress event
    // So we're assuming they don't, and turning off manual progress if they do.
    this.tech.addEvent("progress", function(){

      // Remove this listener from the element
      this.removeEvent("progress", arguments.callee);

      // Update known progress support for this playback technology
      this.support.progressEvent = true;

      // Turn off manual progress tracking
      this.player.manualProgressOff();
    });
  },

  manualProgressOff: function(){
    this.manualProgress = false;
    this.stopTrackingProgress();
  },

  trackProgress: function(){
    this.progressInterval = setInterval(_V_.proxy(this, function(){
      // Don't trigger unless buffered amount is greater than last time
      // log(this.values.bufferEnd, this.buffered().end(0), this.duration())
      /* TODO: update for multiple buffered regions */
      if (this.values.bufferEnd < this.buffered().end(0)) {
        this.triggerEvent("progress");
      } else if (this.bufferedPercent() == 1) {
        this.stopTrackingProgress();
        this.triggerEvent("progress"); // Last update
      }
    }), 500);
  },
  stopTrackingProgress: function(){ clearInterval(this.progressInterval); },

  /* Time Tracking -------------------------------------------------------------- */
  manualTimeUpdatesOn: function(){
    this.manualTimeUpdates = true;

    this.addEvent("play", this.trackCurrentTime);
    this.addEvent("pause", this.stopTrackingCurrentTime);
    // timeupdate is also called by .currentTime whenever current time is set

    // Watch for native timeupdate event
    this.tech.addEvent("timeupdate", function(){

      // Remove this listener from the element
      this.removeEvent("timeupdate", arguments.callee);

      // Update known progress support for this playback technology
      this.support.timeupdateEvent = true;

      // Turn off manual progress tracking
      this.player.manualTimeUpdatesOff();
    });
  },

  manualTimeUpdatesOff: function(){
    this.manualTimeUpdates = false;
    this.stopTrackingCurrentTime();
    this.removeEvent("play", this.trackCurrentTime);
    this.removeEvent("pause", this.stopTrackingCurrentTime);
  },

  trackCurrentTime: function(){
    if (this.currentTimeInterval) { this.stopTrackingCurrentTime(); }
    this.currentTimeInterval = setInterval(_V_.proxy(this, function(){
      this.triggerEvent("timeupdate");
    }), 250); // 42 = 24 fps // 250 is what Webkit uses // FF uses 15
  },

  // Turn off play progress tracking (when paused or dragging)
  stopTrackingCurrentTime: function(){ clearInterval(this.currentTimeInterval); },

  /* Player event handlers (how the player reacts to certain events)
  ================================================================================ */
  onEnded: function(){
    if (this.options.loop) {
      this.currentTime(0);
      this.play();
    } else {
      this.pause();
      this.currentTime(0);
      this.pause();
    }
  },

  onPlay: function(){
    _V_.removeClass(this.el, "vjs-paused");
    _V_.addClass(this.el, "vjs-playing");
  },

  onPause: function(){
    _V_.removeClass(this.el, "vjs-playing");
    _V_.addClass(this.el, "vjs-paused");
  },

  onProgress: function(){
    // Add custom event for when source is finished downloading.
    if (this.bufferedPercent() == 1) {
      this.triggerEvent("loadedalldata");
    }
  },

  onError: function(e) {
    _V_.log("Video Error", e);
  },

/* Player API
================================================================================ */

  // Pass values to the playback tech
  techCall: function(method, arg){

    // If it's not ready yet, call method when it is
    if (!this.tech.isReady) {
      this.tech.ready(function(){
        this[method](arg);
      });

    // Otherwise call method now
    } else {
      try {
        this.tech[method](arg);
      } catch(e) {
        _V_.log(e);
      }
    }
  },

  // Get calls can't wait for the tech, and sometimes don't need to.
  techGet: function(method){

    // Make sure tech is ready
    if (this.tech.isReady) {

      // Flash likes to die and reload when you hide or reposition it.
      // In these cases the object methods go away and we get errors.
      // When that happens we'll catch the errors and inform tech that it's not ready any more.
      try {
        return this.tech[method]();
      } catch(e) {

        // When building additional tech libs, an expected method may not be defined yet
        if (this.tech[method] === undefined) {
          _V_.log("Video.js: " + method + " method not defined for "+this.techName+" playback technology.", e);

        } else {

          // When a method isn't available on the object it throws a TypeError
          if (e.name == "TypeError") {
            _V_.log("Video.js: " + method + " unavailable on "+this.techName+" playback technology element.", e);
            this.tech.isReady = false;

          } else {
            _V_.log(e);
          }
        }
      }
    }

    return;
  },

  // Method for calling methods on the current playback technology
  // techCall: function(method, arg){
  // 
  //   // if (this.isReady) {
  //   //   
  //   // } else {
  //   //   _V_.log("The playback technology API is not ready yet. Use player.ready(myFunction)."+" ["+method+"]", arguments.callee.caller.arguments.callee.caller.arguments.callee.caller)
  //   //   return false;
  //   //   // throw new Error("The playback technology API is not ready yet. Use player.ready(myFunction)."+" ["+method+"]");
  //   // }
  // },

  // http://dev.w3.org/html5/spec/video.html#dom-media-play
  play: function(){
    this.techCall("play");
    return this;
  },

  // http://dev.w3.org/html5/spec/video.html#dom-media-pause
  pause: function(){
    this.techCall("pause");
    return this;
  },
  
  // http://dev.w3.org/html5/spec/video.html#dom-media-paused
  // The initial state of paused should be true (in Safari it's actually false)
  paused: function(){
    return (this.techGet("paused") === false) ? false : true;
  },

  // http://dev.w3.org/html5/spec/video.html#dom-media-currenttime
  currentTime: function(seconds){
    if (seconds !== undefined) {

      // Cache the last set value for smoother scrubbing.
      this.values.lastSetCurrentTime = seconds;

      this.techCall("setCurrentTime", seconds);

      // Improve the accuracy of manual timeupdates
      if (this.manualTimeUpdates) { this.triggerEvent("timeupdate"); }

      return this;
    }

    // Cache last currentTime and return
    // Default to 0 seconds
    return this.values.currentTime = (this.techGet("currentTime") || 0);
  },

  // http://dev.w3.org/html5/spec/video.html#dom-media-duration
  // Duration should return NaN if not available. ParseFloat will turn false-ish values to NaN.
  duration: function(){
    return parseFloat(this.techGet("duration"));
  },

  // Calculates how much time is left. Not in spec, but useful.
  remainingTime: function(){
    return this.duration() - this.currentTime();
  },

  // http://dev.w3.org/html5/spec/video.html#dom-media-buffered
  // Buffered returns a timerange object. Kind of like an array of portions of the video that have been downloaded.
  // So far no browsers return more than one range (portion)
  buffered: function(){
    var buffered = this.techGet("buffered"),
        start = 0,
        end = this.values.bufferEnd = this.values.bufferEnd || 0, // Default end to 0 and store in values
        timeRange;

    if (buffered && buffered.length > 0 && buffered.end(0) !== end) {
      end = buffered.end(0);
      // Storing values allows them be overridden by setBufferedFromProgress
      this.values.bufferEnd = end;
    }

    return _V_.createTimeRange(start, end);
  },

  // Calculates amount of buffer is full. Not in spec but useful.
  bufferedPercent: function(){
    return (this.duration()) ? this.buffered().end(0) / this.duration() : 0;
  },

  // http://dev.w3.org/html5/spec/video.html#dom-media-volume
  volume: function(percentAsDecimal){
    var vol;

    if (percentAsDecimal !== undefined) {
      vol = Math.max(0, Math.min(1, parseFloat(percentAsDecimal))); // Force value to between 0 and 1
      this.values.volume = vol;
      this.techCall("setVolume", vol);
      _V_.setLocalStorage("volume", vol);
      return this;
    }

    // Default to 1 when returning current volume.
    vol = parseFloat(this.techGet("volume"));
    return (isNaN(vol)) ? 1 : vol;
  },

  // http://dev.w3.org/html5/spec/video.html#attr-media-muted
  muted: function(muted){
    if (muted !== undefined) {
      this.techCall("setMuted", muted);
      return this;
    }
    return this.techGet("muted") || false; // Default to false
  },

  // http://dev.w3.org/html5/spec/dimension-attributes.html#attr-dim-height
  // Video tag width/height only work in pixels. No percents.
  // We could potentially allow percents but won't for now until we can do testing around it.
  width: function(width, skipListeners){
    if (width !== undefined) {
      this.el.width = width;
      this.el.style.width = width+"px";

      // skipListeners allows us to avoid triggering the resize event when setting both width and height
      if (!skipListeners) { this.triggerEvent("resize"); }
      return this;
    }
    return parseInt(this.el.getAttribute("width"));
  },
  height: function(height){
    if (height !== undefined) {
      this.el.height = height;
      this.el.style.height = height+"px";
      this.triggerEvent("resize");
      return this;
    }
    return parseInt(this.el.getAttribute("height"));
  },
  // Set both width and height at the same time.
  size: function(width, height){
    // Skip resize listeners on width for optimization
    return this.width(width, true).height(height);
  },

  // Check if current tech can support native fullscreen (e.g. with built in controls lik iOS, so not our flash swf)
  supportsFullScreen: function(){ return this.techGet("supportsFullScreen") || false; },

  // Turn on fullscreen (or window) mode
  requestFullScreen: function(){
    var requestFullScreen = _V_.support.requestFullScreen;

    this.isFullScreen = true;

    // Check for browser element fullscreen support
    if (requestFullScreen) {

      // Trigger fullscreenchange event after change
      _V_.addEvent(document, requestFullScreen.eventName, this.proxy(function(){
        this.isFullScreen = document[requestFullScreen.isFullScreen];

        // If cancelling fullscreen, remove event listener.
        if (this.isFullScreen == false) {
          _V_.removeEvent(document, requestFullScreen.eventName, arguments.callee);
        }

        this.triggerEvent("fullscreenchange");
      }));

      // Flash and other plugins get reloaded when you take their parent to fullscreen.
      // To fix that we'll remove the tech, and reload it after the resize has finished.
      if (this.tech.support.fullscreenResize === false && this.options.flash.iFrameMode != true) {

        this.pause();
        this.unloadTech();

        _V_.addEvent(document, requestFullScreen.eventName, this.proxy(function(){
          _V_.removeEvent(document, requestFullScreen.eventName, arguments.callee);
          this.loadTech(this.techName, { src: this.values.src });
        }));

        this.el[requestFullScreen.requestFn]();

      } else {
        this.el[requestFullScreen.requestFn]();
      }

    } else if (this.tech.supportsFullScreen()) {
      this.triggerEvent("fullscreenchange");
      this.techCall("enterFullScreen");

    } else {
      this.triggerEvent("fullscreenchange");
      this.enterFullWindow();
    }

     return this;
   },

   cancelFullScreen: function(){
    var requestFullScreen = _V_.support.requestFullScreen;

    this.isFullScreen = false;

    // Check for browser element fullscreen support
    if (requestFullScreen) {

     // Flash and other plugins get reloaded when you take their parent to fullscreen.
     // To fix that we'll remove the tech, and reload it after the resize has finished.
     if (this.tech.support.fullscreenResize === false && this.options.flash.iFrameMode != true) {

       this.pause();
       this.unloadTech();

       _V_.addEvent(document, requestFullScreen.eventName, this.proxy(function(){
         _V_.removeEvent(document, requestFullScreen.eventName, arguments.callee);
         this.loadTech(this.techName, { src: this.values.src })
       }));

       document[requestFullScreen.cancelFn]();

     } else {
       document[requestFullScreen.cancelFn]();
     }

    } else if (this.tech.supportsFullScreen()) {
     this.techCall("exitFullScreen");
     this.triggerEvent("fullscreenchange");

    } else {
     this.exitFullWindow();
     this.triggerEvent("fullscreenchange");
    }

    return this;
  },

  // When fullscreen isn't supported we can stretch the video container to as wide as the browser will let us.
  enterFullWindow: function(){
    this.isFullWindow = true;

    // Storing original doc overflow value to return to when fullscreen is off
    this.docOrigOverflow = document.documentElement.style.overflow;

    // Add listener for esc key to exit fullscreen
    _V_.addEvent(document, "keydown", _V_.proxy(this, this.fullWindowOnEscKey));

    // Hide any scroll bars
    document.documentElement.style.overflow = 'hidden';

    // Apply fullscreen styles
    _V_.addClass(document.body, "vjs-full-window");
    _V_.addClass(this.el, "vjs-fullscreen");

    this.triggerEvent("enterFullWindow");
  },
  fullWindowOnEscKey: function(event){
    if (event.keyCode == 27) {
      if (this.isFullScreen == true) {
        this.cancelFullScreen();
      } else {
        this.exitFullWindow();
      }
    }
  },

  exitFullWindow: function(){
    this.isFullWindow = false;
    _V_.removeEvent(document, "keydown", this.fullWindowOnEscKey);

    // Unhide scroll bars.
    document.documentElement.style.overflow = this.docOrigOverflow;

    // Remove fullscreen styles
    _V_.removeClass(document.body, "vjs-full-window");
    _V_.removeClass(this.el, "vjs-fullscreen");

    // Resize the box, controller, and poster to original sizes
    // this.positionAll();
    this.triggerEvent("exitFullWindow");
  },

  selectSource: function(sources){

    // Loop through each playback technology in the options order
    for (var i=0,j=this.options.techOrder;i<j.length;i++) {
      var techName = j[i],
          tech = _V_[techName];
          // tech = _V_.tech[techName];

      // Check if the browser supports this technology
      if (tech.isSupported()) {

        // Loop through each source object
        for (var a=0,b=sources;a<b.length;a++) {
          var source = b[a];

          // Check if source can be played with this technology
          if (tech.canPlaySource.call(this, source)) {

            return { source: source, tech: techName };

          }
        }
      }
    }

    return false;
  },

  // src is a pretty powerful function
  // If you pass it an array of source objects, it will find the best source to play and use that object.src
  //   If the new source requires a new playback technology, it will switch to that.
  // If you pass it an object, it will set the source to object.src
  // If you pass it anything else (url string) it will set the video source to that
  src: function(source){
    // Case: Array of source objects to choose from and pick the best to play
    if (source instanceof Array) {

      var sourceTech = this.selectSource(source),
          source,
          techName;

      if (sourceTech) {
          source = sourceTech.source;
          techName = sourceTech.tech;

        // If this technology is already loaded, set source
        if (techName == this.techName) {
          this.src(source); // Passing the source object

        // Otherwise load this technology with chosen source
        } else {
          this.loadTech(techName, source);
        }
      } else {
        _V_.log("No compatible source and playback technology were found.")
      }

    // Case: Source object { src: "", type: "" ... }
    } else if (source instanceof Object) {

      if (_V_[this.techName].canPlaySource(source)) {
        this.src(source.src);
      } else {
        // Send through tech loop to check for a compatible technology.
        this.src([source]);
      }

    // Case: URL String (http://myvideo...)
    } else {
      // Cache for getting last set source
      this.values.src = source;

      if (!this.isReady) {
        this.ready(function(){
          this.src(source);
        });
      } else {
        this.techCall("src", source);
        if (this.options.preload == "auto") {
          this.load();
        }
        if (this.options.autoplay) {
          this.play();
        }
      }
    }
    return this;
  },

  // Begin loading the src data
  // http://dev.w3.org/html5/spec/video.html#dom-media-load
  load: function(){
    this.techCall("load");
    return this;
  },

  // http://dev.w3.org/html5/spec/video.html#dom-media-currentsrc
  currentSrc: function(){
    return this.techGet("currentSrc") || this.values.src || "";
  },

  // Attributes/Options
  preload: function(value){
    if (value !== undefined) {
      this.techCall("setPreload", value);
      this.options.preload = value;
      return this;
    }
    return this.techGet("preload");
  },
  autoplay: function(value){
    if (value !== undefined) {
      this.techCall("setAutoplay", value);
      this.options.autoplay = value;
      return this;
    }
    return this.techGet("autoplay", value);
  },
  loop: function(value){
    if (value !== undefined) {
      this.techCall("setLoop", value);
      this.options.loop = value;
      return this;
    }
    return this.techGet("loop");
  },

  controls: function(){ return this.options.controls; },
  poster: function(){ return this.techGet("poster"); },
  error: function(){ return this.techGet("error"); },
  ended: function(){ return this.techGet("ended"); }

  // Methods to add support for
  // networkState: function(){ return this.techCall("networkState"); },
  // readyState: function(){ return this.techCall("readyState"); },
  // seeking: function(){ return this.techCall("seeking"); },
  // initialTime: function(){ return this.techCall("initialTime"); },
  // startOffsetTime: function(){ return this.techCall("startOffsetTime"); },
  // played: function(){ return this.techCall("played"); },
  // seekable: function(){ return this.techCall("seekable"); },
  // videoTracks: function(){ return this.techCall("videoTracks"); },
  // audioTracks: function(){ return this.techCall("audioTracks"); },
  // videoWidth: function(){ return this.techCall("videoWidth"); },
  // videoHeight: function(){ return this.techCall("videoHeight"); },
  // defaultPlaybackRate: function(){ return this.techCall("defaultPlaybackRate"); },
  // playbackRate: function(){ return this.techCall("playbackRate"); },
  // mediaGroup: function(){ return this.techCall("mediaGroup"); },
  // controller: function(){ return this.techCall("controller"); },
  // defaultMuted: function(){ return this.techCall("defaultMuted"); }
});

// RequestFullscreen API
(function(){
  var requestFn,
      cancelFn,
      eventName,
      isFullScreen,
      playerProto = _V_.Player.prototype;

  // Current W3C Spec
  // http://dvcs.w3.org/hg/fullscreen/raw-file/tip/Overview.html#api
  // Mozilla Draft: https://wiki.mozilla.org/Gecko:FullScreenAPI#fullscreenchange_event
  if (document.cancelFullscreen !== undefined) {
    requestFn = "requestFullscreen";
    cancelFn = "exitFullscreen";
    eventName = "fullscreenchange";
    isFullScreen = "fullScreen";

  // Webkit (Chrome/Safari) and Mozilla (Firefox) have working implementaitons
  // that use prefixes and vary slightly from the new W3C spec. Specifically, using 'exit' instead of 'cancel',
  // and lowercasing the 'S' in Fullscreen.
  // Other browsers don't have any hints of which version they might follow yet, so not going to try to predict by loopeing through all prefixes.
  } else {

    _V_.each(["moz", "webkit"], function(prefix){

      // https://github.com/zencoder/video-js/pull/128
      if ((prefix != "moz" || document.mozFullScreenEnabled) && document[prefix + "CancelFullScreen"] !== undefined) {
        requestFn = prefix + "RequestFullScreen";
        cancelFn = prefix + "CancelFullScreen";
        eventName = prefix + "fullscreenchange";

        if (prefix == "webkit") {
          isFullScreen = prefix + "IsFullScreen";
        } else {
          isFullScreen = prefix + "FullScreen";
        }
      }

    });

  }

  if (requestFn) {
    _V_.support.requestFullScreen = {
      requestFn: requestFn,
      cancelFn: cancelFn,
      eventName: eventName,
      isFullScreen: isFullScreen
    };
  }

})();/* Playback Technology - Base class for playback technologies
================================================================================ */
_V_.PlaybackTech = _V_.Component.extend({
  init: function(player, options){
    // this._super(player, options);

    // Make playback element clickable
    // _V_.addEvent(this.el, "click", _V_.proxy(this, _V_.PlayToggle.prototype.onClick));

    // this.addEvent("click", this.proxy(this.onClick));

    // player.triggerEvent("techready");
  },
  // destroy: function(){},
  // createElement: function(){},
  onClick: function(){
    if (this.player.options.controls) {
      _V_.PlayToggle.prototype.onClick.call(this);
    }
  }
});

// Create placeholder methods for each that warn when a method isn't supported by the current playback technology
_V_.apiMethods = "play,pause,paused,currentTime,setCurrentTime,duration,buffered,volume,setVolume,muted,setMuted,width,height,supportsFullScreen,enterFullScreen,src,load,currentSrc,preload,setPreload,autoplay,setAutoplay,loop,setLoop,error,networkState,readyState,seeking,initialTime,startOffsetTime,played,seekable,ended,videoTracks,audioTracks,videoWidth,videoHeight,textTracks,defaultPlaybackRate,playbackRate,mediaGroup,controller,controls,defaultMuted".split(",");
_V_.each(_V_.apiMethods, function(methodName){
  _V_.PlaybackTech.prototype[methodName] = function(){
    throw new Error("The '"+methodName+"' method is not available on the playback technology's API");
  }
});

/* HTML5 Playback Technology - Wrapper for HTML5 Media API
================================================================================ */
_V_.html5 = _V_.PlaybackTech.extend({

  init: function(player, options, ready){
    this.player = player;
    this.el = this.createElement();
    this.ready(ready);

    this.addEvent("click", this.proxy(this.onClick));

    var source = options.source;

    // If the element source is already set, we may have missed the loadstart event, and want to trigger it.
    // We don't want to set the source again and interrupt playback.
    if (source && this.el.currentSrc == source.src) {
      player.triggerEvent("loadstart");

    // Otherwise set the source if one was provided.
    } else if (source) {
      this.el.src = source.src;
    }

    // Chrome and Safari both have issues with autoplay.
    // In Safari (5.1.1), when we move the video element into the container div, autoplay doesn't work.
    // In Chrome (15), if you have autoplay + a poster + no controls, the video gets hidden (but audio plays)
    // This fixes both issues. Need to wait for API, so it updates displays correctly
    player.ready(function(){
      if (this.options.autoplay && this.paused()) {
        this.tag.poster = null; // Chrome Fix. Fixed in Chrome v16.
        this.play();
      }
    });

    this.setupTriggers();

    this.triggerReady();
  },

  destroy: function(){
    this.player.tag = false;
    this.removeTriggers();
    this.el.parentNode.removeChild(this.el);
  },

  createElement: function(){
    var html5 = _V_.html5,
        player = this.player,

        // If possible, reuse original tag for HTML5 playback technology element
        el = player.tag,
        newEl;

    // Check if this browser supports moving the element into the box.
    // On the iPhone video will break if you move the element,
    // So we have to create a brand new element.
    if (!el || this.support.movingElementInDOM === false) {

      // If the original tag is still there, remove it.
      if (el) {
        player.el.removeChild(el);
      }

      newEl = _V_.createElement("video", {
        id: el.id || player.el.id + "_html5_api",
        className: el.className || "vjs-tech"
      });

      el = newEl;
      _V_.insertFirst(el, player.el);
    }

    // Update tag settings, in case they were overridden
    _V_.each(["autoplay","preload","loop","muted"], function(attr){ // ,"poster"
      if (player.options[attr] !== null) {
        el[attr] = player.options[attr];
      }
    }, this);

    return el;
  },

  // Make video events trigger player events
  // May seem verbose here, but makes other APIs possible.
  setupTriggers: function(){
    _V_.each.call(this, _V_.html5.events, function(type){
      _V_.addEvent(this.el, type, _V_.proxy(this.player, this.eventHandler));
    });
  },
  removeTriggers: function(){
    _V_.each.call(this, _V_.html5.events, function(type){
      _V_.removeEvent(this.el, type, _V_.proxy(this.player, this.eventHandler));
    });
  },
  eventHandler: function(e){
    e.stopPropagation();
    this.triggerEvent(e);
  },

  play: function(){ this.el.play(); },
  pause: function(){ this.el.pause(); },
  paused: function(){ return this.el.paused; },

  currentTime: function(){ return this.el.currentTime; },
  setCurrentTime: function(seconds){
    try {
      this.el.currentTime = seconds;
      } catch(e) {
        _V_.log(e, "Video isn't ready. (VideoJS)");
      // this.warning(VideoJS.warnings.videoNotReady);
    }
  },

  duration: function(){ return this.el.duration || 0; },
  buffered: function(){ return this.el.buffered; },

  volume: function(){ return this.el.volume; },
  setVolume: function(percentAsDecimal){ this.el.volume = percentAsDecimal; },
  muted: function(){ return this.el.muted; },
  setMuted: function(muted){ this.el.muted = muted },

  width: function(){ return this.el.offsetWidth; },
  height: function(){ return this.el.offsetHeight; },

  supportsFullScreen: function(){
    if (typeof this.el.webkitEnterFullScreen == 'function') {

      // Seems to be broken in Chromium/Chrome && Safari in Leopard
      if (!navigator.userAgent.match("Chrome") && !navigator.userAgent.match("Mac OS X 10.5")) {
        return true;
      }
    }
    return false;
  },

  enterFullScreen: function(){
      try {
        this.el.webkitEnterFullScreen();
      } catch (e) {
        if (e.code == 11) {
          // this.warning(VideoJS.warnings.videoNotReady);
          _V_.log("VideoJS: Video not ready.")
        }
      }
  },
  src: function(src){ this.el.src = src; },
  load: function(){ this.el.load(); },
  currentSrc: function(){ return this.el.currentSrc; },

  preload: function(){ return this.el.preload; },
  setPreload: function(val){ this.el.preload = val; },
  autoplay: function(){ return this.el.autoplay; },
  setAutoplay: function(val){ this.el.autoplay = val; },
  loop: function(){ return this.el.loop; },
  setLoop: function(val){ this.el.loop = val; },

  error: function(){ return this.el.error; },
  // networkState: function(){ return this.el.networkState; },
  // readyState: function(){ return this.el.readyState; },
  seeking: function(){ return this.el.seeking; },
  // initialTime: function(){ return this.el.initialTime; },
  // startOffsetTime: function(){ return this.el.startOffsetTime; },
  // played: function(){ return this.el.played; },
  // seekable: function(){ return this.el.seekable; },
  ended: function(){ return this.el.ended; },
  // videoTracks: function(){ return this.el.videoTracks; },
  // audioTracks: function(){ return this.el.audioTracks; },
  // videoWidth: function(){ return this.el.videoWidth; },
  // videoHeight: function(){ return this.el.videoHeight; },
  // textTracks: function(){ return this.el.textTracks; },
  // defaultPlaybackRate: function(){ return this.el.defaultPlaybackRate; },
  // playbackRate: function(){ return this.el.playbackRate; },
  // mediaGroup: function(){ return this.el.mediaGroup; },
  // controller: function(){ return this.el.controller; },
  controls: function(){ return this.player.options.controls; },
  defaultMuted: function(){ return this.el.defaultMuted; }
});

/* HTML5 Support Testing -------------------------------------------------------- */

_V_.html5.isSupported = function(){
  return !!document.createElement("video").canPlayType;
};

_V_.html5.canPlaySource = function(srcObj){
  return !!document.createElement("video").canPlayType(srcObj.type);
  // TODO: Check Type
  // If no Type, check ext
  // Check Media Type
};

// List of all HTML5 events (various uses).
_V_.html5.events = "loadstart,suspend,abort,error,emptied,stalled,loadedmetadata,loadeddata,canplay,canplaythrough,playing,waiting,seeking,seeked,ended,durationchange,timeupdate,progress,play,pause,ratechange,volumechange".split(",");

/* HTML5 Device Fixes ---------------------------------------------------------- */

_V_.html5.prototype.support = {

  // Support for tech specific full screen. (webkitEnterFullScreen, not requestFullscreen)
  // http://developer.apple.com/library/safari/#documentation/AudioVideo/Reference/HTMLVideoElementClassReference/HTMLVideoElement/HTMLVideoElement.html
  // Seems to be broken in Chromium/Chrome && Safari in Leopard
  fullscreen: (typeof _V_.testVid.webkitEnterFullScreen !== undefined) ? (!_V_.ua.match("Chrome") && !_V_.ua.match("Mac OS X 10.5") ? true : false) : false,

  // In iOS, if you move a video element in the DOM, it breaks video playback.
  movingElementInDOM: !_V_.isIOS()

};

// Android
if (_V_.isAndroid()) {

  // Override Android 2.2 and less canPlayType method which is broken
  if (_V_.androidVersion() < 3) {
    document.createElement("video").constructor.prototype.canPlayType = function(type){
      return (type && type.toLowerCase().indexOf("video/mp4") != -1) ? "maybe" : "";
    };
  }
}


/* VideoJS-SWF - Custom Flash Player with HTML5-ish API - https://github.com/zencoder/video-js-swf
================================================================================ */
_V_.flash = _V_.PlaybackTech.extend({

  init: function(player, options){
    this.player = player;

    var source = options.source,

        // Which element to embed in
        parentEl = options.parentEl,

        // Create a temporary element to be replaced by swf object
        placeHolder = this.el = _V_.createElement("div", { id: parentEl.id + "_temp_flash" }),

        // Generate ID for swf object
        objId = player.el.id+"_flash_api",

        // Store player options in local var for optimization
        playerOptions = player.options,

        // Merge default flashvars with ones passed in to init
        flashVars = _V_.merge({

          // SWF Callback Functions
          readyFunction: "_V_.flash.onReady",
          eventProxyFunction: "_V_.flash.onEvent",
          errorEventProxyFunction: "_V_.flash.onError",

          // Player Settings
          autoplay: playerOptions.autoplay,
          preload: playerOptions.preload,
          loop: playerOptions.loop,
          muted: playerOptions.muted

        }, options.flashVars),

        // Merge default parames with ones passed in
        params = _V_.merge({
          wmode: "opaque", // Opaque is needed to overlay controls, but can affect playback performance
          bgcolor: "#000000" // Using bgcolor prevents a white flash when the object is loading
        }, options.params),

        // Merge default attributes with ones passed in
        attributes = _V_.merge({
          id: objId,
          name: objId, // Both ID and Name needed or swf to identifty itself
          'class': 'vjs-tech'
        }, options.attributes)
    ;

    // If source was supplied pass as a flash var.
    if (source) {
      flashVars.src = encodeURIComponent(_V_.getAbsoluteURL(source.src));
    }

    // Add placeholder to player div
    _V_.insertFirst(placeHolder, parentEl);

    // Having issues with Flash reloading on certain page actions (hide/resize/fullscreen) in certain browsers
    // This allows resetting the playhead when we catch the reload
    if (options.startTime) {
      this.ready(function(){
        this.load();
        this.play();
        this.currentTime(options.startTime);
      });
    }

    // Flash iFrame Mode
    // In web browsers there are multiple instances where changing the parent element or visibility of a plugin causes the plugin to reload.
    // - Firefox just about always. https://bugzilla.mozilla.org/show_bug.cgi?id=90268 (might be fixed by version 13)
    // - Webkit when hiding the plugin
    // - Webkit and Firefox when using requestFullScreen on a parent element
    // Loading the flash plugin into a dynamically generated iFrame gets around most of these issues.
    // Issues that remain include hiding the element and requestFullScreen in Firefox specifically

    // There's on particularly annoying issue with this method which is that Firefox throws a security error on an offsite Flash object loaded into a dynamically created iFrame.
    // Even though the iframe was inserted into a page on the web, Firefox + Flash considers it a local app trying to access an internet file.
    // I tried mulitple ways of setting the iframe src attribute but couldn't find a src that worked well. Tried a real/fake source, in/out of domain.
    // Also tried a method from stackoverflow that caused a security error in all browsers. http://stackoverflow.com/questions/2486901/how-to-set-document-domain-for-a-dynamically-generated-iframe
    // In the end the solution I found to work was setting the iframe window.location.href right before doing a document.write of the Flash object.
    // The only downside of this it seems to trigger another http request to the original page (no matter what's put in the href). Not sure why that is.

    // NOTE (2012-01-29): Cannot get Firefox to load the remote hosted SWF into a dynamically created iFrame
    // Firefox 9 throws a security error, unleess you call location.href right before doc.write.
    //    Not sure why that even works, but it causes the browser to look like it's continuously trying to load the page.
    // Firefox 3.6 keeps calling the iframe onload function anytime I write to it, causing an endless loop.

    if (options.iFrameMode == true && !_V_.isFF) {

      // Create iFrame with vjs-tech class so it's 100% width/height
      var iFrm = _V_.createElement("iframe", {
        id: objId + "_iframe",
        name: objId + "_iframe",
        className: "vjs-tech",
        scrolling: "no",
        marginWidth: 0,
        marginHeight: 0,
        frameBorder: 0
      });

      // Update ready function names in flash vars for iframe window
      flashVars.readyFunction = "ready";
      flashVars.eventProxyFunction = "events";
      flashVars.errorEventProxyFunction = "errors";

      // Tried multiple methods to get this to work in all browsers

      // Tried embedding the flash object in the page first, and then adding a place holder to the iframe, then replacing the placeholder with the page object.
      // The goal here was to try to load the swf URL in the parent page first and hope that got around the firefox security error
      // var newObj = _V_.flash.embed(options.swf, placeHolder, flashVars, params, attributes);
      // (in onload)
      //  var temp = _V_.createElement("a", { id:"asdf", innerHTML: "asdf" } );
      //  iDoc.body.appendChild(temp);

      // Tried embedding the flash object through javascript in the iframe source.
      // This works in webkit but still triggers the firefox security error
      // iFrm.src = "javascript: document.write('"+_V_.flash.getEmbedCode(options.swf, flashVars, params, attributes)+"');";

      // Tried an actual local iframe just to make sure that works, but it kills the easiness of the CDN version if you require the user to host an iframe
      // We should add an option to host the iframe locally though, because it could help a lot of issues.
      // iFrm.src = "iframe.html";

      // Wait until iFrame has loaded to write into it.
      _V_.addEvent(iFrm, "load", _V_.proxy(this, function(){

        var iDoc, objTag, swfLoc,
            iWin = iFrm.contentWindow,
            varString = "";


        // The one working method I found was to use the iframe's document.write() to create the swf object
        // This got around the security issue in all browsers except firefox.
        // I did find a hack where if I call the iframe's window.location.href="", it would get around the security error
        // However, the main page would look like it was loading indefinitely (URL bar loading spinner would never stop)
        // Plus Firefox 3.6 didn't work no matter what I tried.
        // if (_V_.ua.match("Firefox")) {
        //   iWin.location.href = "";
        // }

        // Get the iFrame's document depending on what the browser supports
        iDoc = iFrm.contentDocument ? iFrm.contentDocument : iFrm.contentWindow.document;

        // Tried ensuring both document domains were the same, but they already were, so that wasn't the issue.
        // Even tried adding /. that was mentioned in a browser security writeup
        // document.domain = document.domain+"/.";
        // iDoc.domain = document.domain+"/.";

        // Tried adding the object to the iframe doc's innerHTML. Security error in all browsers.
        // iDoc.body.innerHTML = swfObjectHTML;

        // Tried appending the object to the iframe doc's body. Security error in all browsers.
        // iDoc.body.appendChild(swfObject);

        // Using document.write actually got around the security error that browsers were throwing.
        // Again, it's a dynamically generated (same domain) iframe, loading an external Flash swf.
        // Not sure why that's a security issue, but apparently it is.
        iDoc.write(_V_.flash.getEmbedCode(options.swf, flashVars, params, attributes));

        // Setting variables on the window needs to come after the doc write because otherwise they can get reset in some browsers
        // So far no issues with swf ready event being called before it's set on the window.
        iWin.player = this.player;

        // Create swf ready function for iFrame window
        iWin.ready = _V_.proxy(this.player, function(currSwf){
          var el = iDoc.getElementById(currSwf),
              player = this,
              tech = player.tech;

          // Update reference to playback technology element
          tech.el = el;

          // Now that the element is ready, make a click on the swf play the video
          _V_.addEvent(el, "click", tech.proxy(tech.onClick));

          // Make sure swf is actually ready. Sometimes the API isn't actually yet.
          _V_.flash.checkReady(tech);
        });

        // Create event listener for all swf events
        iWin.events = _V_.proxy(this.player, function(swfID, eventName, other){
          var player = this;
          if (player && player.techName == "flash") {
            player.triggerEvent(eventName);
          }
        });

        // Create error listener for all swf errors
        iWin.errors = _V_.proxy(this.player, function(swfID, eventName){
          _V_.log("Flash Error", eventName);
        });

      }));

      // Replace placeholder with iFrame (it will load now)
      placeHolder.parentNode.replaceChild(iFrm, placeHolder);

    // If not using iFrame mode, embed as normal object
    } else {
      _V_.flash.embed(options.swf, placeHolder, flashVars, params, attributes);
    }
  },

  destroy: function(){
    this.el.parentNode.removeChild(this.el);
  },

  // setupTriggers: function(){}, // Using global onEvent func to distribute events

  play: function(){ this.el.vjs_play(); },
  pause: function(){ this.el.vjs_pause(); },
  src: function(src){
    // Make sure source URL is abosolute.
    src = _V_.getAbsoluteURL(src);

    this.el.vjs_src(src);

    // Currently the SWF doesn't autoplay if you load a source later.
    // e.g. Load player w/ no source, wait 2s, set src.
    if (this.player.autoplay()) {
      var tech = this;
      setTimeout(function(){ tech.play(); }, 0);
    }
  },
  load: function(){ this.el.vjs_load(); },
  poster: function(){ this.el.vjs_getProperty("poster"); },

  buffered: function(){
    return _V_.createTimeRange(0, this.el.vjs_getProperty("buffered"));
  },

  supportsFullScreen: function(){
    return false; // Flash does not allow fullscreen through javascript
  },
  enterFullScreen: function(){
    return false;
  }
});

// Create setters and getters for attributes
(function(){

  var api = _V_.flash.prototype,
      readWrite = "preload,currentTime,defaultPlaybackRate,playbackRate,autoplay,loop,mediaGroup,controller,controls,volume,muted,defaultMuted".split(","),
      readOnly = "error,currentSrc,networkState,readyState,seeking,initialTime,duration,startOffsetTime,paused,played,seekable,ended,videoTracks,audioTracks,videoWidth,videoHeight,textTracks".split(","),
      callOnly = "load,play,pause".split(",");
      // Overridden: buffered

      createSetter = function(attr){
        var attrUpper = attr.charAt(0).toUpperCase() + attr.slice(1);
        api["set"+attrUpper] = function(val){ return this.el.vjs_setProperty(attr, val); };
      },

      createGetter = function(attr){
        api[attr] = function(){ return this.el.vjs_getProperty(attr); };
      }
  ;

  // Create getter and setters for all read/write attributes
  _V_.each(readWrite, function(attr){
    createGetter(attr);
    createSetter(attr);
  });

  // Create getters for read-only attributes
  _V_.each(readOnly, function(attr){
    createGetter(attr);
  });

})();

/* Flash Support Testing -------------------------------------------------------- */

_V_.flash.isSupported = function(){
  return _V_.flash.version()[0] >= 10;
  // return swfobject.hasFlashPlayerVersion("10");
};

_V_.flash.canPlaySource = function(srcObj){
  if (srcObj.type in _V_.flash.prototype.support.formats) { return "maybe"; }
};

_V_.flash.prototype.support = {
  formats: {
    "video/flv": "FLV",
    "video/x-flv": "FLV",
    "video/mp4": "MP4",
    "video/m4v": "MP4"
  },

  // Optional events that we can manually mimic with timers
  progressEvent: false,
  timeupdateEvent: false,

  // Resizing plugins using request fullscreen reloads the plugin
  fullscreenResize: false,

  // Resizing plugins in Firefox always reloads the plugin (e.g. full window mode)
  parentResize: !(_V_.ua.match("Firefox"))
};

_V_.flash.onReady = function(currSwf){

  var el = _V_.el(currSwf);

  // Get player from box
  // On firefox reloads, el might already have a player
  var player = el.player || el.parentNode.player,
      tech = player.tech;

  // Reference player on tech element
  el.player = player;

  // Update reference to playback technology element
  tech.el = el;

  // Now that the element is ready, make a click on the swf play the video
  tech.addEvent("click", tech.onClick);

  _V_.flash.checkReady(tech);
};

// The SWF isn't alwasy ready when it says it is. Sometimes the API functions still need to be added to the object.
// If it's not ready, we set a timeout to check again shortly.
_V_.flash.checkReady = function(tech){

  // Check if API property exists
  if (tech.el.vjs_getProperty) {

    // If so, tell tech it's ready
    tech.triggerReady();

  // Otherwise wait longer.
  } else {

    setTimeout(function(){
      _V_.flash.checkReady(tech);
    }, 50);

  }
};

// Trigger events from the swf on the player
_V_.flash.onEvent = function(swfID, eventName){
  var player = _V_.el(swfID).player;
  player.triggerEvent(eventName);
};

// Log errors from the swf
_V_.flash.onError = function(swfID, err){
  var player = _V_.el(swfID).player;
  player.triggerEvent("error");
  _V_.log("Flash Error", err, swfID);
};

// Flash Version Check
_V_.flash.version = function(){
  var version = '0,0,0'

  // IE
  try {
    version = new ActiveXObject('ShockwaveFlash.ShockwaveFlash').GetVariable('$version').replace(/\D+/g, ',').match(/^,?(.+),?$/)[1];

  // other browsers
  } catch(e) {
    try {
      if (navigator.mimeTypes["application/x-shockwave-flash"].enabledPlugin){
        version = (navigator.plugins["Shockwave Flash 2.0"] || navigator.plugins["Shockwave Flash"]).description.replace(/\D+/g, ",").match(/^,?(.+),?$/)[1];
      }
    } catch(e) {}
  }
  return version.split(",");
}

// Flash embedding method. Only used in non-iframe mode
_V_.flash.embed = function(swf, placeHolder, flashVars, params, attributes){
  var code = _V_.flash.getEmbedCode(swf, flashVars, params, attributes),

      // Get element by embedding code and retrieving created element
      obj = _V_.createElement("div", { innerHTML: code }).childNodes[0],

      par = placeHolder.parentNode
  ;

  placeHolder.parentNode.replaceChild(obj, placeHolder);

  // IE6 seems to have an issue where it won't initialize the swf object after injecting it.
  // This is a dumb temporary fix
  if (_V_.isIE()) {
    var newObj = par.childNodes[0];
    setTimeout(function(){
      newObj.style.display = "block";
    }, 1000);
  }

  return obj;

};

_V_.flash.getEmbedCode = function(swf, flashVars, params, attributes){

  var objTag = '<object type="application/x-shockwave-flash"',
      flashVarsString = '',
      paramsString = ''
      attrsString = '';

  // Convert flash vars to string
  if (flashVars) {
    _V_.eachProp(flashVars, function(key, val){
      flashVarsString += (key + "=" + val + "&amp;");
    });
  }

  // Add swf, flashVars, and other default params
  params = _V_.merge({
    movie: swf,
    flashvars: flashVarsString,
    allowScriptAccess: "always", // Required to talk to swf
    allowNetworking: "all" // All should be default, but having security issues.
  }, params);

  // Create param tags string
  _V_.eachProp(params, function(key, val){
    paramsString += '<param name="'+key+'" value="'+val+'" />';
  });

  attributes = _V_.merge({
    // Add swf to attributes (need both for IE and Others to work)
    data: swf,

    // Default to 100% width/height
    width: "100%",
    height: "100%"

  }, attributes);

  // Create Attributes string
  _V_.eachProp(attributes, function(key, val){
    attrsString += (key + '="' + val + '" ');
  });

  return objTag + attrsString + '>' + paramsString + '</object>';
}
// TEXT TRACKS
// Text tracks are tracks of timed text events.
//    Captions - text displayed over the video for the hearing impared
//    Subtitles - text displayed over the video for those who don't understand langauge in the video
//    Chapters - text displayed in a menu allowing the user to jump to particular points (chapters) in the video
//    Descriptions (not supported yet) - audio descriptions that are read back to the user by a screen reading device

// Player Track Functions - Functions add to the player object for easier access to tracks
_V_.merge(_V_.Player.prototype, {

  // Add an array of text tracks. captions, subtitles, chapters, descriptions
  // Track objects will be stored in the player.textTracks array
  addTextTracks: function(trackObjects){
    var tracks = this.textTracks = (this.textTracks) ? this.textTracks : [],
        i = 0, j = trackObjects.length, track, Kind;

    for (;i<j;i++) {
      // HTML5 Spec says default to subtitles.
      // Uppercase (uc) first letter to match class names
      Kind = _V_.uc(trackObjects[i].kind || "subtitles");

      // Create correct texttrack class. CaptionsTrack, etc.
      track = new _V_[Kind + "Track"](this, trackObjects[i]);

      tracks.push(track);

      // If track.default is set, start showing immediately
      // TODO: Add a process to deterime the best track to show for the specific kind
      // Incase there are mulitple defaulted tracks of the same kind
      // Or the user has a set preference of a specific language that should override the default
      if (track['default']) {
        this.ready(_V_.proxy(track, track.show));
      }
    }

    // Return the track so it can be appended to the display component
    return this;
  },

  // Show a text track
  // disableSameKind: disable all other tracks of the same kind. Value should be a track kind (captions, etc.)
  showTextTrack: function(id, disableSameKind){
    var tracks = this.textTracks,
        i = 0,
        j = tracks.length,
        track, showTrack, kind;

    // Find Track with same ID
    for (;i<j;i++) {
      track = tracks[i];
      if (track.id === id) {
        track.show();
        showTrack = track;

      // Disable tracks of the same kind
      } else if (disableSameKind && track.kind == disableSameKind && track.mode > 0) {
        track.disable();
      }
    }

    // Get track kind from shown track or disableSameKind
    kind = (showTrack) ? showTrack.kind : ((disableSameKind) ? disableSameKind : false);

    // Trigger trackchange event, captionstrackchange, subtitlestrackchange, etc.
    if (kind) {
      this.triggerEvent(kind+"trackchange");
    }

    return this;
  }

});

// Track Class
// Contains track methods for loading, showing, parsing cues of tracks
_V_.Track = _V_.Component.extend({

  init: function(player, options){
    this._super(player, options);

    // Apply track info to track object
    // Options will often be a track element
    _V_.merge(this, {
      // Build ID if one doesn't exist
      id: options.id || ("vjs_" + options.kind + "_" + options.language + "_" + _V_.guid++),

      src: options.src,

      // If default is used, subtitles/captions to start showing
      "default": options["default"], // 'default' is reserved-ish
      title: options.title,

      // Language - two letter string to represent track language, e.g. "en" for English
      // readonly attribute DOMString language;
      language: options.srclang,

      // Track label e.g. "English"
      // readonly attribute DOMString label;
      label: options.label,

      // All cues of the track. Cues have a startTime, endTime, text, and other properties.
      // readonly attribute TextTrackCueList cues;
      cues: [],

      // ActiveCues is all cues that are currently showing
      // readonly attribute TextTrackCueList activeCues;
      activeCues: [],

      // ReadyState describes if the text file has been loaded
      // const unsigned short NONE = 0;
      // const unsigned short LOADING = 1;
      // const unsigned short LOADED = 2;
      // const unsigned short ERROR = 3;
      // readonly attribute unsigned short readyState;
      readyState: 0,

      // Mode describes if the track is showing, hidden, or disabled
      // const unsigned short OFF = 0;
      // const unsigned short HIDDEN = 1; (still triggering cuechange events, but not visible)
      // const unsigned short SHOWING = 2;
      // attribute unsigned short mode;
      mode: 0
    });
  },

  // Create basic div to hold cue text
  createElement: function(){
    return this._super("div", {
      className: "vjs-" + this.kind + " vjs-text-track"
    });
  },

  // Show: Mode Showing (2)
  // Indicates that the text track is active. If no attempt has yet been made to obtain the track's cues, the user agent will perform such an attempt momentarily.
  // The user agent is maintaining a list of which cues are active, and events are being fired accordingly.
  // In addition, for text tracks whose kind is subtitles or captions, the cues are being displayed over the video as appropriate;
  // for text tracks whose kind is descriptions, the user agent is making the cues available to the user in a non-visual fashion;
  // and for text tracks whose kind is chapters, the user agent is making available to the user a mechanism by which the user can navigate to any point in the media resource by selecting a cue.
  // The showing by default state is used in conjunction with the default attribute on track elements to indicate that the text track was enabled due to that attribute.
  // This allows the user agent to override the state if a later track is discovered that is more appropriate per the user's preferences.
  show: function(){
    this.activate();

    this.mode = 2;

    // Show element.
    this._super();
  },

  // Hide: Mode Hidden (1)
  // Indicates that the text track is active, but that the user agent is not actively displaying the cues.
  // If no attempt has yet been made to obtain the track's cues, the user agent will perform such an attempt momentarily.
  // The user agent is maintaining a list of which cues are active, and events are being fired accordingly.
  hide: function(){
    // When hidden, cues are still triggered. Disable to stop triggering.
    this.activate();

    this.mode = 1;

    // Hide element.
    this._super();
  },

  // Disable: Mode Off/Disable (0)
  // Indicates that the text track is not active. Other than for the purposes of exposing the track in the DOM, the user agent is ignoring the text track.
  // No cues are active, no events are fired, and the user agent will not attempt to obtain the track's cues.
  disable: function(){
    // If showing, hide.
    if (this.mode == 2) { this.hide(); }

    // Stop triggering cues
    this.deactivate();

    // Switch Mode to Off
    this.mode = 0;
  },

  // Turn on cue tracking. Tracks that are showing OR hidden are active.
  activate: function(){
    // Load text file if it hasn't been yet.
    if (this.readyState == 0) { this.load(); }

    // Only activate if not already active.
    if (this.mode == 0) {
      // Update current cue on timeupdate
      // Using unique ID for proxy function so other tracks don't remove listener
      this.player.addEvent("timeupdate", this.proxy(this.update, this.id));

      // Reset cue time on media end
      this.player.addEvent("ended", this.proxy(this.reset, this.id));

      // Add to display
      if (this.kind == "captions" || this.kind == "subtitles") {
        this.player.textTrackDisplay.addComponent(this);
      }
    }
  },

  // Turn off cue tracking.
  deactivate: function(){
    // Using unique ID for proxy function so other tracks don't remove listener
    this.player.removeEvent("timeupdate", this.proxy(this.update, this.id));
    this.player.removeEvent("ended", this.proxy(this.reset, this.id));
    this.reset(); // Reset

    // Remove from display
    this.player.textTrackDisplay.removeComponent(this);
  },

  // A readiness state
  // One of the following:
  //
  // Not loaded
  // Indicates that the text track is known to exist (e.g. it has been declared with a track element), but its cues have not been obtained.
  //
  // Loading
  // Indicates that the text track is loading and there have been no fatal errors encountered so far. Further cues might still be added to the track.
  //
  // Loaded
  // Indicates that the text track has been loaded with no fatal errors. No new cues will be added to the track except if the text track corresponds to a MutableTextTrack object.
  //
  // Failed to load
  // Indicates that the text track was enabled, but when the user agent attempted to obtain it, this failed in some way (e.g. URL could not be resolved, network error, unknown text track format). Some or all of the cues are likely missing and will not be obtained.
  load: function(){

    // Only load if not loaded yet.
    if (this.readyState == 0) {
      this.readyState = 1;
      _V_.get(this.src, this.proxy(this.parseCues), this.proxy(this.onError));
    }

  },

  onError: function(err){
    this.error = err;
    this.readyState = 3;
    this.triggerEvent("error");
  },

  // Parse the WebVTT text format for cue times.
  // TODO: Separate parser into own class so alternative timed text formats can be used. (TTML, DFXP)
  parseCues: function(srcContent) {
    var cue, time, text,
        lines = srcContent.split("\n"),
        line = "", id;

    for (var i=1, j=lines.length; i<j; i++) {
      // Line 0 should be 'WEBVTT', so skipping i=0

      line = _V_.trim(lines[i]); // Trim whitespace and linebreaks

      if (line) { // Loop until a line with content

        // First line could be an optional cue ID
        // Check if line has the time separator
        if (line.indexOf("-->") == -1) {
          id = line;
          // Advance to next line for timing.
          line = _V_.trim(lines[++i]);
        } else {
          id = this.cues.length;
        }

        // First line - Number
        cue = {
          id: id, // Cue Number
          index: this.cues.length // Position in Array
        };

        // Timing line
        time = line.split(" --> ");
        cue.startTime = this.parseCueTime(time[0]);
        cue.endTime = this.parseCueTime(time[1]);

        // Additional lines - Cue Text
        text = [];

        // Loop until a blank line or end of lines
        // Assumeing trim("") returns false for blank lines
        while (lines[++i] && (line = _V_.trim(lines[i]))) {
          text.push(line);
        }

        cue.text = text.join('<br/>');

        // Add this cue
        this.cues.push(cue);
      }
    }

    this.readyState = 2;
    this.triggerEvent("loaded");
  },

  parseCueTime: function(timeText) {
    var parts = timeText.split(':'),
        time = 0,
        hours, minutes, other, seconds, ms, flags;

    // Check if optional hours place is included
    // 00:00:00.000 vs. 00:00.000
    if (parts.length == 3) {
      hours = parts[0];
      minutes = parts[1];
      other = parts[2];
    } else {
      hours = 0;
      minutes = parts[0];
      other = parts[1];
    }

    // Break other (seconds, milliseconds, and flags) by spaces
    // TODO: Make additional cue layout settings work with flags
    other = other.split(/\s+/)
    // Remove seconds. Seconds is the first part before any spaces.
    seconds = other.splice(0,1)[0];
    // Could use either . or , for decimal
    seconds = seconds.split(/\.|,/);
    // Get milliseconds
    ms = parseFloat(seconds[1]);
    seconds = seconds[0];

    // hours => seconds
    time += parseFloat(hours) * 3600;
    // minutes => seconds
    time += parseFloat(minutes) * 60;
    // Add seconds
    time += parseFloat(seconds);
    // Add milliseconds
    if (ms) { time += ms/1000; }

    return time;
  },

  // Update active cues whenever timeupdate events are triggered on the player.
  update: function(){
    if (this.cues.length > 0) {

      // Get curent player time
      var time = this.player.currentTime();

      // Check if the new time is outside the time box created by the the last update.
      if (this.prevChange === undefined || time < this.prevChange || this.nextChange <= time) {
        var cues = this.cues,

            // Create a new time box for this state.
            newNextChange = this.player.duration(), // Start at beginning of the timeline
            newPrevChange = 0, // Start at end

            reverse = false, // Set the direction of the loop through the cues. Optimized the cue check.
            newCues = [], // Store new active cues.

            // Store where in the loop the current active cues are, to provide a smart starting point for the next loop.
            firstActiveIndex, lastActiveIndex,

            html = "", // Create cue text HTML to add to the display
            cue, i, j; // Loop vars

        // Check if time is going forwards or backwards (scrubbing/rewinding)
        // If we know the direction we can optimize the starting position and direction of the loop through the cues array.
        if (time >= this.nextChange || this.nextChange === undefined) { // NextChange should happen
          // Forwards, so start at the index of the first active cue and loop forward
          i = (this.firstActiveIndex !== undefined) ? this.firstActiveIndex : 0;
        } else {
          // Backwards, so start at the index of the last active cue and loop backward
          reverse = true;
          i = (this.lastActiveIndex !== undefined) ? this.lastActiveIndex : cues.length - 1;
        }

        while (true) { // Loop until broken
          cue = cues[i];

          // Cue ended at this point
          if (cue.endTime <= time) {
            newPrevChange = Math.max(newPrevChange, cue.endTime);

            if (cue.active) {
              cue.active = false;
            }

            // No earlier cues should have an active start time.
            // Nevermind. Assume first cue could have a duration the same as the video.
            // In that case we need to loop all the way back to the beginning.
            // if (reverse && cue.startTime) { break; }

          // Cue hasn't started
          } else if (time < cue.startTime) {
            newNextChange = Math.min(newNextChange, cue.startTime);

            if (cue.active) {
              cue.active = false;
            }

            // No later cues should have an active start time.
            if (!reverse) { break; }

          // Cue is current
          } else {

            if (reverse) {
              // Add cue to front of array to keep in time order
              newCues.splice(0,0,cue);

              // If in reverse, the first current cue is our lastActiveCue
              if (lastActiveIndex === undefined) { lastActiveIndex = i; }
              firstActiveIndex = i;
            } else {
              // Add cue to end of array
              newCues.push(cue);

              // If forward, the first current cue is our firstActiveIndex
              if (firstActiveIndex === undefined) { firstActiveIndex = i; }
              lastActiveIndex = i;
            }

            newNextChange = Math.min(newNextChange, cue.endTime);
            newPrevChange = Math.max(newPrevChange, cue.startTime);

            cue.active = true;
          }

          if (reverse) {
            // Reverse down the array of cues, break if at first
            if (i === 0) { break; } else { i--; }
          } else {
            // Walk up the array fo cues, break if at last
            if (i === cues.length - 1) { break; } else { i++; }
          }

        }

        this.activeCues = newCues;
        this.nextChange = newNextChange;
        this.prevChange = newPrevChange;
        this.firstActiveIndex = firstActiveIndex;
        this.lastActiveIndex = lastActiveIndex;

        this.updateDisplay();

        this.triggerEvent("cuechange");
      }
    }
  },

  // Add cue HTML to display
  updateDisplay: function(){
    var cues = this.activeCues,
        html = "",
        i=0,j=cues.length;

    for (;i<j;i++) {
      html += "<span class='vjs-tt-cue'>"+cues[i].text+"</span>";
    }

    this.el.innerHTML = html;
  },

  // Set all loop helper values back
  reset: function(){
    this.nextChange = 0;
    this.prevChange = this.player.duration();
    this.firstActiveIndex = 0;
    this.lastActiveIndex = 0;
  }

});

// Create specific track types
_V_.CaptionsTrack = _V_.Track.extend({
  kind: "captions"
});

_V_.SubtitlesTrack = _V_.Track.extend({
  kind: "subtitles"
});

_V_.ChaptersTrack = _V_.Track.extend({
  kind: "chapters"
});


/* Text Track Display
================================================================================ */
// Global container for both subtitle and captions text. Simple div container.
_V_.TextTrackDisplay = _V_.Component.extend({

  createElement: function(){
    return this._super("div", {
      className: "vjs-text-track-display"
    });
  }

});

/* Text Track Menu Items
================================================================================ */
_V_.TextTrackMenuItem = _V_.MenuItem.extend({

  init: function(player, options){
    var track = this.track = options.track;

    // Modify options for parent MenuItem class's init.
    options.label = track.label;
    options.selected = track["default"];
    this._super(player, options);

    this.player.addEvent(track.kind + "trackchange", _V_.proxy(this, this.update));
  },

  onClick: function(){
    this._super();
    this.player.showTextTrack(this.track.id, this.track.kind);
  },

  update: function(){
    if (this.track.mode == 2) {
      this.selected(true);
    } else {
      this.selected(false);
    }
  }

});

_V_.OffTextTrackMenuItem = _V_.TextTrackMenuItem.extend({

  init: function(player, options){
    // Create pseudo track info
    // Requires options.kind
    options.track = { kind: options.kind, player: player, label: "Off" }
    this._super(player, options);
  },

  onClick: function(){
    this._super();
    this.player.showTextTrack(this.track.id, this.track.kind);
  },

  update: function(){
    var tracks = this.player.textTracks,
        i=0, j=tracks.length, track,
        off = true;

    for (;i<j;i++) {
      track = tracks[i];
      if (track.kind == this.track.kind && track.mode == 2) {
        off = false;
      }
    }

    if (off) {
      this.selected(true);
    } else {
      this.selected(false);
    }
  }

});

/* Captions Button
================================================================================ */
_V_.TextTrackButton = _V_.Button.extend({

  init: function(player, options){
    this._super(player, options);

    this.menu = this.createMenu();

    if (this.items.length === 0) {
      this.hide();
    }
  },

  createMenu: function(){
    var menu = new _V_.Menu(this.player);

    // Add a title list item to the top
    menu.el.appendChild(_V_.createElement("li", {
      className: "vjs-menu-title",
      innerHTML: _V_.uc(this.kind)
    }));

    // Add an OFF menu item to turn all tracks off
    menu.addItem(new _V_.OffTextTrackMenuItem(this.player, { kind: this.kind }))

    this.items = this.createItems();

    // Add menu items to the menu
    this.each(this.items, function(item){
      menu.addItem(item);
    });

    // Add list to element
    this.addComponent(menu);

    return menu;
  },

  // Create a menu item for each text track
  createItems: function(){
    var items = [];
    this.each(this.player.textTracks, function(track){
      if (track.kind === this.kind) {
        items.push(new _V_.TextTrackMenuItem(this.player, {
          track: track
        }));
      }
    });
    return items;
  },

  buildCSSClass: function(){
    return this.className + " vjs-menu-button " + this._super();
  },

  // Focus - Add keyboard functionality to element
  onFocus: function(){
    // Show the menu, and keep showing when the menu items are in focus
    this.menu.lockShowing();
    // this.menu.el.style.display = "block";

    // When tabbing through, the menu should hide when focus goes from the last menu item to the next tabbed element.
    _V_.one(this.menu.el.childNodes[this.menu.el.childNodes.length - 1], "blur", this.proxy(function(){
      this.menu.unlockShowing();
    }));
  },
  // Can't turn off list display that we turned on with focus, because list would go away.
  onBlur: function(){},

  onClick: function(){
    // When you click the button it adds focus, which will show the menu indefinitely.
    // So we'll remove focus when the mouse leaves the button.
    // Focus is needed for tab navigation.
    this.one("mouseout", this.proxy(function(){
      this.menu.unlockShowing();
      this.el.blur();
    }));
  }

});

_V_.CaptionsButton = _V_.TextTrackButton.extend({
  kind: "captions",
  buttonText: "Captions",
  className: "vjs-captions-button"
});

_V_.SubtitlesButton = _V_.TextTrackButton.extend({
  kind: "subtitles",
  buttonText: "Subtitles",
  className: "vjs-subtitles-button"
});

// Chapters act much differently than other text tracks
// Cues are navigation vs. other tracks of alternative languages
_V_.ChaptersButton = _V_.TextTrackButton.extend({
  kind: "chapters",
  buttonText: "Chapters",
  className: "vjs-chapters-button",

  // Create a menu item for each text track
  createItems: function(chaptersTrack){
    var items = [];

    this.each(this.player.textTracks, function(track){
      if (track.kind === this.kind) {
        items.push(new _V_.TextTrackMenuItem(this.player, {
          track: track
        }));
      }
    });

    return items;
  },

  createMenu: function(){
    var tracks = this.player.textTracks,
        i = 0,
        j = tracks.length,
        track, chaptersTrack,
        items = this.items = [];

    for (;i<j;i++) {
      track = tracks[i];
      if (track.kind == this.kind && track["default"]) {
        if (track.readyState < 2) {
          this.chaptersTrack = track;
          track.addEvent("loaded", this.proxy(this.createMenu));
          return;
        } else {
          chaptersTrack = track;
          break;
        }
      }
    }

    var menu = this.menu = new _V_.Menu(this.player);

    menu.el.appendChild(_V_.createElement("li", {
      className: "vjs-menu-title",
      innerHTML: _V_.uc(this.kind)
    }));

    if (chaptersTrack) {
      var cues = chaptersTrack.cues,
          i = 0, j = cues.length, cue, mi;

      for (;i<j;i++) {
        cue = cues[i];

        mi = new _V_.ChaptersTrackMenuItem(this.player, {
          track: chaptersTrack,
          cue: cue
        });

        items.push(mi);

        menu.addComponent(mi);
      }
    }

    // Add list to element
    this.addComponent(menu);

    if (this.items.length > 0) {
      this.show();
    }

    return menu;
  }

});

_V_.ChaptersTrackMenuItem = _V_.MenuItem.extend({

  init: function(player, options){
    var track = this.track = options.track,
        cue = this.cue = options.cue,
        currentTime = player.currentTime();

    // Modify options for parent MenuItem class's init.
    options.label = cue.text;
    options.selected = (cue.startTime <= currentTime && currentTime < cue.endTime);
    this._super(player, options);

    track.addEvent("cuechange", _V_.proxy(this, this.update));
  },

  onClick: function(){
    this._super();
    this.player.currentTime(this.cue.startTime);
    this.update(this.cue.startTime);
  },

  update: function(time){
    var cue = this.cue,
        currentTime = this.player.currentTime();

    // _V_.log(currentTime, cue.startTime);
    if (cue.startTime <= currentTime && currentTime < cue.endTime) {
      this.selected(true);
    } else {
      this.selected(false);
    }
  }

});

// Add Buttons to controlBar
_V_.merge(_V_.ControlBar.prototype.options.components, {
  "subtitlesButton": {},
  "captionsButton": {},
  "chaptersButton": {}
});

// _V_.Cue = _V_.Component.extend({
//   init: function(player, options){
//     this._super(player, options);
//   }
// });// Automatically set up any tags that have a data-setup attribute
_V_.autoSetup = function(){
  var options, vid, player,
      vids = document.getElementsByTagName("video");

  // Check if any media elements exist
  if (vids && vids.length > 0) {

    for (var i=0,j=vids.length; i<j; i++) {
      vid = vids[i];

      // Check if element exists, has getAttribute func.
      // IE seems to consider typeof el.getAttribute == "object" instead of "function" like expected, at least when loading the player immediately.
      if (vid && vid.getAttribute) {

        // Make sure this player hasn't already been set up.
        if (vid.player === undefined) {
          options = vid.getAttribute("data-setup");

          // Check if data-setup attr exists. 
          // We only auto-setup if they've added the data-setup attr.
          if (options !== null) {

            // Parse options JSON
            // If empty string, make it a parsable json object.
            options = JSON.parse(options || "{}");

            // Create new video.js instance.
            player = _V_(vid, options);
          }
        }

      // If getAttribute isn't defined, we need to wait for the DOM.
      } else {
        _V_.autoSetupTimeout(1);
        break;
      }
    }

  // No videos were found, so keep looping unless page is finisehd loading.
  } else if (!_V_.windowLoaded) {
    _V_.autoSetupTimeout(1);
  }
};

// Pause to let the DOM keep processing
_V_.autoSetupTimeout = function(wait){
  setTimeout(_V_.autoSetup, wait);
};

_V_.addEvent(window, "load", function(){
  _V_.windowLoaded = true;
});

// Run Auto-load players
_V_.autoSetup();
// Expose to global
window.VideoJS = window._V_ = VideoJS;

// End self-executing function
})(window);
