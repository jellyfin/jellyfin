iron-a11y-announcer
===================

`iron-a11y-announcer` is a singleton element that is intended to add a11y
to features that require on-demand announcement from screen readers. In
order to make use of the announcer, it is best to request its availability
in the announcing element.

Example:

    Polymer({
      is: 'x-chatty',
      attached: function() {
        // This will create the singlton element if it has not
        // been created yet:
        Polymer.IronA11yAnnouncer.requestAvailability();
      }
    });
    
After the `iron-a11y-announcer` has been made available, elements can
make announces by firing bubbling `iron-announce` events.

Example:

    this.fire('iron-announce', {
      text: 'This is an announcement!'
    }, { bubbles: true });
    
Note: announcements are only audible if you have a screen reader enabled.
