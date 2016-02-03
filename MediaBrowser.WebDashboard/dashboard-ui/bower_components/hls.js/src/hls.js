/**
 * HLS interface
 */
'use strict';

import Event from './events';
import {ErrorTypes, ErrorDetails} from './errors';
import PlaylistLoader from './loader/playlist-loader';
import FragmentLoader from './loader/fragment-loader';
import AbrController from    './controller/abr-controller';
import MSEMediaController from './controller/mse-media-controller';
import LevelController from  './controller/level-controller';
import TimelineController from './controller/timeline-controller';
//import FPSController from './controller/fps-controller';
import {logger, enableLogs} from './utils/logger';
import XhrLoader from './utils/xhr-loader';
import EventEmitter from 'events';
import KeyLoader from './loader/key-loader';

class Hls {

  static isSupported() {
    return (window.MediaSource && window.MediaSource.isTypeSupported('video/mp4; codecs="avc1.42E01E,mp4a.40.2"'));
  }

  static get Events() {
    return Event;
  }

  static get ErrorTypes() {
    return ErrorTypes;
  }

  static get ErrorDetails() {
    return ErrorDetails;
  }

  static get DefaultConfig() {
    if(!Hls.defaultConfig) {
       Hls.defaultConfig = {
          autoStartLoad: true,
          debug: false,
          maxBufferLength: 30,
          maxBufferSize: 60 * 1000 * 1000,
          maxBufferHole: 0.5,
          maxSeekHole: 2,
          liveSyncDurationCount:3,
          liveMaxLatencyDurationCount: Infinity,
          maxMaxBufferLength: 600,
          enableWorker: true,
          enableSoftwareAES: true,
          manifestLoadingTimeOut: 10000,
          manifestLoadingMaxRetry: 1,
          manifestLoadingRetryDelay: 1000,
          levelLoadingTimeOut: 10000,
          levelLoadingMaxRetry: 4,
          levelLoadingRetryDelay: 1000,
          fragLoadingTimeOut: 20000,
          fragLoadingMaxRetry: 6,
          fragLoadingRetryDelay: 1000,
          fragLoadingLoopThreshold: 3,
          // fpsDroppedMonitoringPeriod: 5000,
          // fpsDroppedMonitoringThreshold: 0.2,
          appendErrorMaxRetry: 3,
          loader: XhrLoader,
          fLoader: undefined,
          pLoader: undefined,
          abrController : AbrController,
          mediaController: MSEMediaController,
          timelineController: TimelineController,
          enableCEA708Captions: true
        };
    }
    return Hls.defaultConfig;
  }

  static set DefaultConfig(defaultConfig) {
    Hls.defaultConfig = defaultConfig;
  }

  constructor(config = {}) {
    var defaultConfig = Hls.DefaultConfig;
    for (var prop in defaultConfig) {
        if (prop in config) { continue; }
        config[prop] = defaultConfig[prop];
    }

    if (config.liveMaxLatencyDurationCount !== undefined && config.liveMaxLatencyDurationCount <= config.liveSyncDurationCount) {
      throw new Error('Illegal hls.js config: "liveMaxLatencyDurationCount" must be gt "liveSyncDurationCount"');
    }

    enableLogs(config.debug);
    this.config = config;
    // observer setup
    var observer = this.observer = new EventEmitter();
    observer.trigger = function trigger (event, ...data) {
      observer.emit(event, event, ...data);
    };

    observer.off = function off (event, ...data) {
      observer.removeListener(event, ...data);
    };
    this.on = observer.on.bind(observer);
    this.off = observer.off.bind(observer);
    this.trigger = observer.trigger.bind(observer);
    this.playlistLoader = new PlaylistLoader(this);
    this.fragmentLoader = new FragmentLoader(this);
    this.levelController = new LevelController(this);
    this.abrController = new config.abrController(this);
    this.mediaController = new config.mediaController(this);
    this.timelineController = new config.timelineController(this);
    this.keyLoader = new KeyLoader(this);
    //this.fpsController = new FPSController(this);
  }

