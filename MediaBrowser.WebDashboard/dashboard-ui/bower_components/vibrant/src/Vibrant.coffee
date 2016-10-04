###
  Vibrant.js
  by Jari Zwarts

  Color algorithm class that finds variations on colors in an image.

  Credits
  --------
  Lokesh Dhakar (http://www.lokeshdhakar.com) - Created ColorThief
  Google - Palette support library in Android
###

window.Swatch = class Swatch
  hsl: undefined
  rgb: undefined
  population: 1
  @yiq: 0

  constructor: (rgb, population) ->
    @rgb = rgb
    @population = population

  getHsl: ->
    if not @hsl
      @hsl = Vibrant.rgbToHsl @rgb[0], @rgb[1], @rgb[2]
    else @hsl

  getPopulation: ->
    @population

  getRgb: ->
    @rgb

  getHex: ->
    "#" + ((1 << 24) + (@rgb[0] << 16) + (@rgb[1] << 8) + @rgb[2]).toString(16).slice(1, 7);

  getTitleTextColor: ->
    @_ensureTextColors()
    if @yiq < 200 then "#fff" else "#000"

  getBodyTextColor: ->
    @_ensureTextColors()
    if @yiq < 150 then "#fff" else "#000"

  _ensureTextColors: ->
    if not @yiq then @yiq = (@rgb[0] * 299 + @rgb[1] * 587 + @rgb[2] * 114) / 1000

window.Vibrant = class Vibrant

  quantize: require('quantize')

  _swatches: []

  TARGET_DARK_LUMA: 0.26
  MAX_DARK_LUMA: 0.45
  MIN_LIGHT_LUMA: 0.55
  TARGET_LIGHT_LUMA: 0.74

  MIN_NORMAL_LUMA: 0.3
  TARGET_NORMAL_LUMA: 0.5
  MAX_NORMAL_LUMA: 0.7

  TARGET_MUTED_SATURATION: 0.3
  MAX_MUTED_SATURATION: 0.4

  TARGET_VIBRANT_SATURATION: 1
  MIN_VIBRANT_SATURATION: 0.35

  WEIGHT_SATURATION: 3
  WEIGHT_LUMA: 6
  WEIGHT_POPULATION: 1

  VibrantSwatch: undefined
  MutedSwatch: undefined
  DarkVibrantSwatch: undefined
  DarkMutedSwatch: undefined
  LightVibrantSwatch: undefined
  LightMutedSwatch: undefined

  HighestPopulation: 0

  constructor: (sourceImage, colorCount, quality) ->
    if typeof colorCount == 'undefined'
      colorCount = 64
    if typeof quality == 'undefined'
      quality = 5

    image = new CanvasImage(sourceImage)
    try
      imageData = image.getImageData()
      pixels = imageData.data
      pixelCount = image.getPixelCount()

      allPixels = []
      i = 0
      while i < pixelCount
        offset = i * 4
        r = pixels[offset + 0]
        g = pixels[offset + 1]
        b = pixels[offset + 2]
        a = pixels[offset + 3]
        # If pixel is mostly opaque and not white
        if a >= 125
          if not (r > 250 and g > 250 and b > 250)
            allPixels.push [r, g, b]
        i = i + quality

      cmap = @quantize allPixels, colorCount
      @_swatches = cmap.vboxes.map (vbox) =>
        new Swatch vbox.color, vbox.vbox.count()

      @maxPopulation = @findMaxPopulation

      @generateVarationColors()
      @generateEmptySwatches()

    # Clean up
    finally
      image.removeCanvas()

  generateVarationColors: ->
    @VibrantSwatch = @findColorVariation(@TARGET_NORMAL_LUMA, @MIN_NORMAL_LUMA, @MAX_NORMAL_LUMA,
      @TARGET_VIBRANT_SATURATION, @MIN_VIBRANT_SATURATION, 1);

    @LightVibrantSwatch = @findColorVariation(@TARGET_LIGHT_LUMA, @MIN_LIGHT_LUMA, 1,
      @TARGET_VIBRANT_SATURATION, @MIN_VIBRANT_SATURATION, 1);

    @DarkVibrantSwatch = @findColorVariation(@TARGET_DARK_LUMA, 0, @MAX_DARK_LUMA,
      @TARGET_VIBRANT_SATURATION, @MIN_VIBRANT_SATURATION, 1);

    @MutedSwatch = @findColorVariation(@TARGET_NORMAL_LUMA, @MIN_NORMAL_LUMA, @MAX_NORMAL_LUMA,
      @TARGET_MUTED_SATURATION, 0, @MAX_MUTED_SATURATION);

    @LightMutedSwatch = @findColorVariation(@TARGET_LIGHT_LUMA, @MIN_LIGHT_LUMA, 1,
      @TARGET_MUTED_SATURATION, 0, @MAX_MUTED_SATURATION);

    @DarkMutedSwatch = @findColorVariation(@TARGET_DARK_LUMA, 0, @MAX_DARK_LUMA,
      @TARGET_MUTED_SATURATION, 0, @MAX_MUTED_SATURATION);

  generateEmptySwatches: ->
    if @VibrantSwatch is undefined
      # If we do not have a vibrant color...
      if @DarkVibrantSwatch isnt undefined
        # ...but we do have a dark vibrant, generate the value by modifying the luma
        hsl = @DarkVibrantSwatch.getHsl()
        hsl[2] = @TARGET_NORMAL_LUMA
        @VibrantSwatch = new Swatch Vibrant.hslToRgb(hsl[0], hsl[1], hsl[2]), 0

    if @DarkVibrantSwatch is undefined
      # If we do not have a vibrant color...
      if @VibrantSwatch isnt undefined
        # ...but we do have a dark vibrant, generate the value by modifying the luma
        hsl = @VibrantSwatch.getHsl()
        hsl[2] = @TARGET_DARK_LUMA
        @DarkVibrantSwatch = new Swatch Vibrant.hslToRgb(hsl[0], hsl[1], hsl[2]), 0

  findMaxPopulation: ->
    population = 0
    population = Math.max(population, swatch.getPopulation()) for swatch in @_swatches
    population

  findColorVariation: (targetLuma, minLuma, maxLuma, targetSaturation, minSaturation, maxSaturation) ->
    max = undefined
    maxValue = 0

    for swatch in @_swatches
      sat = swatch.getHsl()[1];
      luma = swatch.getHsl()[2]

      if sat >= minSaturation and sat <= maxSaturation and
        luma >= minLuma and luma <= maxLuma and
        not @isAlreadySelected(swatch)
          value = @createComparisonValue sat, targetSaturation, luma, targetLuma,
            swatch.getPopulation(), @HighestPopulation
          if max is undefined or value > maxValue
            max = swatch
            maxValue = value

    max

  createComparisonValue: (saturation, targetSaturation,
      luma, targetLuma, population, maxPopulation) ->
    @weightedMean(
      @invertDiff(saturation, targetSaturation), @WEIGHT_SATURATION,
      @invertDiff(luma, targetLuma), @WEIGHT_LUMA,
      population / maxPopulation, @WEIGHT_POPULATION
    )

  invertDiff: (value, targetValue) ->
    1 - Math.abs value - targetValue

  weightedMean: (values...) ->
    sum = 0
    sumWeight = 0
    i = 0
    while i < values.length
      value = values[i]
      weight = values[i + 1]
      sum += value * weight
      sumWeight += weight
      i += 2
    sum / sumWeight

  swatches: =>
      Vibrant: @VibrantSwatch
      Muted: @MutedSwatch
      DarkVibrant: @DarkVibrantSwatch
      DarkMuted: @DarkMutedSwatch
      LightVibrant: @LightVibrantSwatch
      LightMuted: @LightMuted


  isAlreadySelected: (swatch) ->
    @VibrantSwatch is swatch or @DarkVibrantSwatch is swatch or
      @LightVibrantSwatch is swatch or @MutedSwatch is swatch or
      @DarkMutedSwatch is swatch or @LightMutedSwatch is swatch

  @rgbToHsl: (r, g, b) ->
    r /= 255
    g /= 255
    b /= 255
    max = Math.max(r, g, b)
    min = Math.min(r, g, b)
    h = undefined
    s = undefined
    l = (max + min) / 2
    if max == min
      h = s = 0
      # achromatic
    else
      d = max - min
      s = if l > 0.5 then d / (2 - max - min) else d / (max + min)
      switch max
        when r
          h = (g - b) / d + (if g < b then 6 else 0)
        when g
          h = (b - r) / d + 2
        when b
          h = (r - g) / d + 4
      h /= 6
    [h, s, l]

  @hslToRgb: (h, s, l) ->
    r = undefined
    g = undefined
    b = undefined

    hue2rgb = (p, q, t) ->
      if t < 0
        t += 1
      if t > 1
        t -= 1
      if t < 1 / 6
        return p + (q - p) * 6 * t
      if t < 1 / 2
        return q
      if t < 2 / 3
        return p + (q - p) * (2 / 3 - t) * 6
      p

    if s == 0
      r = g = b = l
      # achromatic
    else
      q = if l < 0.5 then l * (1 + s) else l + s - (l * s)
      p = 2 * l - q
      r = hue2rgb(p, q, h + 1 / 3)
      g = hue2rgb(p, q, h)
      b = hue2rgb(p, q, h - (1 / 3))
    [
      r * 255
      g * 255
      b * 255
    ]


###
  CanvasImage Class
  Class that wraps the html image element and canvas.
  It also simplifies some of the canvas context manipulation
  with a set of helper functions.
  Stolen from https://github.com/lokesh/color-thief
###

window.CanvasImage = class CanvasImage
  constructor: (image) ->
    @canvas = document.createElement('canvas')
    @context = @canvas.getContext('2d')
    document.body.appendChild @canvas
    @width = @canvas.width = image.width
    @height = @canvas.height = image.height
    @context.drawImage image, 0, 0, @width, @height

  clear: ->
    @context.clearRect 0, 0, @width, @height

  update: (imageData) ->
    @context.putImageData imageData, 0, 0

  getPixelCount: ->
    @width * @height

  getImageData: ->
    @context.getImageData 0, 0, @width, @height

  removeCanvas: ->
    @canvas.parentNode.removeChild @canvas
