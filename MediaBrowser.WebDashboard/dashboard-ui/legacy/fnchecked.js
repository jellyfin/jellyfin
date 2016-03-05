// TODO: This needs to be deprecated, but it's used heavily
$.fn.checked = function (value) {
    if (value === true || value === false) {
        // Set the value of the checkbox
        return $(this).each(function () {
            this.checked = value;
        });
    } else {
        // Return check state
        return this.length && this[0].checked;
    }
};