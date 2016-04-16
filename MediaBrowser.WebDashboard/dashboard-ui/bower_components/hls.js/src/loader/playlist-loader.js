/**
 * Playlist Loader
*/

import Event from '../events';
import EventHandler from '../event-handler';
import {ErrorTypes, ErrorDetails} from '../errors';
import URLHelper from '../utils/url';
import AttrList from '../utils/attr-list';
//import {logger} from '../utils/logger';

class PlaylistLoader extends EventHandler {

  constructor(hls) {
    super(hls,
      Event.MANIFEST_LOADING,
      Event.LEVEL_LOADING);
  }

  destroy() {
    if (this.loader) {
      this.loader.destroy();
      this.loader = null;
    }
    this.url = this.id = null;
    EventHandler.prototype.destroy.call(this);
  }

  onManifestLoading(data) {
    this.load(data.url, null);
  }

  onLevelLoading(data) {
    this.load(data.url, data.level, data.id);
  }

  load(url, id1, id2) {
    var config = this.hls.config,
        retry,
        timeout,
        retryDelay;
    this.url = url;
    this.id = id1;
    this.id2 = id2;
    if(this.id === null) {
      retry = config.manifestLoadingMaxRetry;
      timeout = config.manifestLoadingTimeOut;
      retryDelay = config.manifestLoadingRetryDelay;
    } else {
      retry = config.levelLoadingMaxRetry;
      timeout = config.levelLoadingTimeOut;
      retryDelay = config.levelLoadingRetryDelay;
    }
    this.loader = typeof(config.pLoader) !== 'undefined' ? new config.pLoader(config) : new config.loader(config);
    this.loader.load(url, '', this.loadsuccess.bind(this), this.loaderror.bind(this), this.loadtimeout.bind(this), timeout, retry, retryDelay);
  }

  resolve(url, baseUrl) {
    return URLHelper.buildAbsoluteURL(baseUrl, url);
  }

  parseMasterPlaylist(string, baseurl) {
    let levels = [], result;

    // https://regex101.com is your friend
    const re = /#EXT-X-STREAM-INF:([^\n\r]*)[\r\n]+([^\r\n]+)/g;
    while ((result = re.exec(string)) != null){
      const level = {};

      var attrs = level.attrs = new AttrList(result[1]);
      level.url = this.resolve(result[2], baseurl);

      var resolution = attrs.decimalResolution('RESOLUTION');
      if(resolution) {
        level.width = resolution.width;
        level.height = resolution.height;
      }
      level.bitrate = attrs.decimalInteger('AVERAGE-BANDWIDTH') || attrs.decimalInteger('BANDWIDTH');
      level.name = attrs.NAME;

      var codecs = attrs.CODECS;
      if(codecs) {
        codecs = codecs.split(',');
        for (let i = 0; i < codecs.length; i++) {
          const codec = codecs[i];
          if (codec.indexOf('avc1') !== -1) {
            level.videoCodec = this.avc1toavcoti(codec);
          } else {
            level.audioCodec = codec;
          }
        }
      }

      levels.push(level);
    }
    return levels;
  }

  avc1toavcoti(codec) {
    var result, avcdata = codec.split('.');
    if (avcdata.length > 2) {
      result = avcdata.shift() + '.';
      result += parseInt(avcdata.shift()).toString(16);
      result += ('000' + parseInt(avcdata.shift()).toString(16)).substr(-4);
    } else {
      result = codec;
    }
    return result;
  }

  cloneObj(obj) {
    return JSON.parse(JSON.stringify(obj));
  }

