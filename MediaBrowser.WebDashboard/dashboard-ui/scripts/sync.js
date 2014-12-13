(function (window, $) {

    function submitJob(userId, syncOptions, form) {

        if (!userId) {
            throw new Error('userId cannot be null');
        }

        if (!syncOptions) {
            throw new Error('syncOptions cannot be null');
        }

        if (!form) {
            throw new Error('form cannot be null');
        }

        var target = $('.radioSync:checked', form).get().map(function (c) {

            return c.getAttribute('data-targetid');

        })[0];

        if (!target) {

            Dashboard.alert(Globalize.translate('MessagePleaseSelectDeviceToSyncTo'));
            return;
        }

        var options = {

            userId: userId,
            TargetId: target,

            ItemIds: syncOptions.items.map(function (i) {
                return i.Id || i;
            }).join(','),

            Quality: $('.radioSyncQuality', form)[0].getAttribute('data-value'),

            Name: $('#txtSyncJobName', form).val()
        };

        ApiClient.ajax({

            type: "POST",
            url: ApiClient.getUrl("Sync/Jobs"),
            data: JSON.stringify(options),
            contentType: "application/json"

        }).done(function () {

            $('.syncPanel').panel('close');
            $(window.SyncManager).trigger('jobsubmit');
            Dashboard.alert(Globalize.translate('MessageSyncJobCreated'));
        });
    }

    function showSyncMenu(options) {

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getJSON(ApiClient.getUrl('Sync/Targets', {

            UserId: userId

        })).done(function (targets) {

            var html = '<div data-role="panel" data-position="right" data-display="overlay" class="syncPanel" data-position-fixed="true" data-theme="a">';

            html += '<div>';
            html += '<h1 style="margin-top:.5em;">' + Globalize.translate('SyncMedia') + '</h1>';

            html += '<form class="formSubmitSyncRequest">';

            if (options.items.length > 1) {

                html += '<p>';
                html += '<label for="txtSyncJobName">' + Globalize.translate('LabelSyncJobName') + '</label>';
                html += '<input type="text" id="txtSyncJobName" class="txtSyncJobName" required="required" />';
                html += '</p>';
            }

            html += '<div>';
            html += '<fieldset data-role="controlgroup">';
            html += '<legend>' + Globalize.translate('LabelSyncTo') + '</legend>';

            var index = 0;

            html += targets.map(function (t) {

                var targetHtml = '<label for="radioSync' + t.Id + '">' + t.Name + '</label>';

                var checkedHtml = index ? '' : ' checked="checked"';
                targetHtml += '<input class="radioSync" data-targetid="' + t.Id + '" type="radio" id="radioSync' + t.Id + '"' + checkedHtml + ' />';

                index++;
                return targetHtml;

            }).join('');

            html += '</fieldset>';
            html += '</div>';

            html += '<br/>';

            html += '<div>';
            html += '<fieldset data-role="controlgroup">';
            html += '<legend>' + Globalize.translate('LabelQuality') + '</legend>';
            html += '<label for="radioHighSyncQuality">' + Globalize.translate('OptionHigh') + '</label>';
            html += '<input type="radio" id="radioHighSyncQuality" name="radioSyncQuality" checked="checked" class="radioSyncQuality" data-value="High" />';
            html += '<label for="radioMediumSyncQuality">' + Globalize.translate('OptionMedium') + '</label>';
            html += '<input type="radio" id="radioMediumSyncQuality" name="radioSyncQuality" class="radioSyncQuality" data-value="Medium" />';
            html += '<label for="radioLowSyncQuality">' + Globalize.translate('OptionLow') + '</label>';
            html += '<input type="radio" id="radioLowSyncQuality" name="radioSyncQuality" class="radioSyncQuality" data-value="Low" />';
            html += '</fieldset>';
            html += '</div>';

            html += '<br/>';
            html += '<p>';
            html += '<button type="submit" data-icon="refresh" data-theme="b">' + Globalize.translate('ButtonSync') + '</button>';
            html += '</p>';

            html += '</form>';
            html += '</div>';
            html += '</div>';

            $(document.body).append(html);

            var elem = $('.syncPanel').panel({}).trigger('create').panel("open").on("panelclose", function () {
                $(this).off("panelclose").remove();
            });

            $('form', elem).on('submit', function () {

                submitJob(userId, options, this);
                return false;
            });
        });
    }

    function isAvailable(item, user) {

        //return false;
        return item.SupportsSync;
    }

    window.SyncManager = {

        showMenu: showSyncMenu,

        isAvailable: isAvailable

    };

})(window, jQuery);