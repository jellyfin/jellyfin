define(['paperdialoghelper', 'events', 'paper-checkbox'], function (paperDialogHelper, events) {

    function updateFilterControls(context, options) {

        var query = options.query;

        if (options.mode == 'livetvchannels') {

            $('.chkFavorite', context).checked(query.IsFavorite == true);
            $('.chkLikes', context).checked(query.IsLiked == true);
            $('.chkDislikes', context).checked(query.IsDisliked == true);

        } else {
            $('.chkStandardFilter', context).each(function () {

                var filters = "," + (query.Filters || "");
                var filterName = this.getAttribute('data-filter');

                this.checked = filters.indexOf(',' + filterName) != -1;

            });
        }
    }

    function triggerChange(instance) {

        events.trigger(instance, 'filterchange');
    }

    function bindEvents(instance, context, options) {

        var query = options.query;

        if (options.mode == 'livetvchannels') {

            $('.chkFavorite', context).on('change', function () {
                query.StartIndex = 0;
                query.IsFavorite = this.checked ? true : null;
                triggerChange(instance);
            });


            $('.chkLikes', context).on('change', function () {

                query.StartIndex = 0;
                query.IsLiked = this.checked ? true : null;
                triggerChange(instance);
            });

            $('.chkDislikes', context).on('change', function () {

                query.StartIndex = 0;
                query.IsDisliked = this.checked ? true : null;
                triggerChange(instance);
            });

        } else {
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
    }

    function setVisibility(context, options) {

        if (options.mode == 'livetvchannels') {
            hideByClass(context, 'nolivetvchannels');
        }

    }

    function hideByClass(context, className) {

        var elems = context.querySelectorAll('.' + className);

        for (var i = 0, length = elems.length; i < length; i++) {
            elems[i].classList.add('hide');
        }
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

                    dlg.classList.add('ui-body-a');
                    dlg.classList.add('background-theme-a');

                    dlg.classList.add('formDialog');

                    var html = '';

                    html += Globalize.translateDocument(template);

                    dlg.innerHTML = html;
                    setVisibility(dlg, options);
                    document.body.appendChild(dlg);

                    paperDialogHelper.open(dlg);

                    dlg.addEventListener('iron-overlay-closed', resolve);

                    updateFilterControls(dlg, options);
                    bindEvents(self, dlg, options);
                }

                xhr.send();
            });
        };

    };
});