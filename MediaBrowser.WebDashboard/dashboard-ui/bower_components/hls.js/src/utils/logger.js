'use strict';

function noop() {}

const fakeLogger = {
  trace: noop,
  debug: noop,
  log: noop,
  warn: noop,
  info: noop,
  error: noop
};

let exportedLogger = fakeLogger;

//let lastCallTime;
// function formatMsgWithTimeInfo(type, msg) {
//   const now = Date.now();
//   const diff = lastCallTime ? '+' + (now - lastCallTime) : '0';
//   lastCallTime = now;
//   msg = (new Date(now)).toISOString() + ' | [' +  type + '] > ' + msg + ' ( ' + diff + ' ms )';
//   return msg;
// }

function formatMsg(type, msg) {
  msg = '[' +  type + '] > ' + msg;
  return msg;
}

function consolePrintFn(type) {
  const func = window.console[type];
  if (func) {
    return function(...args) {
      if(args[0]) {
        args[0] = formatMsg(type, args[0]);
      }
      func.apply(window.console, args);
    };
  }
  return noop;
}

function exportLoggerFunctions(debugConfig, ...functions) {
  functions.forEach(function(type) {
    exportedLogger[type] = debugConfig[type] ? debugConfig[type].bind(debugConfig) : consolePrintFn(type);
  });
}

export var enableLogs = function(debugConfig) {
  if (debugConfig === true || typeof debugConfig === 'object') {
    exportLoggerFunctions(debugConfig,
      // Remove out from list here to hard-disable a log-level
      //'trace',
      'debug',
      'log',
      'info',
      'warn',
      'error'
    );
    // Some browsers don't allow to use bind on console object anyway
    // fallback to default if needed
    try {
     exportedLogger.log();
    } catch (e) {
      exportedLogger = fakeLogger;
    }
  }
  else {
    exportedLogger = fakeLogger;
  }
};

export var logger = exportedLogger;
