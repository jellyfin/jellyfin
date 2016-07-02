/*
 * simple ABR Controller
 *  - compute next level based on last fragment bw heuristics
 *  - implement an abandon rules triggered if we have less than 2 frag buffered and if computed bw shows that we risk buffer stalling
 */

import Event from '../events';
import EventHandler from '../event-handler';
import BufferHelper from '../helper/buffer-helper';
import {ErrorDetails} from '../errors';
import {logger} from '../utils/logger';
import EwmaBandWidthEstimator from './ewma-bandwidth-estimator';

class AbrController extends EventHandler {

  constructor(hls) {
    super(hls, Event.FRAG_LOADING,
               Event.FRAG_LOADED,
               Event.ERROR);
    this.lastLoadedFragLevel = 0;
    this._autoLevelCapping = -1;
    this._nextAutoLevel = -1;
    this.hls = hls;
    this.onCheck = this.abandonRulesCheck.bind(this);
  }

  destroy() {
    this.clearTimer();
    EventHandler.prototype.destroy.call(this);
  }

  onFragLoading(data) {
    if (!this.timer) {
      this.timer = setInterval(this.onCheck, 100);
    }

    // lazy init of bw Estimator, rationale is that we use different params for Live/VoD
    // so we need to wait for stream manifest / playlist type to instantiate it.
    if (!this.bwEstimator) {
      let hls = this.hls,
          level = data.frag.level,
          isLive = hls.levels[level].details.live,
          config = hls.config,
          ewmaFast, ewmaSlow;

      if (isLive) {
        ewmaFast = config.abrEwmaFastLive;
        ewmaSlow = config.abrEwmaSlowLive;
      } else {
        ewmaFast = config.abrEwmaFastVoD;
        ewmaSlow = config.abrEwmaSlowVoD;
      }
      this.bwEstimator = new EwmaBandWidthEstimator(hls,ewmaSlow,ewmaFast,config.abrEwmaDefaultEstimate);
    }

    let frag = data.frag;
    frag.trequest = performance.now();
    this.fragCurrent = frag;
  }

  abandonRulesCheck() {
    /*
      monitor fragment retrieval time...
      we compute expected time of arrival of the complete fragment.
      we compare it to expected time of buffer starvation
    */
    let hls = this.hls, v = hls.media,frag = this.fragCurrent;

    // if loader has been destroyed or loading has been aborted, stop timer and return
    if(!frag.loader || ( frag.loader.stats && frag.loader.stats.aborted)) {
      logger.warn(`frag loader destroy or aborted, disarm abandonRulesCheck`);
      this.clearTimer();
      return;
    }
    /* only monitor frag retrieval time if
    (video not paused OR first fragment being loaded(ready state === HAVE_NOTHING = 0)) AND autoswitching enabled AND not lowest level (=> means that we have several levels) */
    if (v && (!v.paused || !v.readyState) && frag.autoLevel && frag.level) {
      let requestDelay = performance.now() - frag.trequest;
      // monitor fragment load progress after half of expected fragment duration,to stabilize bitrate
      if (requestDelay > (500 * frag.duration)) {
        let levels = hls.levels,
            loadRate = Math.max(1,frag.loaded * 1000 / requestDelay), // byte/s; at least 1 byte/s to avoid division by zero
            // compute expected fragment length using frag duration and level bitrate. also ensure that expected len is gte than already loaded size
            expectedLen = Math.max(frag.loaded, Math.round(frag.duration * levels[frag.level].bitrate / 8));

        let pos = v.currentTime;
        let fragLoadedDelay = (expectedLen - frag.loaded) / loadRate;
        let bufferStarvationDelay = BufferHelper.bufferInfo(v,pos,hls.config.maxBufferHole).end - pos;
        // consider emergency switch down only if we have less than 2 frag buffered AND
        // time to finish loading current fragment is bigger than buffer starvation delay
        // ie if we risk buffer starvation if bw does not increase quickly
        if (bufferStarvationDelay < 2*frag.duration && fragLoadedDelay > bufferStarvationDelay) {
          let fragLevelNextLoadedDelay, nextLoadLevel;
          // lets iterate through lower level and try to find the biggest one that could avoid rebuffering
          // we start from current level - 1 and we step down , until we find a matching level
          for (nextLoadLevel = frag.level - 1 ; nextLoadLevel >=0 ; nextLoadLevel--) {
            // compute time to load next fragment at lower level
            // 0.8 : consider only 80% of current bw to be conservative
            // 8 = bits per byte (bps/Bps)
            fragLevelNextLoadedDelay = frag.duration * levels[nextLoadLevel].bitrate / (8 * 0.8 * loadRate);
            logger.log(`fragLoadedDelay/bufferStarvationDelay/fragLevelNextLoadedDelay[${nextLoadLevel}] :${fragLoadedDelay.toFixed(1)}/${bufferStarvationDelay.toFixed(1)}/${fragLevelNextLoadedDelay.toFixed(1)}`);
            if (fragLevelNextLoadedDelay < bufferStarvationDelay) {
              // we found a lower level that be rebuffering free with current estimated bw !
              break;
            }
          }
          // only emergency switch down if it takes less time to load new fragment at lowest level instead
          // of finishing loading current one ...
          if (fragLevelNextLoadedDelay < fragLoadedDelay) {
            // ensure nextLoadLevel is not negative
            nextLoadLevel = Math.max(0,nextLoadLevel);
            // force next load level in auto mode
            hls.nextLoadLevel = nextLoadLevel;
            // update bw estimate for this fragment before cancelling load (this will help reducing the bw)
            this.bwEstimator.sample(requestDelay,frag.loaded);
            // abort fragment loading ...
            logger.warn(`loading too slow, abort fragment loading and switch to level ${nextLoadLevel}`);
            //abort fragment loading
            frag.loader.abort();
            this.clearTimer();
            hls.trigger(Event.FRAG_LOAD_EMERGENCY_ABORTED, {frag: frag});
          }
        }
      }
    }
  }

