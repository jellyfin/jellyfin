/**
 * Generate MP4 Box
*/

//import Hex from '../utils/hex';
class MP4 {
  static init() {
    MP4.types = {
      avc1: [], // codingname
      avcC: [],
      btrt: [],
      dinf: [],
      dref: [],
      esds: [],
      ftyp: [],
      hdlr: [],
      mdat: [],
      mdhd: [],
      mdia: [],
      mfhd: [],
      minf: [],
      moof: [],
      moov: [],
      mp4a: [],
      mvex: [],
      mvhd: [],
      sdtp: [],
      stbl: [],
      stco: [],
      stsc: [],
      stsd: [],
      stsz: [],
      stts: [],
      tfdt: [],
      tfhd: [],
      traf: [],
      trak: [],
      trun: [],
      trex: [],
      tkhd: [],
      vmhd: [],
      smhd: []
    };

    var i;
    for (i in MP4.types) {
      if (MP4.types.hasOwnProperty(i)) {
        MP4.types[i] = [
          i.charCodeAt(0),
          i.charCodeAt(1),
          i.charCodeAt(2),
          i.charCodeAt(3)
        ];
      }
    }

    var videoHdlr = new Uint8Array([
      0x00, // version 0
      0x00, 0x00, 0x00, // flags
      0x00, 0x00, 0x00, 0x00, // pre_defined
      0x76, 0x69, 0x64, 0x65, // handler_type: 'vide'
      0x00, 0x00, 0x00, 0x00, // reserved
      0x00, 0x00, 0x00, 0x00, // reserved
      0x00, 0x00, 0x00, 0x00, // reserved
      0x56, 0x69, 0x64, 0x65,
      0x6f, 0x48, 0x61, 0x6e,
      0x64, 0x6c, 0x65, 0x72, 0x00 // name: 'VideoHandler'
    ]);

    var audioHdlr = new Uint8Array([
      0x00, // version 0
      0x00, 0x00, 0x00, // flags
      0x00, 0x00, 0x00, 0x00, // pre_defined
      0x73, 0x6f, 0x75, 0x6e, // handler_type: 'soun'
      0x00, 0x00, 0x00, 0x00, // reserved
      0x00, 0x00, 0x00, 0x00, // reserved
      0x00, 0x00, 0x00, 0x00, // reserved
      0x53, 0x6f, 0x75, 0x6e,
      0x64, 0x48, 0x61, 0x6e,
      0x64, 0x6c, 0x65, 0x72, 0x00 // name: 'SoundHandler'
    ]);

    MP4.HDLR_TYPES = {
      'video': videoHdlr,
      'audio': audioHdlr
    };

    var dref = new Uint8Array([
      0x00, // version 0
      0x00, 0x00, 0x00, // flags
      0x00, 0x00, 0x00, 0x01, // entry_count
      0x00, 0x00, 0x00, 0x0c, // entry_size
      0x75, 0x72, 0x6c, 0x20, // 'url' type
      0x00, // version 0
      0x00, 0x00, 0x01 // entry_flags
    ]);

    var stco = new Uint8Array([
      0x00, // version
      0x00, 0x00, 0x00, // flags
      0x00, 0x00, 0x00, 0x00 // entry_count
    ]);

    MP4.STTS = MP4.STSC = MP4.STCO = stco;

    MP4.STSZ = new Uint8Array([
      0x00, // version
      0x00, 0x00, 0x00, // flags
      0x00, 0x00, 0x00, 0x00, // sample_size
      0x00, 0x00, 0x00, 0x00, // sample_count
    ]);
    MP4.VMHD = new Uint8Array([
      0x00, // version
      0x00, 0x00, 0x01, // flags
      0x00, 0x00, // graphicsmode
      0x00, 0x00,
      0x00, 0x00,
      0x00, 0x00 // opcolor
    ]);
    MP4.SMHD = new Uint8Array([
      0x00, // version
      0x00, 0x00, 0x00, // flags
      0x00, 0x00, // balance
      0x00, 0x00 // reserved
    ]);

    MP4.STSD = new Uint8Array([
      0x00, // version 0
      0x00, 0x00, 0x00, // flags
      0x00, 0x00, 0x00, 0x01]);// entry_count

    var majorBrand = new Uint8Array([105,115,111,109]); // isom
    var avc1Brand = new Uint8Array([97,118,99,49]); // avc1
    var minorVersion = new Uint8Array([0, 0, 0, 1]);

    MP4.FTYP = MP4.box(MP4.types.ftyp, majorBrand, minorVersion, majorBrand, avc1Brand);
    MP4.DINF = MP4.box(MP4.types.dinf, MP4.box(MP4.types.dref, dref));
  }

