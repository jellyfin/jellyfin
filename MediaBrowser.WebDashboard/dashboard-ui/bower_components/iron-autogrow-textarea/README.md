# iron-autogrow-textarea

`iron-autogrow-textarea` is an element containing a textarea that grows in height as more
lines of input are entered. Unless an explicit height or the `maxRows` property is set, it will
never scroll.

Example:

    <iron-autogrow-textarea id="a1">
      <textarea id="t1"></textarea>
    </iron-autogrow-textarea>

Because the `textarea`'s `value` property is not observable, you should use
this element's `bind-value` instead for imperative updates. Alternatively, if
you do set the `textarea`'s `value` imperatively, you must also call `update`
to notify this element the value has changed.

    Example:
        /* preferred, using the example HTML above*/
        a1.bindValue = 'some\ntext';

        /* alternatively, */
        t1.value = 'some\ntext';
        a1.update();
