/*
*
* All objects in the event handling chain should inherit from this class
*
*/

import {logger} from './utils/logger';
import {ErrorTypes, ErrorDetails} from './errors';

class EventHandler {

  constructor(hls, ...events) {
    this.hls = hls;
    this.onEvent = this.onEvent.bind(this);
    this.handledEvents = events;
    this.useGenericHandler = true;

    this.registerListeners();
  }

  destroy() {
    this.unregisterListeners();
  }

  isEventHandler() {
    return typeof this.handledEvents === 'object' && this.handledEvents.length && typeof this.onEvent === 'function';
  }

  registerListeners() {
    if (this.isEventHandler()) {
      this.handledEvents.forEach(function(event) {
        if (event === 'hlsEventGeneric') {
          throw new Error('Forbidden event name: ' + event);
        }
        this.hls.on(event, this.onEvent);
      }.bind(this));
    }
  }

  unregisterListeners() {
    if (this.isEventHandler()) {
      this.handledEvents.forEach(function(event) {
        this.hls.off(event, this.onEvent);
      }.bind(this));
    }
  }

  /*
  * arguments: event (string), data (any)
  */
  onEvent(event, data) {
    this.onEventGeneric(event, data);
  }

  onEventGeneric(event, data) {
    var eventToFunction = function(event, data) {
      var funcName = 'on' + event.replace('hls', '');
      if (typeof this[funcName] !== 'function') {
        throw new Error(`Event ${event} has no generic handler in this ${this.constructor.name} class (tried ${funcName})`);
      }
      return this[funcName].bind(this, data);
    };
    try {
      eventToFunction.call(this, event, data).call();
    } catch (err) {
      logger.error(`internal error happened while processing ${event}:${err.message}`);
      this.hls.trigger(Event.ERROR, {type: ErrorTypes.OTHER_ERROR, details: ErrorDetails.INTERNAL_EXCEPTION, fatal: false, event : event, err : err});
    }
  }
}

export default EventHandler;
