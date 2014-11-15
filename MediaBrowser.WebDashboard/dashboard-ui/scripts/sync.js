(function (window, $) {

    function submitJob(userId, items, form) {

        var target = $('.radioSync:checked', form).get().map(function (c) {

            return c.getAttribute('data-targetid');

        })[0];

        if (!target) {

            Dashboard.alert('Please select a device to sync to.');
            return;
        }

        var options = {

            userId: userId,
            TargetId: target,

            ItemIds: items.map(function (i) {
                return i.Id;
            }).join(','),

            Quality: $('.radioSyncQuality', form)[0].getAttribute('data-value')
        };

        ApiClient.ajax({

            type: "POST",
            url: ApiClient.getUrl("Sync/Jobs"),
            data: JSON.stringify(options),
            contentType: "application/json"

        }).done(function () {

        });
    }

    function showSyncMenu(items) {

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getJSON(ApiClient.getUrl('Sync/Targets', {

            UserId: userId

        })).done(function (targets) {

            var html = '<div data-role="panel" data-position="right" data-display="overlay" class="syncPanel" data-position-fixed="true" data-theme="a">';

            html += '<div>';
            html += '<h1 style="margin-top:.5em;">Sync Media</h1>';

            html += '<form class="formSubmitSyncRequest">';

            html += '<div>';
            html += '<fieldset data-role="controlgroup">';
            html += '<legend>Sync to:</legend>';

            html += targets.map(function (t) {

                var targetHtml = '<label for="radioSync' + t.Id + '">' + t.Name + '</label>';
                targetHtml += '<input class="radioSync" data-targetid="' + t.Id + '" type="radio" id="radioSync' + t.Id + '" />';

                return targetHtml;

            }).join('');

            html += '</fieldset>';
            html += '</div>';

            html += '<br/>';

            html += '<div>';
            html += '<fieldset data-role="controlgroup">';
            html += '<legend>Quality:</legend>';
            html += '<label for="radioHighSyncQuality">High</label>';
            html += '<input type="radio" id="radioHighSyncQuality" name="radioSyncQuality" checked="checked" class="radioSyncQuality" data-value="High" />';
            html += '<label for="radioMediumSyncQuality">Medium</label>';
            html += '<input type="radio" id="radioMediumSyncQuality" name="radioSyncQuality" class="radioSyncQuality" data-value="Medium" />';
            html += '<label for="radioLowSyncQuality">Low</label>';
            html += '<input type="radio" id="radioLowSyncQuality" name="radioSyncQuality" class="radioSyncQuality" data-value="Low" />';
            html += '</fieldset>';
            html += '</div>';

            html += '<br/>';
            html += '<p>';
            html += '<button type="submit" data-icon="refresh" data-theme="b">Sync</button>';
            html += '</p>';

            html += '</form>';
            html += '</div>';
            html += '</div>';

            $(document.body).append(html);

            var elem = $('.syncPanel').panel({}).trigger('create').panel("open").on("panelclose", function () {
                $(this).off("panelclose").remove();
            });

            $('form', elem).on('submit', function () {

                submitJob(userId, items, this);
                return false;
            });
        });
    }

    function isAvailable(item, user) {

        return false;
        return item.SupportsSync;
    }

    window.SyncManager = {

        showMenu: showSyncMenu,

        isAvailable: isAvailable

    };

})(window, jQuery);