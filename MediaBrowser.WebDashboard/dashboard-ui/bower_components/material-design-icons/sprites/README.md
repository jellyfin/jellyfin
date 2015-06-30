# Material icon sprites

These sprites may not be completely up-to-date, we will update them soon.

## Creating your own sprites

Our recommended way to use sprite sheets is to create them with the subset of icons needed for the project. Here are two good options for creating your own CSS and SVG sprites.

 * [Sprity](https://www.npmjs.com/package/sprity) (previously css-sprite) for CSS sprites.
 * [svg-sprite](https://github.com/jkphl/svg-sprite) for SVG sprites.


## Using provided sprite sheets

Material design icons come with SVG and CSS sprites for each category of icon we include. These can be found in the `sprites` directory, under `svg-sprite` and `css-sprite`.

### Using CSS Sprites

To use a CSS spritesheet, reference the stylesheet for the icon category you wish to use, then include the icon definition in your markup. E.g to use one of the play icons in `css-sprite-av`:

Reference the stylesheet:

```html
	<link href="css-sprite/sprite-av-black.css" rel="stylesheet">
```

Create an element which will use the icon as a background:

```html
	<div></div>
```

Add a class referencing the `icon` sprite sheet and specific icon `icon-ic_play_circle_outline_black_24dp`, which you can get from the above stylesheet.

```html
	<div class="icon icon-ic_play_circle_outline_black_24dp"></div>
```

That's it!

Don't forget to publish the corresponding CSS and SVG/PNG files when deploying your project.

### Using SVG Sprites

Similarly, to use an SVG sprite sheet, reference the stylesheet for the icon category, then include the icon definition in your markup.

E.g: to use one of the play icons in `svg-sprite-av`, reference the stylesheet:

```html
	<link href="svg-sprite/svg-sprite-av.css" rel="stylesheet">
```

Create an element which will use the icon as a background:

```html
	<div></div>
```

Next, make sure to set a dimension for the icon. This can either be done inline or via a class. We'll use a class for this example:

```html
	<style>
		.svg-ic_play_circle_outline_24px-dims { width: 24px; height: 24px; }
	</style>
```

Finally, set the dimension and specific icon `svg-ic_play_circle_outline_24px`, which you can get from the above stylesheet.

```html
	<div class="svg-ic_play_circle_outline_24px svg-ic_play_circle_outline_24px-dims"></div>
```
