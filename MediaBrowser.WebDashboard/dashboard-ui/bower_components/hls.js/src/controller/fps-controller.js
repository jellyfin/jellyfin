/*
 * FPS Controller
*/

import Event from '../events';
import {logger} from '../utils/logger';

class FPSController {

  constructor(hls) {
    this.hls = hls;
    this.timer = setInterval(this.checkFPS, hls.config.fpsDroppedMonitoringPeriod);
  }

  destroy() {
    if (this.timer) {
     clearInterval(this.timer);
    }
  }

  checkFPS() {
    var v = this.hls.video;
    if (v) {
      var decodedFrames = v.webkitDecodedFrameCount, droppedFrames = v.webkitDroppedFrameCount, currentTime = new Date();
      if (decodedFrames) {
        if (this.lastTime) {
          var currentPeriod = currentTime - this.lastTime;
          var currentDropped = droppedFrames - this.lastDroppedFrames;
          var currentDecoded = decodedFrames - this.lastDecodedFrames;
          var decodedFPS = 1000 * currentDecoded / currentPeriod;
          var droppedFPS = 1000 * currentDropped / currentPeriod;
          if (droppedFPS > 0) {
            logger.log(`checkFPS : droppedFPS/decodedFPS:${droppedFPS.toFixed(1)}/${decodedFPS.toFixed(1)}`);
            if (currentDropped > this.hls.config.fpsDroppedMonitoringThreshold * currentDecoded) {
              logger.warn('drop FPS ratio greater than max allowed value');
              this.hls.trigger(Event.FPS_DROP, {currentDropped: currentDropped, currentDecoded: currentDecoded, totalDroppedFrames: droppedFrames});
            }
          }
        }
        this.lastTime = currentTime;
        this.lastDroppedFrames = droppedFrames;
        this.lastDecodedFrames = decodedFrames;
      }
    }
  }
}

export default FPSController;