  parseLevelPlaylist(string, baseurl, id) {
    var currentSN = 0,
        totalduration = 0,
        level = {url: baseurl, fragments: [], live: true, startSN: 0},
        levelkey = {method : null, key : null, iv : null, uri : null},
        cc = 0,
        programDateTime = null,
        frag = null,
        result,
        regexp,
        byteRangeEndOffset,
        byteRangeStartOffset;

    regexp = /(?:#EXT-X-(MEDIA-SEQUENCE):(\d+))|(?:#EXT-X-(TARGETDURATION):(\d+))|(?:#EXT-X-(KEY):(.*))|(?:#EXT(INF):([\d\.]+)[^\r\n]*([\r\n]+[^#|\r\n]+)?)|(?:#EXT-X-(BYTERANGE):([\d]+[@[\d]*)]*[\r\n]+([^#|\r\n]+)?|(?:#EXT-X-(ENDLIST))|(?:#EXT-X-(DIS)CONTINUITY))|(?:#EXT-X-(PROGRAM-DATE-TIME):(.*))/g;
    while ((result = regexp.exec(string)) !== null) {
      result.shift();
      result = result.filter(function(n) { return (n !== undefined); });
      switch (result[0]) {
        case 'MEDIA-SEQUENCE':
          currentSN = level.startSN = parseInt(result[1]);
          break;
        case 'TARGETDURATION':
          level.targetduration = parseFloat(result[1]);
          break;
        case 'ENDLIST':
          level.live = false;
          break;
        case 'DIS':
          cc++;
          break;
        case 'BYTERANGE':
          var params = result[1].split('@');
          if (params.length === 1) {
            byteRangeStartOffset = byteRangeEndOffset;
          } else {
            byteRangeStartOffset = parseInt(params[1]);
          }
          byteRangeEndOffset = parseInt(params[0]) + byteRangeStartOffset;
          if (frag && !frag.url) {
            frag.byteRangeStartOffset = byteRangeStartOffset;
            frag.byteRangeEndOffset = byteRangeEndOffset;
            frag.url = this.resolve(result[2], baseurl);
          }
          break;
        case 'INF':
          var duration = parseFloat(result[1]);
          if (!isNaN(duration)) {
            var fragdecryptdata,
                sn = currentSN++;
            if (levelkey.method && levelkey.uri && !levelkey.iv) {
              fragdecryptdata = this.cloneObj(levelkey);
              var uint8View = new Uint8Array(16);
              for (var i = 12; i < 16; i++) {
                uint8View[i] = (sn >> 8*(15-i)) & 0xff;
              }
              fragdecryptdata.iv = uint8View;
            } else {
              fragdecryptdata = levelkey;
            }
            var url = result[2] ? this.resolve(result[2], baseurl) : null;
            frag = {url: url, duration: duration, start: totalduration, sn: sn, level: id, cc: cc, byteRangeStartOffset: byteRangeStartOffset, byteRangeEndOffset: byteRangeEndOffset, decryptdata : fragdecryptdata, programDateTime: programDateTime};
            level.fragments.push(frag);
            totalduration += duration;
            byteRangeStartOffset = null;
            programDateTime = null;
          }
          break;
        case 'KEY':
          // https://tools.ietf.org/html/draft-pantos-http-live-streaming-08#section-3.4.4
          var decryptparams = result[1];
          var keyAttrs = new AttrList(decryptparams);
          var decryptmethod = keyAttrs.enumeratedString('METHOD'),
              decrypturi = keyAttrs.URI,
              decryptiv = keyAttrs.hexadecimalInteger('IV');
          if (decryptmethod) {
            levelkey = { method: null, key: null, iv: null, uri: null };
            if ((decrypturi) && (decryptmethod === 'AES-128')) {
              levelkey.method = decryptmethod;
              // URI to get the key
              levelkey.uri = this.resolve(decrypturi, baseurl);
              levelkey.key = null;
              // Initialization Vector (IV)
              levelkey.iv = decryptiv;
            }
          }
          break;
        case 'PROGRAM-DATE-TIME':
          programDateTime = new Date(Date.parse(result[1]));
          break;
        default:
          break;
      }
    }
    //logger.log('found ' + level.fragments.length + ' fragments');
    if(frag && !frag.url) {
      level.fragments.pop();
      totalduration-=frag.duration;
    }
    level.totalduration = totalduration;
    level.averagetargetduration = totalduration / level.fragments.length;
    level.endSN = currentSN - 1;
    return level;
  }

  loadsuccess(event, stats) {
    var target = event.currentTarget,
        string = target.responseText,
        url = target.responseURL,
        id = this.id,
        id2 = this.id2,
        hls = this.hls,
        levels;
    // responseURL not supported on some browsers (it is used to detect URL redirection)
    // data-uri mode also not supported (but no need to detect redirection)
    if (url === undefined || url.indexOf('data:') === 0) {
      // fallback to initial URL
      url = this.url;
    }
    stats.tload = performance.now();
    stats.mtime = new Date(target.getResponseHeader('Last-Modified'));
    if (string.indexOf('#EXTM3U') === 0) {
      if (string.indexOf('#EXTINF:') > 0) {
        // 1 level playlist
        // if first request, fire manifest loaded event, level will be reloaded afterwards
        // (this is to have a uniform logic for 1 level/multilevel playlists)
        if (this.id === null) {
          hls.trigger(Event.MANIFEST_LOADED, {levels: [{url: url}], url: url, stats: stats});
        } else {
          var levelDetails = this.parseLevelPlaylist(string, url, id);
          stats.tparsed = performance.now();
          hls.trigger(Event.LEVEL_LOADED, {details: levelDetails, level: id, id: id2, stats: stats});
        }
      } else {
        levels = this.parseMasterPlaylist(string, url);
        // multi level playlist, parse level info
        if (levels.length) {
          hls.trigger(Event.MANIFEST_LOADED, {levels: levels, url: url, stats: stats});
        } else {
          hls.trigger(Event.ERROR, {type: ErrorTypes.NETWORK_ERROR, details: ErrorDetails.MANIFEST_PARSING_ERROR, fatal: true, url: url, reason: 'no level found in manifest'});
        }
      }
    } else {
      hls.trigger(Event.ERROR, {type: ErrorTypes.NETWORK_ERROR, details: ErrorDetails.MANIFEST_PARSING_ERROR, fatal: true, url: url, reason: 'no EXTM3U delimiter'});
    }
  }

  loaderror(event) {
    var details, fatal;
    if (this.id === null) {
      details = ErrorDetails.MANIFEST_LOAD_ERROR;
      fatal = true;
    } else {
      details = ErrorDetails.LEVEL_LOAD_ERROR;
      fatal = false;
    }
    if (this.loader) {
      this.loader.abort();
    }
    this.hls.trigger(Event.ERROR, {type: ErrorTypes.NETWORK_ERROR, details: details, fatal: fatal, url: this.url, loader: this.loader, response: event.currentTarget, level: this.id, id: this.id2});
  }

  loadtimeout() {
    var details, fatal;
    if (this.id === null) {
      details = ErrorDetails.MANIFEST_LOAD_TIMEOUT;
      fatal = true;
    } else {
      details = ErrorDetails.LEVEL_LOAD_TIMEOUT;
      fatal = false;
    }
    if (this.loader) {
      this.loader.abort();
    }
    this.hls.trigger(Event.ERROR, {type: ErrorTypes.NETWORK_ERROR, details: details, fatal: fatal, url: this.url, loader: this.loader, level: this.id, id: this.id2});
  }
}

export default PlaylistLoader;
