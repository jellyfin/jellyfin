/*
 * EWMA Bandwidth Estimator
 *  - heavily inspired from shaka-player
 * Tracks bandwidth samples and estimates available bandwidth.
 * Based on the minimum of two exponentially-weighted moving averages with
 * different half-lives.
 */

import EWMA from '../utils/ewma';


class EwmaBandWidthEstimator {

  constructor(hls,slow,fast,defaultEstimate) {
    this.hls = hls;
    this.defaultEstimate_ = defaultEstimate;
    this.minWeight_ = 0.001;
    this.minDelayMs_ = 50;
    this.slow_ = new EWMA(slow);
    this.fast_ = new EWMA(fast);
  }

  sample(durationMs,numBytes) {
    durationMs = Math.max(durationMs, this.minDelayMs_);
    var bandwidth = 8000* numBytes / durationMs,
    //console.log('instant bw:'+ Math.round(bandwidth));
    // we weight sample using loading duration....
        weight = durationMs / 1000;
    this.fast_.sample(weight,bandwidth);
    this.slow_.sample(weight,bandwidth);
  }


  getEstimate() {
    if (!this.fast_ || !this.slow_ || this.fast_.getTotalWeight() < this.minWeight_) {
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

