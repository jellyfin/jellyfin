/**
 *  ADTS parser helper
 */
import {logger} from '../utils/logger';
import {ErrorTypes, ErrorDetails} from '../errors';

 class ADTS {

  static getAudioConfig(observer, data, offset, audioCodec) {
    var adtsObjectType, // :int
        adtsSampleingIndex, // :int
        adtsExtensionSampleingIndex, // :int
        adtsChanelConfig, // :int
        config,
        userAgent = navigator.userAgent.toLowerCase(),
        adtsSampleingRates = [
            96000, 88200,
            64000, 48000,
            44100, 32000,
            24000, 22050,
            16000, 12000,
            11025, 8000,
            7350];
    // byte 2
    adtsObjectType = ((data[offset + 2] & 0xC0) >>> 6) + 1;
    adtsSampleingIndex = ((data[offset + 2] & 0x3C) >>> 2);
    if(adtsSampleingIndex > adtsSampleingRates.length-1) {
      observer.trigger(Event.ERROR, {type: ErrorTypes.MEDIA_ERROR, details: ErrorDetails.FRAG_PARSING_ERROR, fatal: true, reason: `invalid ADTS sampling index:${adtsSampleingIndex}`});
      return;
    }
    adtsChanelConfig = ((data[offset + 2] & 0x01) << 2);
    // byte 3
    adtsChanelConfig |= ((data[offset + 3] & 0xC0) >>> 6);
    logger.log(`manifest codec:${audioCodec},ADTS data:type:${adtsObjectType},sampleingIndex:${adtsSampleingIndex}[${adtsSampleingRates[adtsSampleingIndex]}Hz],channelConfig:${adtsChanelConfig}`);
    // firefox: freq less than 24kHz = AAC SBR (HE-AAC)
    if (userAgent.indexOf('firefox') !== -1) {
      if (adtsSampleingIndex >= 6) {
        adtsObjectType = 5;
        config = new Array(4);
        // HE-AAC uses SBR (Spectral Band Replication) , high frequencies are constructed from low frequencies
        // there is a factor 2 between frame sample rate and output sample rate
        // multiply frequency by 2 (see table below, equivalent to substract 3)
        adtsExtensionSampleingIndex = adtsSampleingIndex - 3;
      } else {
        adtsObjectType = 2;
        config = new Array(2);
        adtsExtensionSampleingIndex = adtsSampleingIndex;
      }
      // Android : always use AAC
    } else if (userAgent.indexOf('android') !== -1) {
      adtsObjectType = 2;
      config = new Array(2);
      adtsExtensionSampleingIndex = adtsSampleingIndex;
    } else {
      /*  for other browsers (chrome ...)
          always force audio type to be HE-AAC SBR, as some browsers do not support audio codec switch properly (like Chrome ...)
      */
      adtsObjectType = 5;
      config = new Array(4);
      // if (manifest codec is HE-AAC or HE-AACv2) OR (manifest codec not specified AND frequency less than 24kHz)
      if ((audioCodec && ((audioCodec.indexOf('mp4a.40.29') !== -1) ||
                          (audioCodec.indexOf('mp4a.40.5') !== -1))) ||
          (!audioCodec && adtsSampleingIndex >= 6)) {
        // HE-AAC uses SBR (Spectral Band Replication) , high frequencies are constructed from low frequencies
        // there is a factor 2 between frame sample rate and output sample rate
        // multiply frequency by 2 (see table below, equivalent to substract 3)
        adtsExtensionSampleingIndex = adtsSampleingIndex - 3;
      } else {
        // if (manifest codec is AAC) AND (frequency less than 24kHz AND nb channel is 1) OR (manifest codec not specified and mono audio)
        // Chrome fails to play back with low frequency AAC LC mono when initialized with HE-AAC.  This is not a problem with stereo.
        if (audioCodec && audioCodec.indexOf('mp4a.40.2') !== -1 && (adtsSampleingIndex >= 6 && adtsChanelConfig === 1) ||
            (!audioCodec && adtsChanelConfig === 1)) {
          adtsObjectType = 2;
          config = new Array(2);
        }
        adtsExtensionSampleingIndex = adtsSampleingIndex;
      }
    }
    /* refer to http://wiki.multimedia.cx/index.php?title=MPEG-4_Audio#Audio_Specific_Config
        ISO 14496-3 (AAC).pdf - Table 1.13 — Syntax of AudioSpecificConfig()
      Audio Profile / Audio Object Type
      0: Null
      1: AAC Main
      2: AAC LC (Low Complexity)
      3: AAC SSR (Scalable Sample Rate)
      4: AAC LTP (Long Term Prediction)
      5: SBR (Spectral Band Replication)
      6: AAC Scalable
     sampling freq
      0: 96000 Hz
      1: 88200 Hz
      2: 64000 Hz
      3: 48000 Hz
      4: 44100 Hz
      5: 32000 Hz
      6: 24000 Hz
      7: 22050 Hz
      8: 16000 Hz
      9: 12000 Hz
      10: 11025 Hz
      11: 8000 Hz
      12: 7350 Hz
      13: Reserved
      14: Reserved
      15: frequency is written explictly
      Channel Configurations
      These are the channel configurations:
      0: Defined in AOT Specifc Config
      1: 1 channel: front-center
      2: 2 channels: front-left, front-right
    */
    // audioObjectType = profile => profile, the MPEG-4 Audio Object Type minus 1
    config[0] = adtsObjectType << 3;
    // samplingFrequencyIndex
    config[0] |= (adtsSampleingIndex & 0x0E) >> 1;
    config[1] |= (adtsSampleingIndex & 0x01) << 7;
    // channelConfiguration
    config[1] |= adtsChanelConfig << 3;
    if (adtsObjectType === 5) {
      // adtsExtensionSampleingIndex
      config[1] |= (adtsExtensionSampleingIndex & 0x0E) >> 1;
      config[2] = (adtsExtensionSampleingIndex & 0x01) << 7;
      // adtsObjectType (force to 2, chrome is checking that object type is less than 5 ???
      //    https://chromium.googlesource.com/chromium/src.git/+/master/media/formats/mp4/aac.cc
      config[2] |= 2 << 2;
      config[3] = 0;
    }
    return {config: config, samplerate: adtsSampleingRates[adtsSampleingIndex], channelCount: adtsChanelConfig, codec: ('mp4a.40.' + adtsObjectType)};
  }
}

export default ADTS;
