/**
 * fMP4 remuxer
*/


import Event from '../events';
import {logger} from '../utils/logger';
import MP4 from '../remux/mp4-generator';
import {ErrorTypes, ErrorDetails} from '../errors';

class MP4Remuxer {
  constructor(observer) {
    this.observer = observer;
    this.ISGenerated = false;
    this.PES2MP4SCALEFACTOR = 4;
    this.PES_TIMESCALE = 90000;
    this.MP4_TIMESCALE = this.PES_TIMESCALE / this.PES2MP4SCALEFACTOR;
  }

  get timescale() {
    return this.MP4_TIMESCALE;
  }

  destroy() {
  }

  insertDiscontinuity() {
    this._initPTS = this._initDTS = this.nextAacPts = this.nextAvcDts = undefined;
  }

  switchLevel() {
    this.ISGenerated = false;
  }

  remux(audioTrack,videoTrack,id3Track,timeOffset, contiguous) {
    // generate Init Segment if needed
    if (!this.ISGenerated) {
      this.generateIS(audioTrack,videoTrack,timeOffset);
    }
    //logger.log('nb AVC samples:' + videoTrack.samples.length);
    if (videoTrack.samples.length) {
      this.remuxVideo(videoTrack,timeOffset,contiguous);
    }
    //logger.log('nb AAC samples:' + audioTrack.samples.length);
    if (audioTrack.samples.length) {
      this.remuxAudio(audioTrack,timeOffset,contiguous);
    }
    //logger.log('nb ID3 samples:' + audioTrack.samples.length);
    if (id3Track.samples.length) {
      this.remuxID3(id3Track,timeOffset);
    }
    //notify end of parsing
    this.observer.trigger(Event.FRAG_PARSED);
  }

  generateIS(audioTrack,videoTrack,timeOffset) {
    var observer = this.observer,
        audioSamples = audioTrack.samples,
        videoSamples = videoTrack.samples,
        nbAudio = audioSamples.length,
        nbVideo = videoSamples.length,
        pesTimeScale = this.PES_TIMESCALE;

    if(nbAudio === 0 && nbVideo === 0) {
      observer.trigger(Event.ERROR, {type : ErrorTypes.MEDIA_ERROR, details: ErrorDetails.FRAG_PARSING_ERROR, fatal: false, reason: 'no audio/video samples found'});
    } else if (nbVideo === 0) {
      //audio only
      if (audioTrack.config) {
         observer.trigger(Event.FRAG_PARSING_INIT_SEGMENT, {
          audioMoov: MP4.initSegment([audioTrack]),
          audioCodec : audioTrack.codec,
          audioChannelCount : audioTrack.channelCount
        });
        this.ISGenerated = true;
      }
      if (this._initPTS === undefined) {
        // remember first PTS of this demuxing context
        this._initPTS = audioSamples[0].pts - pesTimeScale * timeOffset;
        this._initDTS = audioSamples[0].dts - pesTimeScale * timeOffset;
      }
    } else
    if (nbAudio === 0) {
      //video only
      if (videoTrack.sps && videoTrack.pps) {
         observer.trigger(Event.FRAG_PARSING_INIT_SEGMENT, {
          videoMoov: MP4.initSegment([videoTrack]),
          videoCodec: videoTrack.codec,
          videoWidth: videoTrack.width,
          videoHeight: videoTrack.height
        });
        this.ISGenerated = true;
        if (this._initPTS === undefined) {
          // remember first PTS of this demuxing context
          this._initPTS = videoSamples[0].pts - pesTimeScale * timeOffset;
          this._initDTS = videoSamples[0].dts - pesTimeScale * timeOffset;
        }
      }
    } else {
      //audio and video
      if (audioTrack.config && videoTrack.sps && videoTrack.pps) {
          observer.trigger(Event.FRAG_PARSING_INIT_SEGMENT, {
          audioMoov: MP4.initSegment([audioTrack]),
          audioCodec: audioTrack.codec,
          audioChannelCount: audioTrack.channelCount,
          videoMoov: MP4.initSegment([videoTrack]),
          videoCodec: videoTrack.codec,
          videoWidth: videoTrack.width,
          videoHeight: videoTrack.height
        });
        this.ISGenerated = true;
        if (this._initPTS === undefined) {
          // remember first PTS of this demuxing context
          this._initPTS = Math.min(videoSamples[0].pts, audioSamples[0].pts) - pesTimeScale * timeOffset;
          this._initDTS = Math.min(videoSamples[0].dts, audioSamples[0].dts) - pesTimeScale * timeOffset;
        }
      }
    }
  }

