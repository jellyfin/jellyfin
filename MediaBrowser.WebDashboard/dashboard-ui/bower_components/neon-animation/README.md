# neon-animation

`neon-animation` is a suite of elements and behaviors to implement pluggable animated transitions for Polymer Elements using [Web Animations](https://w3c.github.io/web-animations/).

*Warning: The API may change.*

* [A basic animatable element](#basic)
* [Animation configuration](#configuration)
  * [Animation types](#configuration-types)
  * [Configuration properties](#configuration-properties)
  * [Using multiple animations](#configuration-multiple)
  * [Running animations encapsulated in children nodes](#configuration-encapsulation)
* [Page transitions](#page-transitions)
  * [Shared element animations](#shared-element)
  * [Declarative page transitions](#declarative-page)
* [Included animations](#animations)
* [Demos](#demos)

<a name="basic"></a>
## A basic animatable element

Elements that can be animated should implement the `Polymer.NeonAnimatableBehavior` behavior, or `Polymer.NeonAnimationRunnerBehavior` if they're also responsible for running an animation.

```js
Polymer({
  is: 'my-animatable',
  behaviors: [
    Polymer.NeonAnimationRunnerBehavior
  ],
  properties: {
    animationConfig: {
      value: function() {
        return {
          // provided by neon-animation/animations/scale-down-animation.html
          name: 'scale-down-animation',
          node: this
        }
      }
    }
  },
  listeners: {
    // this event is fired when the animation finishes
    'neon-animation-finish': '_onNeonAnimationFinish'
  },
  animate: function() {
    // run scale-down-animation
    this.playAnimation();
  },
  _onNeonAnimationFinish: function() {
    console.log('animation done!');
  }
});
```

[Live demo](http://morethanreal.github.io/neon-animation-demo/bower_components/neon-animation/demo/doc/basic.html)

<a name="configuration"></a>
## Animation configuration

<a name="configuration-types"></a>
### Animation types

An element might run different animations, for example it might do something different when it enters the view and when it exits from view. You can set the `animationConfig` property to a map from an animation type to configuration.

```js
Polymer({
  is: 'my-dialog',
  behaviors: [
    Polymer.NeonAnimationRunnerBehavior
  ],
  properties: {
    opened: {
      type: Boolean
    },
    animationConfig: {
      value: function() {
        return {
          'entry': {
            // provided by neon-animation/animations/scale-up-animation.html
            name: 'scale-up-animation',
            node: this
          },
          'exit': {
            // provided by neon-animation/animations/fade-out-animation.html
            name: 'fade-out-animation',
            node: this
          }
        }
      }
    }
  },
  listeners: {
    'neon-animation-finish': '_onNeonAnimationFinish'
  },
  show: function() {
    this.opened = true;
    this.style.display = 'inline-block';
    // run scale-up-animation
    this.playAnimation('entry');
  },
  hide: function() {
    this.opened = false;
    // run fade-out-animation
    this.playAnimation('exit');
  },
  _onNeonAnimationFinish: function() {
    if (!this.opened) {
      this.style.display = 'none';
    }
  }
});
```

[Live demo](http://morethanreal.github.io/neon-animation-demo/bower_components/neon-animation/demo/doc/types.html)

You can also use the convenience properties `entryAnimation` and `exitAnimation` to set `entry` and `exit` animations:

```js
properties: {
  entryAnimation: {
    value: 'scale-up-animation'
  },
  exitAnimation: {
    value: 'fade-out-animation'
  }
}
```

<a name="configuration-properties"></a>
### Configuration properties

You can pass additional parameters to configure an animation in the animation configuration object.
All animations should accept the following properties:

 * `name`: The name of an animation, ie. an element implementing `Polymer.NeonAnimationBehavior`.
 * `node`: The target node to apply the animation to. Defaults to `this`.
 * `timing`: Timing properties to use in this animation. They match the [Web Animations Animation Effect Timing interface](https://w3c.github.io/web-animations/#the-animationeffecttiming-interface). The
 properties include the following:
     * `duration`: The duration of the animation in milliseconds.
     * `delay`: The delay before the start of the animation in milliseconds.
     * `easing`: A timing function for the animation. Matches the CSS timing function values.

Animations may define additional configuration properties and they are listed in their documentation.

<a name="configuration-multiple"></a>
### Using multiple animations

Set the animation configuration to an array to combine animations, like this:

```js
animationConfig: {
  value: function() {
    return {
      // fade-in-animation is run with a 50ms delay from slide-down-animation
      'entry': [{
        name: 'slide-down-animation',
        node: this
      }, {
        name: 'fade-in-animation',
        node: this,
        timing: {delay: 50}
      }]
    }
  }
}
```

<a name="configuration-encapsulation"></a>
### Running animations encapsulated in children nodes

You can include animations in the configuration that are encapsulated in a child element that implement `Polymer.NeonAnimatableBehavior` with the `animatable` property.

```js
animationConfig: {
  value: function() {
    return {
      // run fade-in-animation on this, and the entry animation on this.$.myAnimatable
      'entry': [
        {name: 'fade-in-animation', node: this},
        {animatable: this.$.myAnimatable, type: 'entry'}
      ]
    }
  }
}
```

<a name="page-transitions"></a>
## Page transitions

*The artist formerly known as `<core-animated-pages>`*

The `neon-animated-pages` element manages a set of pages to switch between, and runs animations between the page transitions. It implements the `Polymer.IronSelectableBehavior` behavior. Each child node should implement `Polymer.NeonAnimatableBehavior` and define the `entry` and `exit` animations. During a page transition, the `entry` animation is run on the new page and the `exit` animation is run on the old page.

<a name="shared-element"></a>
### Shared element animations

Shared element animations work on multiple nodes. For example, a "hero" animation is used during a page transition to make two elements from separate pages appear to animate as a single element. Shared element animation configurations have an `id` property that identify they belong in the same animation. Elements containing shared elements also have a `sharedElements` property defines a map from `id` to element, the element involved with the animation.

In the incoming page:

```js
properties: {
  animationConfig: {
    value: function() {
      return {
        // the incoming page defines the 'entry' animation
        'entry': {
          name: 'hero-animation',
          id: 'hero',
          toPage: this
        }
      }
    }
  },
  sharedElements: {
    value: function() {
      return {
        'hero': this.$.hero
      }
    }
  }
}
```

In the outgoing page:

```js
properties: {
  animationConfig: {
    value: function() {
      return {
        // the outgoing page defines the 'exit' animation
        'exit': {
          name: 'hero-animation',
          id: 'hero',
          fromPage: this
        }
      }
    }
  },
  sharedElements: {
    value: function() {
      return {
        'hero': this.$.otherHero
      }
    }
  }
}
```

<a name="declarative-page"></a>
### Declarative page transitions

For convenience, if you define the `entry-animation` and `exit-animation` attributes on `<neon-animated-pages>`, those animations will apply for all page transitions.

For example:

```js
<neon-animated-pages id="pages" class="flex" selected="[[selected]]" entry-animation="slide-from-right-animation" exit-animation="slide-left-animation">
  <neon-animatable>1</neon-animatable>
  <neon-animatable>2</neon-animatable>
  <neon-animatable>3</neon-animatable>
  <neon-animatable>4</neon-animatable>
  <neon-animatable>5</neon-animatable>
</neon-animated-pages>
```

The new page will slide in from the right, and the old page slide away to the left.

<a name="animations"></a>
## Included animations

Single element animations:

 * `fade-in-animation` Animates opacity from `0` to `1`.
 * `fade-out-animation` Animates opacity from `1` to `0`.
 * `scale-down-animation` Animates transform from `scale(1)` to `scale(0)`.
 * `scale-up-animation` Animates transform from `scale(0)` to `scale(1)`.
 * `slide-down-animation` Animates transform from `translateY(-100%)` to `none`.
 * `slide-up-animation` Animates transform from `none` to `translateY(-100%)`.
 * `slide-left-animation` Animates transform from `none` to `translateX(-100%)`;
 * `slide-right-animation` Animates transform from `none` to `translateX(100%)`;
 * `slide-from-left-animation` Animates transform from `translateX(-100%)` to `none`;
 * `slide-from-right-animation` Animates transform from `translateX(100%)` to `none`;
 * `transform-animation` Animates a custom transform.

Note that there is a restriction that only one transform animation can be applied on the same element at a time. Use the custom `transform-animation` to combine transform properties.

Shared element animations

 * `hero-animation` Animates an element such that it looks like it scales and transforms from another element.
 * `ripple-animation` Animates an element to full screen such that it looks like it ripples from another element.

Group animations
 * `cascaded-animation` Applys an animation to an array of elements with a delay between each.

<a name="demos"></a>
## Demos

 * [Grid to full screen](http://morethanreal.github.io/neon-animation-demo/bower_components/neon-animation/demo/grid/index.html)
 * [Animation on load](http://morethanreal.github.io/neon-animation-demo/bower_components/neon-animation/demo/load/index.html)
 * [List item to detail](http://morethanreal.github.io/neon-animation-demo/bower_components/neon-animation/demo/list/index.html) (For narrow width)
 * [Dots to squares](http://morethanreal.github.io/neon-animation-demo/bower_components/neon-animation/demo/tiles/index.html)
 * [Declarative](http://morethanreal.github.io/neon-animation-demo/bower_components/neon-animation/demo/declarative/index.html)
