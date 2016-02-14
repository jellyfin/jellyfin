define(['jqmwidget'], function () {

    var rbrace = /(?:\{[\s\S]*\}|\[[\s\S]*\])$/;

    $.extend($.mobile, {

        // Namespace used framework-wide for data-attrs. Default is no namespace

        // Retrieve an attribute from an element and perform some massaging of the value

        getAttribute: function (element, key) {
            var data;

            element = element.jquery ? element[0] : element;

            if (element && element.getAttribute) {
                data = element.getAttribute("data-" + key);
            }

            // Copied from core's src/data.js:dataAttr()
            // Convert from a string to a proper data type
            try {
                data = data === "true" ? true :
                    data === "false" ? false :
                    data === "null" ? null :
                    // Only convert to a number if it doesn't change the string
                    +data + "" === data ? +data :
                    rbrace.test(data) ? JSON.parse(data) :
                    data;
            } catch (err) { }

            return data;
        }

    });


    $.widget("mobile.table", {
        options: {
            enhanced: false
        },

        _create: function () {
            if (!this.options.enhanced) {
                this.element.addClass("ui-table");
            }

            // extend here, assign on refresh > _setHeaders
            $.extend(this, {

                // Expose headers and allHeaders properties on the widget
                // headers references the THs within the first TR in the table
                headers: undefined,

                // allHeaders references headers, plus all THs in the thead, which may
                // include several rows, or not
                allHeaders: undefined
            });

            this._refresh(true);
        },

        _setHeaders: function () {
            var trs = this.element.find("thead tr");

            this.headers = this.element.find("tr:eq(0)").children();
            this.allHeaders = this.headers.add(trs.children());
        },

        refresh: function () {
            this._refresh();
        },

        rebuild: $.noop,

        _refresh: function ( /* create */) {
            var table = this.element,
				trs = table.find("thead tr");

            // updating headers on refresh (fixes #5880)
            this._setHeaders();

            // Iterate over the trs
            trs.each(function () {
                var columnCount = 0;

                // Iterate over the children of the tr
                $(this).children().each(function () {
                    var span = parseInt(this.getAttribute("colspan"), 10),
						selector = ":nth-child(" + (columnCount + 1) + ")",
						j;

                    this.setAttribute("data-colstart", columnCount + 1);

                    if (span) {
                        for (j = 0; j < span - 1; j++) {
                            columnCount++;
                            selector += ", :nth-child(" + (columnCount + 1) + ")";
                        }
                    }

                    // Store "cells" data on header as a reference to all cells in the
                    // same column as this TH
                    $(this).data("cells", table.find("tr").not(trs.eq(0)).not(this).children(selector));

                    columnCount++;
                });
            });
        }
    });


    $.widget("mobile.table", $.mobile.table, {
        options: {
            mode: "reflow"
        },

        _create: function () {
            this._super();

            // If it's not reflow mode, return here.
            if (this.options.mode !== "reflow") {
                return;
            }

            if (!this.options.enhanced) {
                this.element.addClass("ui-table-reflow");

                this._updateReflow();
            }
        },

        rebuild: function () {
            this._super();

            if (this.options.mode === "reflow") {
                this._refresh(false);
            }
        },

        _refresh: function (create) {
            this._super(create);
            if (!create && this.options.mode === "reflow") {
                this._updateReflow();
            }
        },

        _updateReflow: function () {
            var table = this,
				opts = this.options;

            // get headers in reverse order so that top-level headers are appended last
            $(table.allHeaders.get().reverse()).each(function () {
                var cells = $(this).data("cells"),
					colstart = $.mobile.getAttribute(this, "colstart"),
					hierarchyClass = cells.not(this).filter("thead th").length && " ui-table-cell-label-top",
					contents = $(this).clone().contents(),
					iteration, filter;

                if (contents.length > 0) {

                    if (hierarchyClass) {
                        iteration = parseInt(this.getAttribute("colspan"), 10);
                        filter = "";

                        if (iteration) {
                            filter = "td:nth-child(" + iteration + "n + " + (colstart) + ")";
                        }

                        table._addLabels(cells.filter(filter),
							"ui-table-cell-label" + hierarchyClass, contents);
                    } else {
                        table._addLabels(cells, "ui-table-cell-label", contents);
                    }

                }
            });
        },

        _addLabels: function (cells, label, contents) {
            if (contents.length === 1 && contents[0].nodeName.toLowerCase() === "abbr") {
                contents = contents.eq(0).attr("title");
            }
            // .not fixes #6006
            cells
				.not(":has(b." + label + ")")
					.prepend($("<b class='" + label + "'></b>").append(contents));
        }
    });

});