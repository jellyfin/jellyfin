(function ($, document, window) {


    function createSearchHintsElement() {

        $(document.body).append('<div id="searchHints" class="searchHints"><div class="searchHintsContent">Coming soon<div><div>').off("mousedown.hidesearchhints").on("mousedown.hidesearchhints", function (e) {

            var elem = $(e.target);

            if (!elem.is('#searchHints,#txtSearch,#btnSearch') && !elem.parents('#searchHints,#txtSearch,#btnSearch').length) {

                $('#searchHints').remove();

                $(document.body).off("mousedown.hidesearchhints");
            }

        });

        var txtElem = $('#txtSearch');
        var pos = txtElem.offset();
        
        var hints = $('#searchHints')[0];

        hints.style.top = txtElem[0].offsetHeight + pos.top + 1 + "px";
        hints.style.left = pos.left + "px";

    }

    function renderSearchHints(searchTerm) {

        var hints = $('#searchHints');

        if (!hints.length) {

            hints = createSearchHintsElement();
        }
    }

    function search() {

        var self = this;

        self.getSearchHtml = function () {

            var html = '<div class="headerSearch"><form id="searchForm" name="searchForm">';

            html += '<input id="txtSearch" class="txtSearch" type="search" required="required" />';

            html += '<button id="btnSearch" class="btnSearch" type="submit">';
            html += '<img src="css/images/searchbutton.png" />';
            html += '</button>';

            html += '</form></div>';

            return html;
        };

        self.onSearchRendered = function (parentElem) {


            $('#searchForm', parentElem).on("submit", function () {

                Dashboard.alert('Coming soon.');

                return false;
            });

            $('#txtSearch', parentElem).on("keypress", function () {

                renderSearchHints(this.value);

            }).on("focus", function () {

                var value = this.value;
                
                if (value) {
                    renderSearchHints(value);
                }

            });

        };
    }

    window.Search = new search();

})(jQuery, document, window);