  onFragLoaded(data) {
    var stats = data.stats;
    // only update stats on first frag loading
    // if same frag is loaded multiple times, it might be in browser cache, and loaded quickly
    // and leading to wrong bw estimation
    if (stats.aborted === undefined && data.frag.loadCounter === 1) {
      this.bwEstimator.sample(performance.now() - stats.trequest,stats.loaded);
    }

    // stop monitoring bw once frag loaded
    this.clearTimer();
    // store level id after successful fragment load
    this.lastLoadedFragLevel = data.frag.level;
    // reset forced auto level value so that next level will be selected
    this._nextAutoLevel = -1;
  }

  onError(data) {
    // stop timer in case of frag loading error
    switch(data.details) {
      case ErrorDetails.FRAG_LOAD_ERROR:
      case ErrorDetails.FRAG_LOAD_TIMEOUT:
        this.clearTimer();
        break;
      default:
        break;
    }
  }

 clearTimer() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
 }

  /** Return the capping/max level value that could be used by automatic level selection algorithm **/
  get autoLevelCapping() {
    return this._autoLevelCapping;
  }

  /** set the capping/max level value that could be used by automatic level selection algorithm **/
  set autoLevelCapping(newLevel) {
    this._autoLevelCapping = newLevel;
  }

  get nextAutoLevel() {
    var hls = this.hls, i, maxAutoLevel, levels = hls.levels, config = hls.config;
    if (this._autoLevelCapping === -1 && levels && levels.length) {
      maxAutoLevel = levels.length - 1;
    } else {
      maxAutoLevel = this._autoLevelCapping;
    }

    // in case next auto level has been forced, return it straight-away (but capped)
    if (this._nextAutoLevel !== -1) {
      return Math.min(this._nextAutoLevel,maxAutoLevel);
    }

    let avgbw = this.bwEstimator ? this.bwEstimator.getEstimate() : config.abrEwmaDefaultEstimate,
        adjustedbw;
    // follow algorithm captured from stagefright :
    // https://android.googlesource.com/platform/frameworks/av/+/master/media/libstagefright/httplive/LiveSession.cpp
    // Pick the highest bandwidth stream below or equal to estimated bandwidth.
    for (i = 0; i <= maxAutoLevel; i++) {
    // consider only 80% of the available bandwidth, but if we are switching up,
    // be even more conservative (70%) to avoid overestimating and immediately
    // switching back.
      if (i <= this.lastLoadedFragLevel) {
        adjustedbw = config.abrBandWidthFactor * avgbw;
      } else {
        adjustedbw = config.abrBandWidthUpFactor * avgbw;
      }
      if (adjustedbw < levels[i].bitrate) {
        return Math.max(0, i - 1);
      }
    }
    return i - 1;
  }

  set nextAutoLevel(nextLevel) {
    this._nextAutoLevel = nextLevel;
  }
}

export default AbrController;

