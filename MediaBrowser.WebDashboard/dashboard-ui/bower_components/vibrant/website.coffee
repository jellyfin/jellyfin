document.addEventListener 'DOMContentLoaded', ->
  examples = document.querySelectorAll '.examples > div'

  for example in examples
    img = example.querySelector('img')
    img.setAttribute('src', img.getAttribute('data-src'))
    img.addEventListener 'load', (e) ->
      vibrant = new Vibrant this
      panel = e.target.parentElement
      panel = panel.parentElement while not panel.classList.contains('panel')

      panel.style.backgroundColor = vibrant.VibrantSwatch.getHex()
      panel.style.color = vibrant.VibrantSwatch.getTitleTextColor()

      colors = document.createElement 'div'
      colors.classList.add 'colors'
      panel.querySelector('.panel-body').appendChild colors
      
      profiles = ['VibrantSwatch', 'MutedSwatch', 'DarkVibrantSwatch', 'DarkMutedSwatch', 'LightVibrantSwatch', 'LightMutedSwatch']
      for profileName in profiles
        profile = vibrant[profileName]
        if not profile then continue
        colorHolder = document.createElement 'div'
        color = document.createElement 'div'
        color.classList.add 'color'
        color.classList.add 'shadow-z-1'
        color.style.backgroundColor = profile.getHex()
        colorName = document.createElement 'span'
        colorName.innerHTML = profileName.substring(0, profileName.length - 6)

        colorHolder.appendChild color
        colorHolder.appendChild colorName

        colors.appendChild colorHolder