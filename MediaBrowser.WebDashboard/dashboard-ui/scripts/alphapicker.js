(function (window, document, $) {

    function getPickerHtml() {

        var html = '';

        html += '<a href="#">#</a>';
        html += '<a href="#">A</a>';
        html += '<a href="#">B</a>';
        html += '<a href="#">C</a>';
        html += '<a href="#">D</a>';
        html += '<a href="#">E</a>';
        html += '<a href="#">F</a>';
        html += '<a href="#">G</a>';
        html += '<a href="#">H</a>';
        html += '<a href="#">I</a>';
        html += '<a href="#">J</a>';
        html += '<a href="#">K</a>';
        html += '<a href="#">L</a>';
        html += '<a href="#">M</a>';
        html += '<a href="#">N</a>';
        html += '<a href="#">O</a>';
        html += '<a href="#">P</a>';
        html += '<a href="#">Q</a>';
        html += '<a href="#">R</a>';
        html += '<a href="#">S</a>';
        html += '<a href="#">T</a>';
        html += '<a href="#">U</a>';
        html += '<a href="#">V</a>';
        html += '<a href="#">W</a>';
        html += '<a href="#">X</a>';
        html += '<a href="#">Y</a>';
        html += '<a href="#">Z</a>';

        return html;
    }

    $(document).on('pageinitdepends', ".libraryPage", function () {

        var page = this;

        var picker = page.querySelector('.alphabetPicker');

        if (!picker) {
            return;
        }

        $('.itemsContainer', page).addClass('itemsContainerWithAlphaPicker');

        picker.innerHTML = getPickerHtml();

        Events.on(picker, 'click', 'a', function () {

            var elem = this;

            var isSelected = elem.classList.contains('selectedCharacter');

            $('.selectedCharacter', picker).removeClass('selectedCharacter');

            if (!isSelected) {

                elem.classList.add('selectedCharacter');
                Events.trigger(picker, 'alphaselect', [this.innerHTML]);
            } else {
                Events.trigger(picker, 'alphaclear');
            }
        });
    });

    $.fn.alphaValue = function (val) {

        if (val == null) {
            return $('.selectedCharacter', this).html();
        }

        val = val.toLowerCase();

        $('.selectedCharacter', this).removeClass('selectedCharacter');

        $('a', this).each(function () {

            if (this.innerHTML.toLowerCase() == val) {

                this.classList.add('selectedCharacter');

            } else {
                this.classList.remove('selectedCharacter');
            }

        });

        return this;
    };

    $.fn.alphaClear = function (val) {

        return this.alphaValue('');
    };

})(window, document, jQuery);