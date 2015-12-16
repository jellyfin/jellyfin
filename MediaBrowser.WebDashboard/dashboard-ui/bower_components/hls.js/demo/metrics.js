  function showMetrics()  {
    if(metricsDisplayed) {
      var width = window.innerWidth-30;
      $("#bufferWindow_c")[0].width =
      $("#bitrateTimerange_c")[0].width =
      $("#bufferTimerange_c")[0].width =
      $("#videoEvent_c")[0].width =
      $("#metricsButton")[0].width =
      $("#loadEvent_c")[0].width = width;
      $("#bufferWindow_c").show();
      $("#bitrateTimerange_c").show();
      $("#bufferTimerange_c").show();
      $("#videoEvent_c").show();
      $("#metricsButton").show();
      $("#loadEvent_c").show();
    }
  }


  function toggleMetricsDisplay() {
    metricsDisplayed = !metricsDisplayed;
    if(metricsDisplayed) {
      showMetrics();
    } else {
      hideMetrics();
    }
  }

  function hideMetrics()  {
      if(!metricsDisplayed) {
        $("#bufferWindow_c").hide();
        $("#bitrateTimerange_c").hide();
        $("#bufferTimerange_c").hide();
        $("#videoEvent_c").hide();
        $("#metricsButton").hide();
        $("#loadEvent_c").hide();
      }
  }

  function timeRangeSetSliding(duration) {
    windowDuration = duration;
    windowSliding = true;
    refreshCanvas();
  }

var timeRangeMouseDown=false;
 function timeRangeCanvasonMouseDown(evt) {
    var canvas = evt.currentTarget,
        bRect = canvas.getBoundingClientRect(),
        mouseX = Math.round((evt.clientX - bRect.left)*(canvas.width/bRect.width));
    windowStart = Math.max(0,Math.round((mouseX-eventLeftMargin) * getWindowTimeRange().now / (canvas.width-eventLeftMargin)));
    windowEnd = windowStart+500;
    timeRangeMouseDown = true;
    windowSliding = false;
    //console.log('windowStart/windowEnd:' + '/' + windowStart + '/' + windowEnd);
    $("#windowStart").val(windowStart);
    $("#windowEnd").val(windowEnd);
    refreshCanvas();
 }

 function timeRangeCanvasonMouseMove(evt) {
    if(timeRangeMouseDown) {
      var canvas = evt.currentTarget,
          bRect = canvas.getBoundingClientRect(),
          mouseX = Math.round((evt.clientX - bRect.left)*(canvas.width/bRect.width)),
          pos = Math.max(0,Math.round((mouseX-eventLeftMargin) * getWindowTimeRange().now / (canvas.width-eventLeftMargin)));
      if(pos < windowStart) {
        windowStart = pos;
      } else {
        windowEnd = pos;
      }
      if(windowStart === windowEnd) {
        // to avoid division by zero ...
        windowEnd +=50;
      }
      //console.log('windowStart/windowEnd:' + '/' + windowStart + '/' + windowEnd);
    $("#windowStart").val(windowStart);
    $("#windowEnd").val(windowEnd);
      refreshCanvas();
    }
 }

 function timeRangeCanvasonMouseUp(evt) {
  timeRangeMouseDown = false;
 }

 function timeRangeCanvasonMouseOut(evt) {
  timeRangeMouseDown = false;
 }

 function windowCanvasonMouseMove(evt) {
    var canvas = evt.currentTarget,
        bRect = canvas.getBoundingClientRect(),
        mouseX = Math.round((evt.clientX - bRect.left)*(canvas.width/bRect.width)),
        timeRange = getWindowTimeRange();
    windowFocus = timeRange.min + Math.max(0,Math.round((mouseX-eventLeftMargin) * (timeRange.max - timeRange.min)  / (canvas.width-eventLeftMargin)));
    //console.log(windowFocus);
    refreshCanvas();
 }

var windowDuration=20000,windowSliding=true,windowStart=0,windowEnd=10000,windowFocus,metricsDisplayed=false;
$("#windowStart").val(windowStart);
$("#windowEnd").val(windowEnd);
  function refreshCanvas()  {
    if(metricsDisplayed) {
      try {
        var windowTime = getWindowTimeRange();
        canvasBufferTimeRangeUpdate($("#bufferTimerange_c")[0], 0, windowTime.now, windowTime.min,windowTime.max, events.buffer);
        if(windowTime.min !== 0 || windowTime.max !== windowTime.now) {
          $("#bufferWindow_c").show();
          canvasBufferWindowUpdate($("#bufferWindow_c")[0], windowTime.min,windowTime.max, windowTime.focus, events.buffer);
        } else {
          $("#bufferWindow_c").hide();
        }
        canvasBitrateEventUpdate($("#bitrateTimerange_c")[0], 0, windowTime.now, windowTime.min,windowTime.max, events.level, events.bitrate);
        canvasVideoEventUpdate($("#videoEvent_c")[0], windowTime.min,windowTime.max, events.video);
        canvasLoadEventUpdate($("#loadEvent_c")[0], windowTime.min,windowTime.max, events.load);
      } catch(err) {
        console.log("refreshCanvas error:" +err.message);
      }
    }
  }

  function getWindowTimeRange() {
      var tnow,minTime,maxTime;
      if(events.buffer.length) {
        tnow = events.buffer[events.buffer.length-1].time;
      } else {
        tnow = 0;
      }
      if(windowSliding) {
        // let's show the requested window
        if(windowDuration) {
          minTime = Math.max(0, tnow-windowDuration),
          maxTime = Math.min(minTime + windowDuration, tnow);
        } else {
          minTime = 0;
          maxTime = tnow;
        }
      } else {
        minTime = windowStart;
        maxTime = windowEnd;
      }
      if(windowFocus === undefined || windowFocus < minTime || windowFocus > maxTime) {
        windowFocus = minTime;
      }
      return { min : minTime, max: maxTime, now : tnow, focus : windowFocus}
  }

function timeRangeZoomIn() {
  if(windowSliding) {
    windowDuration/=2;
  } else {
    var duration = windowEnd-windowStart;
    windowStart+=duration/4;
    windowEnd-=duration/4;
    if(windowStart === windowEnd) {
      windowEnd+=50;
    }
  }
  $("#windowStart").val(windowStart);
  $("#windowEnd").val(windowEnd);
  refreshCanvas();
}

function timeRangeZoomOut() {
  if(windowSliding) {
    windowDuration*=2;
  }  else {
    var duration = windowEnd-windowStart;
    windowStart-=duration/2;
    windowEnd+=duration/2;
    windowStart=Math.max(0,windowStart);
    windowEnd=Math.min(events.buffer[events.buffer.length-1].time,windowEnd);
  }
  $("#windowStart").val(windowStart);
  $("#windowEnd").val(windowEnd);
  refreshCanvas();
}

function timeRangeSlideLeft() {
  var duration = windowEnd-windowStart;
  windowStart-=duration/4;
  windowEnd-=duration/4;
  windowStart=Math.max(0,windowStart);
  windowEnd=Math.min(events.buffer[events.buffer.length-1].time,windowEnd);
  $("#windowStart").val(windowStart);
  $("#windowEnd").val(windowEnd);
  refreshCanvas();
}

function timeRangeSlideRight() {
  var duration = windowEnd-windowStart;
  windowStart+=duration/4;
  windowEnd+=duration/4;
  windowStart=Math.max(0,windowStart);
  windowEnd=Math.min(events.buffer[events.buffer.length-1].time,windowEnd);
  $("#windowStart").val(windowStart);
  $("#windowEnd").val(windowEnd);
  refreshCanvas();
}