  static box(type) {
  var
    payload = Array.prototype.slice.call(arguments, 1),
    size = 8,
    i = payload.length,
    len = i,
    result;
    // calculate the total size we need to allocate
    while (i--) {
      size += payload[i].byteLength;
    }
    result = new Uint8Array(size);
    result[0] = (size >> 24) & 0xff;
    result[1] = (size >> 16) & 0xff;
    result[2] = (size >> 8) & 0xff;
    result[3] = size  & 0xff;
    result.set(type, 4);
    // copy the payload into the result
    for (i = 0, size = 8; i < len; i++) {
      // copy payload[i] array @ offset size
      result.set(payload[i], size);
      size += payload[i].byteLength;
    }
    return result;
  }

  static hdlr(type) {
    return MP4.box(MP4.types.hdlr, MP4.HDLR_TYPES[type]);
  }

  static mdat(data) {
    return MP4.box(MP4.types.mdat, data);
  }

  static mdhd(timescale, duration) {
    duration *= timescale;
    return MP4.box(MP4.types.mdhd, new Uint8Array([
      0x00, // version 0
      0x00, 0x00, 0x00, // flags
      0x00, 0x00, 0x00, 0x02, // creation_time
      0x00, 0x00, 0x00, 0x03, // modification_time
      (timescale >> 24) & 0xFF,
      (timescale >> 16) & 0xFF,
      (timescale >>  8) & 0xFF,
      timescale & 0xFF, // timescale
      (duration >> 24),
      (duration >> 16) & 0xFF,
      (duration >>  8) & 0xFF,
      duration & 0xFF, // duration
      0x55, 0xc4, // 'und' language (undetermined)
      0x00, 0x00
    ]));
  }

  static mdia(track) {
    return MP4.box(MP4.types.mdia, MP4.mdhd(track.timescale, track.duration), MP4.hdlr(track.type), MP4.minf(track));
  }

  static mfhd(sequenceNumber) {
    return MP4.box(MP4.types.mfhd, new Uint8Array([
      0x00,
      0x00, 0x00, 0x00, // flags
      (sequenceNumber >> 24),
      (sequenceNumber >> 16) & 0xFF,
      (sequenceNumber >>  8) & 0xFF,
      sequenceNumber & 0xFF, // sequence_number
    ]));
  }

  static minf(track) {
    if (track.type === 'audio') {
      return MP4.box(MP4.types.minf, MP4.box(MP4.types.smhd, MP4.SMHD), MP4.DINF, MP4.stbl(track));
    } else {
      return MP4.box(MP4.types.minf, MP4.box(MP4.types.vmhd, MP4.VMHD), MP4.DINF, MP4.stbl(track));
    }
  }

  static moof(sn, baseMediaDecodeTime, track) {
    return MP4.box(MP4.types.moof, MP4.mfhd(sn), MP4.traf(track,baseMediaDecodeTime));
  }
/**
 * @param tracks... (optional) {array} the tracks associated with this movie
 */
  static moov(tracks) {
    var
      i = tracks.length,
      boxes = [];

    while (i--) {
      boxes[i] = MP4.trak(tracks[i]);
    }

    return MP4.box.apply(null, [MP4.types.moov, MP4.mvhd(tracks[0].timescale, tracks[0].duration)].concat(boxes).concat(MP4.mvex(tracks)));
  }

  static mvex(tracks) {
    var
      i = tracks.length,
      boxes = [];

    while (i--) {
      boxes[i] = MP4.trex(tracks[i]);
    }
    return MP4.box.apply(null, [MP4.types.mvex].concat(boxes));
  }

