/*
 * cap stream level to media size dimension controller
*/

import Event from '../events';
import EventHandler from '../event-handler';

class CapLevelController extends EventHandler {
	constructor(hls) {
    super(hls,
      Event.MEDIA_ATTACHING,
      Event.MANIFEST_PARSED);   
	}
	
	destroy() {
    if (this.hls.config.capLevelToPlayerSize) {
      this.media = null;
      this.autoLevelCapping = Number.POSITIVE_INFINITY;
      if (this.timer) {
        this.timer = clearInterval(this.timer);
      }
    }
  }
	  
	onMediaAttaching(data) {
    this.media = data.media instanceof HTMLVideoElement ? data.media : null;  
  }

  onManifestParsed(data) {
    if (this.hls.config.capLevelToPlayerSize) {
      this.autoLevelCapping = Number.POSITIVE_INFINITY;
      this.levels = data.levels;
      this.hls.firstLevel = this.getMaxLevel(data.firstLevel);
      clearInterval(this.timer);
      this.timer = setInterval(this.detectPlayerSize.bind(this), 1000);
      this.detectPlayerSize();
    }
  }
  
  detectPlayerSize() {
    if (this.media) {
      let levelsLength = this.levels ? this.levels.length : 0;
      if (levelsLength) {
        this.hls.autoLevelCapping = this.getMaxLevel(levelsLength - 1);
        if (this.hls.autoLevelCapping > this.autoLevelCapping) {
          // if auto level capping has a higher value for the previous one, flush the buffer using nextLevelSwitch
          // usually happen when the user go to the fullscreen mode.
          this.hls.streamController.nextLevelSwitch();
        }
        this.autoLevelCapping = this.hls.autoLevelCapping;        
      }  
    }
  }
  
  /*
  * returns level should be the one with the dimensions equal or greater than the media (player) dimensions (so the video will be downscaled)
  */
  getMaxLevel(capLevelIndex) {
    let result,
        i,
        level,
        mWidth = this.mediaWidth,
        mHeight = this.mediaHeight,
        lWidth = 0,
        lHeight = 0;
        
    for (i = 0; i <= capLevelIndex; i++) {
      level = this.levels[i];
      result = i;
      lWidth = level.width;
      lHeight = level.height;
      if (mWidth <= lWidth || mHeight <= lHeight) {
        break;
      }
    }  
    return result;
  }
  
  get contentScaleFactor() {
    let pixelRatio = 1;
    try {
      pixelRatio =  window.devicePixelRatio;
    } catch(e) {}
    return pixelRatio;
  }
  
  get mediaWidth() {
    let width;
    if (this.media) {
      width = this.media.width || this.media.clientWidth || this.media.offsetWidth;
      width *= this.contentScaleFactor;
    }
    return width;
  }
  
  get mediaHeight() {
    let height;
    if (this.media) {
      height = this.media.height || this.media.clientHeight || this.media.offsetHeight;
      height *= this.contentScaleFactor; 
    }
    return height;
  }
}

export default CapLevelController;