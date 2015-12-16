/*
 * simple ABR Controller
*/

import Event from '../events';

class AbrController {

  constructor(hls) {
    this.hls = hls;
    this.lastfetchlevel = 0;
    this._autoLevelCapping = -1;
    this._nextAutoLevel = -1;
    this.onflp = this.onFragmentLoadProgress.bind(this);
    hls.on(Event.FRAG_LOAD_PROGRESS, this.onflp);
  }

  destroy() {
    this.hls.off(Event.FRAG_LOAD_PROGRESS, this.onflp);
  }

  onFragmentLoadProgress(event, data) {
    var stats = data.stats;
    if (stats.aborted === undefined) {
      this.lastfetchduration = (performance.now() - stats.trequest) / 1000;
      this.lastfetchlevel = data.frag.level;
      this.lastbw = (stats.loaded * 8) / this.lastfetchduration;
      //console.log(`fetchDuration:${this.lastfetchduration},bw:${(this.lastbw/1000).toFixed(0)}/${stats.aborted}`);
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
    var lastbw = this.lastbw, hls = this.hls,adjustedbw, i, maxAutoLevel;
    if (this._autoLevelCapping === -1) {
      maxAutoLevel = hls.levels.length - 1;
    } else {
      maxAutoLevel = this._autoLevelCapping;
    }

    if (this._nextAutoLevel !== -1) {
      var nextLevel = Math.min(this._nextAutoLevel,maxAutoLevel);
      if (nextLevel === this.lastfetchlevel) {
        this._nextAutoLevel = -1;
      } else {
        return nextLevel;
      }
    }

    // follow algorithm captured from stagefright :
    // https://android.googlesource.com/platform/frameworks/av/+/master/media/libstagefright/httplive/LiveSession.cpp
    // Pick the highest bandwidth stream below or equal to estimated bandwidth.
    for (i = 0; i <= maxAutoLevel; i++) {
    // consider only 80% of the available bandwidth, but if we are switching up,
    // be even more conservative (70%) to avoid overestimating and immediately
    // switching back.
      if (i <= this.lastfetchlevel) {
        adjustedbw = 0.8 * lastbw;
      } else {
        adjustedbw = 0.7 * lastbw;
      }
      if (adjustedbw < hls.levels[i].bitrate) {
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

