/**
 * Playlist Loader
*/

import Event from '../events';
import {ErrorTypes, ErrorDetails} from '../errors';
import URLHelper from '../utils/url';
//import {logger} from '../utils/logger';

class PlaylistLoader {

  constructor(hls) {
    this.hls = hls;
    this.onml = this.onManifestLoading.bind(this);
    this.onll = this.onLevelLoading.bind(this);
    hls.on(Event.MANIFEST_LOADING, this.onml);
    hls.on(Event.LEVEL_LOADING, this.onll);
  }

  destroy() {
    if (this.loader) {
      this.loader.destroy();
      this.loader = null;
    }
    this.url = this.id = null;
    this.hls.off(Event.MANIFEST_LOADING, this.onml);
    this.hls.off(Event.LEVEL_LOADING, this.onll);
  }

  onManifestLoading(event, data) {
    this.load(data.url, null);
  }

  onLevelLoading(event, data) {
    this.load(data.url, data.level, data.id);
  }

  load(url, id1, id2) {
    var config = this.hls.config;
    this.url = url;
    this.id = id1;
    this.id2 = id2;
    this.loader = typeof(config.pLoader) !== 'undefined' ? new config.pLoader(config) : new config.loader(config);
    this.loader.load(url, '', this.loadsuccess.bind(this), this.loaderror.bind(this), this.loadtimeout.bind(this), config.manifestLoadingTimeOut, config.manifestLoadingMaxRetry, config.manifestLoadingRetryDelay);
  }

  resolve(url, baseUrl) {
    return URLHelper.buildAbsoluteURL(baseUrl, url);
  }

  parseMasterPlaylist(string, baseurl) {
    var levels = [], level =  {}, result, codecs, codec;
    // https://regex101.com is your friend
    var re = /#EXT-X-STREAM-INF:([^\n\r]*(BAND)WIDTH=(\d+))?([^\n\r]*(CODECS)=\"([^\"\n\r]*)\",?)?([^\n\r]*(RES)OLUTION=(\d+)x(\d+))?([^\n\r]*(NAME)=\"(.*)\")?[^\n\r]*[\r\n]+([^\r\n]+)/g;
    while ((result = re.exec(string)) != null){
      result.shift();
      result = result.filter(function(n) { return (n !== undefined); });
      level.url = this.resolve(result.pop(), baseurl);
      while (result.length > 0) {
        switch (result.shift()) {
          case 'RES':
            level.width = parseInt(result.shift());
            level.height = parseInt(result.shift());
            break;
          case 'BAND':
            level.bitrate = parseInt(result.shift());
            break;
          case 'NAME':
            level.name = result.shift();
            break;
          case 'CODECS':
            codecs = result.shift().split(',');
            while (codecs.length > 0) {
              codec = codecs.shift();
              if (codec.indexOf('avc1') !== -1) {
                level.videoCodec = this.avc1toavcoti(codec);
              } else {
                level.audioCodec = codec;
              }
            }
            break;
          default:
            break;
        }
      }
      levels.push(level);
      level = {};
    }
    return levels;
  }

  avc1toavcoti(codec) {
    var result, avcdata = codec.split('.');
    if (avcdata.length > 2) {
      result = avcdata.shift() + '.';
      result += parseInt(avcdata.shift()).toString(16);
      result += ('00' + parseInt(avcdata.shift()).toString(16)).substr(-4);
    } else {
      result = codec;
    }
    return result;
  }

  parseKeyParamsByRegex(string, regexp) {
    var result = regexp.exec(string);
    if (result) {
      result.shift();
      result = result.filter(function(n) { return (n !== undefined); });
      if (result.length === 2) {
        return result[1];
      }
    }
    return null;
  }

  cloneObj(obj) {
    return JSON.parse(JSON.stringify(obj));
  }

  parseLevelPlaylist(string, baseurl, id) {
    var currentSN = 0, totalduration = 0, level = {url: baseurl, fragments: [], live: true, startSN: 0}, result, regexp, cc = 0, frag, byteRangeEndOffset, byteRangeStartOffset;
    var levelkey = {method : null, key : null, iv : null, uri : null};
    regexp = /(?:#EXT-X-(MEDIA-SEQUENCE):(\d+))|(?:#EXT-X-(TARGETDURATION):(\d+))|(?:#EXT-X-(KEY):(.*))|(?:#EXT(INF):([\d\.]+)[^\r\n]*([\r\n]+[^#|\r\n]+)?)|(?:#EXT-X-(BYTERANGE):([\d]+[@[\d]*)]*[\r\n]+([^#|\r\n]+)?|(?:#EXT-X-(ENDLIST))|(?:#EXT-X-(DIS)CONTINUITY))/g;
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
          frag = level.fragments.length ? level.fragments[level.fragments.length - 1] : null;
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
            level.fragments.push({url: result[2] ? this.resolve(result[2], baseurl) : null, duration: duration, start: totalduration, sn: sn, level: id, cc: cc, byteRangeStartOffset: byteRangeStartOffset, byteRangeEndOffset: byteRangeEndOffset, decryptdata : fragdecryptdata});
            totalduration += duration;
            byteRangeStartOffset = null;
          }
          break;
        case 'KEY':
          // https://tools.ietf.org/html/draft-pantos-http-live-streaming-08#section-3.4.4
          var decryptparams = result[1];
          var decryptmethod = this.parseKeyParamsByRegex(decryptparams, /(METHOD)=([^,]*)/),
              decrypturi = this.parseKeyParamsByRegex(decryptparams, /(URI)=["]([^,]*)["]/),
              decryptiv = this.parseKeyParamsByRegex(decryptparams, /(IV)=([^,]*)/);
          if (decryptmethod) {
            levelkey = { method: null, key: null, iv: null, uri: null };
            if ((decrypturi) && (decryptmethod === 'AES-128')) {
              levelkey.method = decryptmethod;
              // URI to get the key
              levelkey.uri = this.resolve(decrypturi, baseurl);
              levelkey.key = null;
              // Initialization Vector (IV)
              if (decryptiv) {
                levelkey.iv = decryptiv;
                if (levelkey.iv.substring(0, 2) === '0x') {
                  levelkey.iv = levelkey.iv.substring(2);
                }
                levelkey.iv = levelkey.iv.match(/.{8}/g);
                levelkey.iv[0] = parseInt(levelkey.iv[0], 16);
                levelkey.iv[1] = parseInt(levelkey.iv[1], 16);
                levelkey.iv[2] = parseInt(levelkey.iv[2], 16);
                levelkey.iv[3] = parseInt(levelkey.iv[3], 16);
                levelkey.iv = new Uint32Array(levelkey.iv);
              }
            }
          }
          break;
        default:
          break;
      }
    }
    //logger.log('found ' + level.fragments.length + ' fragments');
    level.totalduration = totalduration;
    level.endSN = currentSN - 1;
    return level;
  }

  loadsuccess(event, stats) {
    var string = event.currentTarget.responseText, url = event.currentTarget.responseURL, id = this.id, id2 = this.id2, hls = this.hls, levels;
    // responseURL not supported on some browsers (it is used to detect URL redirection)
    if (url === undefined) {
      // fallback to initial URL
      url = this.url;
    }
    stats.tload = performance.now();
    stats.mtime = new Date(event.currentTarget.getResponseHeader('Last-Modified'));
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
    this.loader.abort();
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
   this.loader.abort();
   this.hls.trigger(Event.ERROR, {type: ErrorTypes.NETWORK_ERROR, details: details, fatal: fatal, url: this.url, loader: this.loader, level: this.id, id: this.id2});
  }
}

export default PlaylistLoader;