  destroy() {
    logger.log('destroy');
    this.trigger(Event.DESTROYING);
    this.detachMedia();
    this.playlistLoader.destroy();
    this.fragmentLoader.destroy();
    this.levelController.destroy();
    this.mediaController.destroy();
    this.timelineController.destroy();
    this.keyLoader.destroy();
    //this.fpsController.destroy();
    this.url = null;
    this.observer.removeAllListeners();
  }

  attachMedia(media) {
    logger.log('attachMedia');
    this.media = media;
    this.trigger(Event.MEDIA_ATTACHING, {media: media});
  }

  detachMedia() {
    logger.log('detachMedia');
    this.trigger(Event.MEDIA_DETACHING);
    this.media = null;
  }

  loadSource(url) {
    logger.log(`loadSource:${url}`);
    this.url = url;
    // when attaching to a source URL, trigger a playlist load
    this.trigger(Event.MANIFEST_LOADING, {url: url});
  }

  startLoad() {
    logger.log('startLoad');
    this.mediaController.startLoad();
  }

  swapAudioCodec() {
    logger.log('swapAudioCodec');
    this.mediaController.swapAudioCodec();
  }

  recoverMediaError() {
    logger.log('recoverMediaError');
    var media = this.media;
    this.detachMedia();
    this.attachMedia(media);
  }

  /** Return all quality levels **/
  get levels() {
    return this.levelController.levels;
  }

  /** Return current playback quality level **/
  get currentLevel() {
    return this.mediaController.currentLevel;
  }

  /* set quality level immediately (-1 for automatic level selection) */
  set currentLevel(newLevel) {
    logger.log(`set currentLevel:${newLevel}`);
    this.loadLevel = newLevel;
    this.mediaController.immediateLevelSwitch();
  }

  /** Return next playback quality level (quality level of next fragment) **/
  get nextLevel() {
    return this.mediaController.nextLevel;
  }

  /* set quality level for next fragment (-1 for automatic level selection) */
  set nextLevel(newLevel) {
    logger.log(`set nextLevel:${newLevel}`);
    this.levelController.manualLevel = newLevel;
    this.mediaController.nextLevelSwitch();
  }

  /** Return the quality level of current/last loaded fragment **/
  get loadLevel() {
    return this.levelController.level;
  }

  /* set quality level for current/next loaded fragment (-1 for automatic level selection) */
  set loadLevel(newLevel) {
    logger.log(`set loadLevel:${newLevel}`);
    this.levelController.manualLevel = newLevel;
  }

  /** Return the quality level of next loaded fragment **/
  get nextLoadLevel() {
    return this.levelController.nextLoadLevel();
  }

  /** set quality level of next loaded fragment **/
  set nextLoadLevel(level) {
    this.levelController.level = level;
  }

  /** Return first level (index of first level referenced in manifest)
  **/
  get firstLevel() {
    return this.levelController.firstLevel;
  }

  /** set first level (index of first level referenced in manifest)
  **/
  set firstLevel(newLevel) {
    logger.log(`set firstLevel:${newLevel}`);
    this.levelController.firstLevel = newLevel;
  }

  /** Return start level (level of first fragment that will be played back)
      if not overrided by user, first level appearing in manifest will be used as start level
      if -1 : automatic start level selection, playback will start from level matching download bandwidth (determined from download of first segment)
  **/
  get startLevel() {
    return this.levelController.startLevel;
  }

  /** set  start level (level of first fragment that will be played back)
      if not overrided by user, first level appearing in manifest will be used as start level
      if -1 : automatic start level selection, playback will start from level matching download bandwidth (determined from download of first segment)
  **/
  set startLevel(newLevel) {
    logger.log(`set startLevel:${newLevel}`);
    this.levelController.startLevel = newLevel;
  }

  /** Return the capping/max level value that could be used by automatic level selection algorithm **/
  get autoLevelCapping() {
    return this.abrController.autoLevelCapping;
  }

  /** set the capping/max level value that could be used by automatic level selection algorithm **/
  set autoLevelCapping(newLevel) {
    logger.log(`set autoLevelCapping:${newLevel}`);
    this.abrController.autoLevelCapping = newLevel;
  }

  /* check if we are in automatic level selection mode */
  get autoLevelEnabled() {
    return (this.levelController.manualLevel === -1);
  }

  /* return manual level */
  get manualLevel() {
    return this.levelController.manualLevel;
  }
}

export default Hls;