  remuxVideo(track, timeOffset, contiguous) {
    var view,
        offset = 8,
        pesTimeScale = this.PES_TIMESCALE,
        pes2mp4ScaleFactor = this.PES2MP4SCALEFACTOR,
        avcSample,
        mp4Sample,
        mp4SampleLength,
        unit,
        mdat, moof,
        firstPTS, firstDTS, lastDTS,
        pts, dts, ptsnorm, dtsnorm,
        samples = [];
    /* concatenate the video data and construct the mdat in place
      (need 8 more bytes to fill length and mpdat type) */
    mdat = new Uint8Array(track.len + (4 * track.nbNalu) + 8);
    view = new DataView(mdat.buffer);
    view.setUint32(0, mdat.byteLength);
    mdat.set(MP4.types.mdat, 4);
    while (track.samples.length) {
      avcSample = track.samples.shift();
      mp4SampleLength = 0;
      // convert NALU bitstream to MP4 format (prepend NALU with size field)
      while (avcSample.units.units.length) {
        unit = avcSample.units.units.shift();
        view.setUint32(offset, unit.data.byteLength);
        offset += 4;
        mdat.set(unit.data, offset);
        offset += unit.data.byteLength;
        mp4SampleLength += 4 + unit.data.byteLength;
      }
      pts = avcSample.pts - this._initDTS;
      dts = avcSample.dts - this._initDTS;
      // ensure DTS is not bigger than PTS
      dts = Math.min(pts,dts);
      //logger.log(`Video/PTS/DTS:${pts}/${dts}`);
      // if not first AVC sample of video track, normalize PTS/DTS with previous sample value
      // and ensure that sample duration is positive
      if (lastDTS !== undefined) {
        ptsnorm = this._PTSNormalize(pts, lastDTS);
        dtsnorm = this._PTSNormalize(dts, lastDTS);
        var sampleDuration = (dtsnorm - lastDTS) / pes2mp4ScaleFactor;
        if (sampleDuration <= 0) {
          logger.log(`invalid sample duration at PTS/DTS: ${avcSample.pts}/${avcSample.dts}:${sampleDuration}`);
          sampleDuration = 1;
        }
        mp4Sample.duration = sampleDuration;
      } else {
        var nextAvcDts = this.nextAvcDts,delta;
        // first AVC sample of video track, normalize PTS/DTS
        ptsnorm = this._PTSNormalize(pts, nextAvcDts);
        dtsnorm = this._PTSNormalize(dts, nextAvcDts);
        delta = Math.round((dtsnorm - nextAvcDts) / 90);
        // if fragment are contiguous, or delta less than 600ms, ensure there is no overlap/hole between fragments
        if (contiguous || Math.abs(delta) < 600) {
          if (delta) {
            if (delta > 1) {
              logger.log(`AVC:${delta} ms hole between fragments detected,filling it`);
            } else if (delta < -1) {
              logger.log(`AVC:${(-delta)} ms overlapping between fragments detected`);
            }
            // set DTS to next DTS
            dtsnorm = nextAvcDts;
            // offset PTS as well, ensure that PTS is smaller or equal than new DTS
            ptsnorm = Math.max(ptsnorm - delta, dtsnorm);
            logger.log(`Video/PTS/DTS adjusted: ${ptsnorm}/${dtsnorm},delta:${delta}`);
          }
        }
        // remember first PTS of our avcSamples, ensure value is positive
        firstPTS = Math.max(0, ptsnorm);
        firstDTS = Math.max(0, dtsnorm);
      }
      //console.log('PTS/DTS/initDTS/normPTS/normDTS/relative PTS : ${avcSample.pts}/${avcSample.dts}/${this._initDTS}/${ptsnorm}/${dtsnorm}/${(avcSample.pts/4294967296).toFixed(3)}');
      mp4Sample = {
        size: mp4SampleLength,
        duration: 0,
        cts: (ptsnorm - dtsnorm) / pes2mp4ScaleFactor,
        flags: {
          isLeading: 0,
          isDependedOn: 0,
          hasRedundancy: 0,
          degradPrio: 0
        }
      };
      if (avcSample.key === true) {
        // the current sample is a key frame
        mp4Sample.flags.dependsOn = 2;
        mp4Sample.flags.isNonSync = 0;
      } else {
        mp4Sample.flags.dependsOn = 1;
        mp4Sample.flags.isNonSync = 1;
      }
      samples.push(mp4Sample);
      lastDTS = dtsnorm;
    }
    var lastSampleDuration = 0;
    if (samples.length >= 2) {
      lastSampleDuration = samples[samples.length - 2].duration;
      mp4Sample.duration = lastSampleDuration;
    }
    // next AVC sample DTS should be equal to last sample DTS + last sample duration
    this.nextAvcDts = dtsnorm + lastSampleDuration * pes2mp4ScaleFactor;
    track.len = 0;
    track.nbNalu = 0;
    if(samples.length && navigator.userAgent.toLowerCase().indexOf('chrome') > -1) {
      var flags = samples[0].flags;
    // chrome workaround, mark first sample as being a Random Access Point to avoid sourcebuffer append issue
    // https://code.google.com/p/chromium/issues/detail?id=229412
      flags.dependsOn = 2;
      flags.isNonSync = 0;
    }
    track.samples = samples;
    moof = MP4.moof(track.sequenceNumber++, firstDTS / pes2mp4ScaleFactor, track);
    track.samples = [];
    this.observer.trigger(Event.FRAG_PARSING_DATA, {
      moof: moof,
      mdat: mdat,
      startPTS: firstPTS / pesTimeScale,
      endPTS: (ptsnorm + pes2mp4ScaleFactor * lastSampleDuration) / pesTimeScale,
      startDTS: firstDTS / pesTimeScale,
      endDTS: this.nextAvcDts / pesTimeScale,
      type: 'video',
      nb: samples.length
    });
  }

