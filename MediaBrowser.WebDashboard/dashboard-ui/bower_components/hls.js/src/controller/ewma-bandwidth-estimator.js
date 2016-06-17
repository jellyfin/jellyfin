/*
 * EWMA Bandwidth Estimator
 *  - heavily inspired from shaka-player
 * Tracks bandwidth samples and estimates available bandwidth.
 * Based on the minimum of two exponentially-weighted moving averages with
 * different half-lives.
 */

import EWMA from '../utils/ewma';


class EwmaBandWidthEstimator {

  constructor(hls) {
    this.hls = hls;
    this.defaultEstimate_ = 5e5; // 500kbps
    this.minWeight_ = 0.001;
    this.minDelayMs_ = 50;
  }

  sample(durationMs,numBytes) {
    durationMs = Math.max(durationMs, this.minDelayMs_);
    var bandwidth = 8000* numBytes / durationMs;
    //console.log('instant bw:'+ Math.round(bandwidth));
    // we weight sample using loading duration....
    var weigth = durationMs / 1000;

    // lazy initialization. this allows to take into account config param changes that could happen after Hls instantiation,
    // but before first fragment loading. this is useful to A/B tests those params
    if(!this.fast_) {
      let config = this.hls.config;
      this.fast_ = new EWMA(config.abrEwmaFast);
      this.slow_ = new EWMA(config.abrEwmaSlow);
    }
    this.fast_.sample(weigth,bandwidth);
    this.slow_.sample(weigth,bandwidth);
  }


  getEstimate() {
    if (!this.fast_ || this.fast_.getTotalWeight() < this.minWeight_) {
      return this.defaultEstimate_;
    }
    //console.log('slow estimate:'+ Math.round(this.slow_.getEstimate()));
    //console.log('fast estimate:'+ Math.round(this.fast_.getEstimate()));
    // Take the minimum of these two estimates.  This should have the effect of
    // adapting down quickly, but up more slowly.
    return Math.min(this.fast_.getEstimate(),this.slow_.getEstimate());
  }

  destroy() {
  }
}
export default EwmaBandWidthEstimator;

