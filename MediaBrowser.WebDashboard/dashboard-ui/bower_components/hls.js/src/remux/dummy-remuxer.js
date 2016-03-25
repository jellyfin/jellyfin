/**
 * dummy remuxer
*/

class DummyRemuxer {
  constructor(observer) {
    this.PES_TIMESCALE = 90000;
    this.observer = observer;
  }

  get passthrough() {
    return false;
  }

  destroy() {
  }

  insertDiscontinuity() {
  }

  remux(audioTrack,videoTrack,id3Track,textTrack,timeOffset) {
    this._remuxAACSamples(audioTrack,timeOffset);
    this._remuxAVCSamples(videoTrack,timeOffset);
    this._remuxID3Samples(id3Track,timeOffset);
    this._remuxTextSamples(textTrack,timeOffset);
  }

  _remuxAVCSamples(track, timeOffset) {
    var avcSample, unit;
    // loop through track.samples
    while (track.samples.length) {
      avcSample = track.samples.shift();
      // loop through AVC sample NALUs
      while (avcSample.units.units.length) {
        unit = avcSample.units.units.shift();
      }
    }
    //please lint
    timeOffset = timeOffset;
  }

  _remuxAACSamples(track,timeOffset) {
    var aacSample,unit;
    // loop through track.samples
    while (track.samples.length) {
      aacSample = track.samples.shift();
      unit = aacSample.unit;
    }
    //please lint
    timeOffset = timeOffset;
  }

  _remuxID3Samples(track,timeOffset) {
    var id3Sample,unit;
    // loop through track.samples
    while (track.samples.length) {
      id3Sample = track.samples.shift();
      unit = id3Sample.unit;
    }
    //please lint
    timeOffset = timeOffset;
  }

  _remuxTextSamples(track,timeOffset) {
    var textSample,bytes;
    // loop through track.samples
    while (track.samples.length) {
      textSample = track.samples.shift();
      bytes = textSample.bytes;
    }
    //please lint
    timeOffset = timeOffset;
  }
}

export default DummyRemuxer;