  static mvhd(timescale,duration) {
    duration*=timescale;
    var
      bytes = new Uint8Array([
        0x00, // version 0
        0x00, 0x00, 0x00, // flags
        0x00, 0x00, 0x00, 0x01, // creation_time
        0x00, 0x00, 0x00, 0x02, // modification_time
        (timescale >> 24) & 0xFF,
        (timescale >> 16) & 0xFF,
        (timescale >>  8) & 0xFF,
        timescale & 0xFF, // timescale
        (duration >> 24) & 0xFF,
        (duration >> 16) & 0xFF,
        (duration >>  8) & 0xFF,
        duration & 0xFF, // duration
        0x00, 0x01, 0x00, 0x00, // 1.0 rate
        0x01, 0x00, // 1.0 volume
        0x00, 0x00, // reserved
        0x00, 0x00, 0x00, 0x00, // reserved
        0x00, 0x00, 0x00, 0x00, // reserved
        0x00, 0x01, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x01, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x40, 0x00, 0x00, 0x00, // transformation: unity matrix
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, // pre_defined
        0xff, 0xff, 0xff, 0xff // next_track_ID
      ]);
    return MP4.box(MP4.types.mvhd, bytes);
  }

  static sdtp(track) {
    var
      samples = track.samples || [],
      bytes = new Uint8Array(4 + samples.length),
      flags,
      i;
    // leave the full box header (4 bytes) all zero
    // write the sample table
    for (i = 0; i < samples.length; i++) {
      flags = samples[i].flags;
      bytes[i + 4] = (flags.dependsOn << 4) |
        (flags.isDependedOn << 2) |
        (flags.hasRedundancy);
    }

    return MP4.box(MP4.types.sdtp, bytes);
  }

  static stbl(track) {
    return MP4.box(MP4.types.stbl, MP4.stsd(track), MP4.box(MP4.types.stts, MP4.STTS), MP4.box(MP4.types.stsc, MP4.STSC), MP4.box(MP4.types.stsz, MP4.STSZ), MP4.box(MP4.types.stco, MP4.STCO));
  }

  static avc1(track) {
    var sps = [], pps = [], i, data, len;
    // assemble the SPSs

    for (i = 0; i < track.sps.length; i++) {
      data = track.sps[i];
      len = data.byteLength;
      sps.push((len >>> 8) & 0xFF);
      sps.push((len & 0xFF));
      sps = sps.concat(Array.prototype.slice.call(data)); // SPS
    }

    // assemble the PPSs
    for (i = 0; i < track.pps.length; i++) {
      data = track.pps[i];
      len = data.byteLength;
      pps.push((len >>> 8) & 0xFF);
      pps.push((len & 0xFF));
      pps = pps.concat(Array.prototype.slice.call(data));
    }

    var avcc = MP4.box(MP4.types.avcC, new Uint8Array([
            0x01,   // version
            sps[3], // profile
            sps[4], // profile compat
            sps[5], // level
            0xfc | 3, // lengthSizeMinusOne, hard-coded to 4 bytes
            0xE0 | track.sps.length // 3bit reserved (111) + numOfSequenceParameterSets
          ].concat(sps).concat([
            track.pps.length // numOfPictureParameterSets
          ]).concat(pps))), // "PPS"
        width = track.width,
        height = track.height;
    //console.log('avcc:' + Hex.hexDump(avcc));
    return MP4.box(MP4.types.avc1, new Uint8Array([
        0x00, 0x00, 0x00, // reserved
        0x00, 0x00, 0x00, // reserved
        0x00, 0x01, // data_reference_index
        0x00, 0x00, // pre_defined
        0x00, 0x00, // reserved
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, // pre_defined
        (width >> 8) & 0xFF,
        width & 0xff, // width
        (height >> 8) & 0xFF,
        height & 0xff, // height
        0x00, 0x48, 0x00, 0x00, // horizresolution
        0x00, 0x48, 0x00, 0x00, // vertresolution
        0x00, 0x00, 0x00, 0x00, // reserved
        0x00, 0x01, // frame_count
        0x12,
        0x64, 0x61, 0x69, 0x6C, //dailymotion/hls.js
        0x79, 0x6D, 0x6F, 0x74,
        0x69, 0x6F, 0x6E, 0x2F,
        0x68, 0x6C, 0x73, 0x2E,
        0x6A, 0x73, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, // compressorname
        0x00, 0x18,   // depth = 24
        0x11, 0x11]), // pre_defined = -1
          avcc,
          MP4.box(MP4.types.btrt, new Uint8Array([
            0x00, 0x1c, 0x9c, 0x80, // bufferSizeDB
            0x00, 0x2d, 0xc6, 0xc0, // maxBitrate
            0x00, 0x2d, 0xc6, 0xc0])) // avgBitrate
          );
  }