  remuxAudio(track,timeOffset, contiguous) {
    var view,
        offset = 8,
        pesTimeScale = this.PES_TIMESCALE,
        pes2mp4ScaleFactor = this.PES2MP4SCALEFACTOR,
        aacSample, mp4Sample,
        unit,
        mdat, moof,
        firstPTS, firstDTS, lastDTS,
        pts, dts, ptsnorm, dtsnorm,
        samples = [],
        samples0 = [];

    track.samples.forEach(aacSample => {
      if(pts === undefined || aacSample.pts > pts) {
        samples0.push(aacSample);
        pts = aacSample.pts;
      } else {
        logger.warn('dropping past audio frame');
      }
    });

    while (samples0.length) {
      aacSample = samples0.shift();
      unit = aacSample.unit;
      pts = aacSample.pts - this._initDTS;
      dts = aacSample.dts - this._initDTS;
      //logger.log(`Audio/PTS:${aacSample.pts.toFixed(0)}`);
      // if not first sample
      if (lastDTS !== undefined) {
        ptsnorm = this._PTSNormalize(pts, lastDTS);
        dtsnorm = this._PTSNormalize(dts, lastDTS);
        // let's compute sample duration
        mp4Sample.duration = (dtsnorm - lastDTS) / pes2mp4ScaleFactor;
        if (mp4Sample.duration < 0) {
          // not expected to happen ...
          logger.log(`invalid AAC sample duration at PTS:${aacSample.pts}:${mp4Sample.duration}`);
          mp4Sample.duration = 0;
        }
      } else {
        var nextAacPts = this.nextAacPts,delta;
        ptsnorm = this._PTSNormalize(pts, nextAacPts);
        dtsnorm = this._PTSNormalize(dts, nextAacPts);
        delta = Math.round(1000 * (ptsnorm - nextAacPts) / pesTimeScale);
        // if fragment are contiguous, or delta less than 600ms, ensure there is no overlap/hole between fragments
        if (contiguous || Math.abs(delta) < 600) {
          // log delta
          if (delta) {
            if (delta > 0) {
              logger.log(`${delta} ms hole between AAC samples detected,filling it`);
            } else if (delta < 0) {
              // drop overlapping audio frames... browser will deal with it
              logger.log(`${(-delta)} ms overlapping between AAC samples detected, drop frame`);
              track.len -= unit.byteLength;
              continue;
            }
            // set DTS to next DTS
            ptsnorm = dtsnorm = nextAacPts;
          }
        }
        // remember first PTS of our aacSamples, ensure value is positive
        firstPTS = Math.max(0, ptsnorm);
        firstDTS = Math.max(0, dtsnorm);
        /* concatenate the audio data and construct the mdat in place
          (need 8 more bytes to fill length and mdat type) */
        mdat = new Uint8Array(track.len + 8);
        view = new DataView(mdat.buffer);
        view.setUint32(0, mdat.byteLength);
        mdat.set(MP4.types.mdat, 4);
      }
      mdat.set(unit, offset);
      offset += unit.byteLength;
      //console.log('PTS/DTS/initDTS/normPTS/normDTS/relative PTS : ${aacSample.pts}/${aacSample.dts}/${this._initDTS}/${ptsnorm}/${dtsnorm}/${(aacSample.pts/4294967296).toFixed(3)}');
      mp4Sample = {
        size: unit.byteLength,
        cts: 0,
        duration:0,
        flags: {
          isLeading: 0,
          isDependedOn: 0,
          hasRedundancy: 0,
          degradPrio: 0,
          dependsOn: 1,
        }
      };
      samples.push(mp4Sample);
      lastDTS = dtsnorm;
    }
    var lastSampleDuration = 0;
    var nbSamples = samples.length;
    //set last sample duration as being identical to previous sample
    if (nbSamples >= 2) {
      lastSampleDuration = samples[nbSamples - 2].duration;
      mp4Sample.duration = lastSampleDuration;
    }
    if (nbSamples) {
      // next aac sample PTS should be equal to last sample PTS + duration
      this.nextAacPts = ptsnorm + pes2mp4ScaleFactor * lastSampleDuration;
      //logger.log('Audio/PTS/PTSend:' + aacSample.pts.toFixed(0) + '/' + this.nextAacDts.toFixed(0));
      track.len = 0;
      track.samples = samples;
      moof = MP4.moof(track.sequenceNumber++, firstDTS / pes2mp4ScaleFactor, track);
      track.samples = [];
      this.observer.trigger(Event.FRAG_PARSING_DATA, {
        moof: moof,
        mdat: mdat,
        startPTS: firstPTS / pesTimeScale,
        endPTS: this.nextAacPts / pesTimeScale,
        startDTS: firstDTS / pesTimeScale,
        endDTS: (dtsnorm + pes2mp4ScaleFactor * lastSampleDuration) / pesTimeScale,
        type: 'audio',
        nb: nbSamples
      });
    }
  }

  remuxID3(track,timeOffset) {
    var length = track.samples.length, sample;
    // consume samples
    if(length) {
      for(var index = 0; index < length; index++) {
        sample = track.samples[index];
        // setting id3 pts, dts to relative time
        // using this._initPTS and this._initDTS to calculate relative time
        sample.pts = ((sample.pts - this._initPTS) / this.PES_TIMESCALE);
        sample.dts = ((sample.dts - this._initDTS) / this.PES_TIMESCALE);
      }
      this.observer.trigger(Event.FRAG_PARSING_METADATA, {
        samples:track.samples
      });
    }

    track.samples = [];
    timeOffset = timeOffset;
  }

  _PTSNormalize(value, reference) {
    var offset;
    if (reference === undefined) {
      return value;
    }
    if (reference < value) {
      // - 2^33
      offset = -8589934592;
    } else {
      // + 2^33
      offset = 8589934592;
    }
    /* PTS is 33bit (from 0 to 2^33 -1)
      if diff between value and reference is bigger than half of the amplitude (2^32) then it means that
      PTS looping occured. fill the gap */
    while (Math.abs(value - reference) > 4294967296) {
        value += offset;
    }
    return value;
  }

}

export default MP4Remuxer;
