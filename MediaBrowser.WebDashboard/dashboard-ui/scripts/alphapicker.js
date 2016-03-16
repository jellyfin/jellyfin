define(['jQuery'], function ($) {

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

    function init(container, picker) {

        $('.itemsContainer', container).addClass('itemsContainerWithAlphaPicker');

        picker.innerHTML = getPickerHtml();

        $(picker).on('click', 'a', function () {

            var elem = this;

            var isSelected = elem.classList.contains('selectedCharacter');

            $('.selectedCharacter', picker).removeClass('selectedCharacter');

            if (!isSelected) {

                elem.classList.add('selectedCharacter');
                $(picker).trigger('alphaselect', [this.innerHTML]);
            } else {
                $(picker).trigger('alphaclear');
            }
        });
    }

    pageClassOn('pageinit', "libraryPage", function () {

        var page = this;

        var pickers = page.querySelectorAll('.alphabetPicker');

        if (!pickers.length) {
            return;
        }

        if (page.classList.contains('pageWithAbsoluteTabs')) {

            for (var i = 0, length = pickers.length; i < length; i++) {
                init($(pickers[i]).parents('.pageTabContent'), pickers[i]);
            }

        } else {
            init(page, pickers[0]);
        }
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

});