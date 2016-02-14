define(['paperdialoghelper', 'events', 'paper-checkbox'], function (paperDialogHelper, events) {

    function updateFilterControls(context, query) {

        $('.chkStandardFilter', context).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });
    }

    function triggerChange(instance) {

        events.trigger(instance, 'filterchange');
    }

    function bindEvents(instance, context, query) {
        
        $('.chkStandardFilter', context).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Filters = filters;
            triggerChange(instance);
        });
    }

    return function (options) {

        var self = this;

        self.show = function () {
            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/filterdialog/filterdialog.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;
                    var dlg = paperDialogHelper.createDialog({
                        removeOnClose: true,
                        modal: false,
                        enableHistory: false,
                        entryAnimationDuration: 160,
                        exitAnimationDuration: 200
                    });

                    dlg.classList.add('ui-body-b');
                    dlg.classList.add('background-theme-b');

                    dlg.classList.add('formDialog');

                    var html = '';

                    html += Globalize.translateDocument(template);

                    dlg.innerHTML = html;
                    document.body.appendChild(dlg);

                    paperDialogHelper.open(dlg);

                    dlg.addEventListener('iron-overlay-closed', resolve);

                    updateFilterControls(dlg, options.query);
                    bindEvents(self, dlg, options.query);
                }

                xhr.send();
            });
        };

    };
});