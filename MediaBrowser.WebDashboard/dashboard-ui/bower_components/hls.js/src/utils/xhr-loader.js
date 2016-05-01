/**
 * XHR based logger
*/

import {logger} from '../utils/logger';

class XhrLoader {

  constructor(config) {
    if (config && config.xhrSetup) {
      this.xhrSetup = config.xhrSetup;
    }
  }

  destroy() {
    this.abort();
    this.loader = null;
  }

  abort() {
    var loader = this.loader,
        timeoutHandle = this.timeoutHandle;
    if (loader && loader.readyState !== 4) {
      this.stats.aborted = true;
      loader.abort();
    }
    if (timeoutHandle) {
      window.clearTimeout(timeoutHandle);
    }
  }

  load(url, responseType, onSuccess, onError, onTimeout, timeout, maxRetry, retryDelay, onProgress = null, frag = null) {
    this.url = url;
    if (frag && !isNaN(frag.byteRangeStartOffset) && !isNaN(frag.byteRangeEndOffset)) {
        this.byteRange = frag.byteRangeStartOffset + '-' + (frag.byteRangeEndOffset-1);
    }
    this.responseType = responseType;
    this.onSuccess = onSuccess;
    this.onProgress = onProgress;
    this.onTimeout = onTimeout;
    this.onError = onError;
    this.stats = {trequest: performance.now(), retry: 0};
    this.timeout = timeout;
    this.maxRetry = maxRetry;
    this.retryDelay = retryDelay;
    this.loadInternal();
  }

  loadInternal() {
    var xhr;

    if (typeof XDomainRequest !== 'undefined') {
       xhr = this.loader = new XDomainRequest();
    } else {
       xhr = this.loader = new XMLHttpRequest();
    }

    xhr.onloadend = this.loadend.bind(this);
    xhr.onprogress = this.loadprogress.bind(this);

    xhr.open('GET', this.url, true);
    if (this.byteRange) {
      xhr.setRequestHeader('Range', 'bytes=' + this.byteRange);
    }
    xhr.responseType = this.responseType;
    this.stats.tfirst = null;
    this.stats.loaded = 0;
    if (this.xhrSetup) {
      this.xhrSetup(xhr, this.url);
    }
    this.timeoutHandle = window.setTimeout(this.loadtimeout.bind(this), this.timeout);
    xhr.send();
  }

  loadend(event) {
    var xhr = event.currentTarget,
        status = xhr.status,
        stats = this.stats;
    // don't proceed if xhr has been aborted
    if (!stats.aborted) {
        // http status between 200 to 299 are all successful
        if (status >= 200 && status < 300)  {
          window.clearTimeout(this.timeoutHandle);
          stats.tload = performance.now();
          this.onSuccess(event, stats);
      } else {
        // error ...
        if (stats.retry < this.maxRetry) {
          logger.warn(`${status} while loading ${this.url}, retrying in ${this.retryDelay}...`);
          this.destroy();
          window.setTimeout(this.loadInternal.bind(this), this.retryDelay);
          // exponential backoff
          this.retryDelay = Math.min(2 * this.retryDelay, 64000);
          stats.retry++;
        } else {
          window.clearTimeout(this.timeoutHandle);
          logger.error(`${status} while loading ${this.url}` );
          this.onError(event);
        }
      }
    }
  }

  loadtimeout(event) {
    logger.warn(`timeout while loading ${this.url}` );
    this.onTimeout(event, this.stats);
  }

  loadprogress(event) {
    var stats = this.stats;
    if (stats.tfirst === null) {
      stats.tfirst = performance.now();
    }
    stats.loaded = event.loaded;
    if (this.onProgress) {
      this.onProgress(event, stats);
    }
  }
}

export default XhrLoader;
