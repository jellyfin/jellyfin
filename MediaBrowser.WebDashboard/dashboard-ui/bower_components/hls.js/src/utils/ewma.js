/*
 * compute an Exponential Weighted moving average
 * - https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average
 *  - heavily inspired from shaka-player
 */

class EWMA {

 //  About half of the estimated value will be from the last |halfLife| samples by weight.
  constructor(halfLife) {
    // Larger values of alpha expire historical data more slowly.
    this.alpha_ = halfLife ? Math.exp(Math.log(0.5) / halfLife) : 0;
    this.estimate_ = 0;
    this.totalWeight_ = 0;
  }

  sample(weight,value) {
    var adjAlpha = Math.pow(this.alpha_, weight);
    this.estimate_ = value * (1 - adjAlpha) + adjAlpha * this.estimate_;
    this.totalWeight_ += weight;
  }

  getTotalWeight() {
    return this.totalWeight_;
  }

  getEstimate() {
    if (this.alpha_) {
      var zeroFactor = 1 - Math.pow(this.alpha_, this.totalWeight_);
      return this.estimate_ / zeroFactor;
    } else {
      return this.estimate_;
    }
  }
}

export default EWMA;