  static esds(track) {
    var configlen = track.config.length;
    return new Uint8Array([
      0x00, // version 0
      0x00, 0x00, 0x00, // flags

      0x03, // descriptor_type
      0x17+configlen, // length
      0x00, 0x01, //es_id
      0x00, // stream_priority

      0x04, // descriptor_type
      0x0f+configlen, // length
      0x40, //codec : mpeg4_audio
      0x15, // stream_type
      0x00, 0x00, 0x00, // buffer_size
      0x00, 0x00, 0x00, 0x00, // maxBitrate
      0x00, 0x00, 0x00, 0x00, // avgBitrate

      0x05 // descriptor_type
      ].concat([configlen]).concat(track.config).concat([0x06, 0x01, 0x02])); // GASpecificConfig)); // length + audio config descriptor
  }

  static mp4a(track) {
    var audiosamplerate = track.audiosamplerate;
      return MP4.box(MP4.types.mp4a, new Uint8Array([
      0x00, 0x00, 0x00, // reserved
      0x00, 0x00, 0x00, // reserved
      0x00, 0x01, // data_reference_index
      0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, // reserved
      0x00, track.channelCount, // channelcount
      0x00, 0x10, // sampleSize:16bits
      0x00, 0x00, 0x00, 0x00, // reserved2
      (audiosamplerate >> 8) & 0xFF,
      audiosamplerate & 0xff, //
      0x00, 0x00]),
      MP4.box(MP4.types.esds, MP4.esds(track)));
  }

  static stsd(track) {
    if (track.type === 'audio') {
      return MP4.box(MP4.types.stsd, MP4.STSD, MP4.mp4a(track));
    } else {
      return MP4.box(MP4.types.stsd, MP4.STSD, MP4.avc1(track));
    }
  }

  static tkhd(track) {
    var id = track.id,
        duration = track.duration*track.timescale,
        width = track.width,
        height = track.height;
    return MP4.box(MP4.types.tkhd, new Uint8Array([
      0x00, // version 0
      0x00, 0x00, 0x07, // flags
      0x00, 0x00, 0x00, 0x00, // creation_time
      0x00, 0x00, 0x00, 0x00, // modification_time
      (id >> 24) & 0xFF,
      (id >> 16) & 0xFF,
      (id >> 8) & 0xFF,
      id & 0xFF, // track_ID
      0x00, 0x00, 0x00, 0x00, // reserved
      (duration >> 24),
      (duration >> 16) & 0xFF,
      (duration >>  8) & 0xFF,
      duration & 0xFF, // duration
      0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, // reserved
      0x00, 0x00, // layer
      0x00, 0x00, // alternate_group
      0x00, 0x00, // non-audio track volume
      0x00, 0x00, // reserved
      0x00, 0x01, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00,
      0x00, 0x01, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00,
      0x40, 0x00, 0x00, 0x00, // transformation: unity matrix
      (width >> 8) & 0xFF,
      width & 0xFF,
      0x00, 0x00, // width
      (height >> 8) & 0xFF,
      height & 0xFF,
      0x00, 0x00 // height
    ]));
  }

  static traf(track,baseMediaDecodeTime) {
    var sampleDependencyTable = MP4.sdtp(track),
        id = track.id;
    return MP4.box(MP4.types.traf,
               MP4.box(MP4.types.tfhd, new Uint8Array([
                 0x00, // version 0
                 0x00, 0x00, 0x00, // flags
                 (id >> 24),
                 (id >> 16) & 0XFF,
                 (id >> 8) & 0XFF,
                 (id & 0xFF) // track_ID
               ])),
               MP4.box(MP4.types.tfdt, new Uint8Array([
                 0x00, // version 0
                 0x00, 0x00, 0x00, // flags
                 (baseMediaDecodeTime >>24),
                 (baseMediaDecodeTime >> 16) & 0XFF,
                 (baseMediaDecodeTime >> 8) & 0XFF,
                 (baseMediaDecodeTime & 0xFF) // baseMediaDecodeTime
               ])),
               MP4.trun(track,
                    sampleDependencyTable.length +
                    16 + // tfhd
                    16 + // tfdt
                    8 +  // traf header
                    16 + // mfhd
                    8 +  // moof header
                    8),  // mdat header
               sampleDependencyTable);
  }

  /**
   * Generate a track box.
   * @param track {object} a track definition
   * @return {Uint8Array} the track box
   */
  static trak(track) {
    track.duration = track.duration || 0xffffffff;
    return MP4.box(MP4.types.trak, MP4.tkhd(track), MP4.mdia(track));
  }

  static trex(track) {
    var id = track.id;
    return MP4.box(MP4.types.trex, new Uint8Array([
      0x00, // version 0
      0x00, 0x00, 0x00, // flags
     (id >> 24),
     (id >> 16) & 0XFF,
     (id >> 8) & 0XFF,
     (id & 0xFF), // track_ID
      0x00, 0x00, 0x00, 0x01, // default_sample_description_index
      0x00, 0x00, 0x00, 0x00, // default_sample_duration
      0x00, 0x00, 0x00, 0x00, // default_sample_size
      0x00, 0x01, 0x00, 0x01 // default_sample_flags
    ]));
  }

  static trun(track, offset) {
    var samples= track.samples || [],
        len = samples.length,
        arraylen = 12 + (16 * len),
        array = new Uint8Array(arraylen),
        i,sample,duration,size,flags,cts;
    offset += 8 + arraylen;
    array.set([
      0x00, // version 0
      0x00, 0x0f, 0x01, // flags
      (len >>> 24) & 0xFF,
      (len >>> 16) & 0xFF,
      (len >>> 8) & 0xFF,
      len & 0xFF, // sample_count
      (offset >>> 24) & 0xFF,
      (offset >>> 16) & 0xFF,
      (offset >>> 8) & 0xFF,
      offset & 0xFF // data_offset
    ],0);
    for (i = 0; i < len; i++) {
      sample = samples[i];
      duration = sample.duration;
      size = sample.size;
      flags = sample.flags;
      cts = sample.cts;
      array.set([
        (duration >>> 24) & 0xFF,
        (duration >>> 16) & 0xFF,
        (duration >>> 8) & 0xFF,
        duration & 0xFF, // sample_duration
        (size >>> 24) & 0xFF,
        (size >>> 16) & 0xFF,
        (size >>> 8) & 0xFF,
        size & 0xFF, // sample_size
        (flags.isLeading << 2) | flags.dependsOn,
        (flags.isDependedOn << 6) |
          (flags.hasRedundancy << 4) |
          (flags.paddingValue << 1) |
          flags.isNonSync,
        flags.degradPrio & 0xF0 << 8,
        flags.degradPrio & 0x0F, // sample_flags
        (cts >>> 24) & 0xFF,
        (cts >>> 16) & 0xFF,
        (cts >>> 8) & 0xFF,
        cts & 0xFF // sample_composition_time_offset
      ],12+16*i);
    }
    return MP4.box(MP4.types.trun, array);
  }

  static initSegment(tracks) {
    if (!MP4.types) {
      MP4.init();
    }
    var movie = MP4.moov(tracks), result;
    result = new Uint8Array(MP4.FTYP.byteLength + movie.byteLength);
    result.set(MP4.FTYP);
    result.set(movie, MP4.FTYP.byteLength);
    return result;
  }
}

export default MP4;
