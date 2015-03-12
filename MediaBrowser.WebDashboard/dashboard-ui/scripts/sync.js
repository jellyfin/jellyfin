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

        var target = $('#selectSyncTarget', form).val();

        if (!target) {

            Dashboard.alert(Globalize.translate('MessagePleaseSelectDeviceToSyncTo'));
            return;
        }

        var options = {

            userId: userId,
            TargetId: target,

            Quality: $('#selectQuality', form).val() || null,

            Name: $('#txtSyncJobName', form).val(),

            SyncNewContent: $('#chkSyncNewContent', form).checked(),
            UnwatchedOnly: $('#chkUnwatchedOnly', form).checked(),
            ItemLimit: $('#txtItemLimit').val() || null,

            ParentId: syncOptions.ParentId,
            Category: syncOptions.Category
        };

        if (syncOptions.items && syncOptions.items.length) {
            options.ItemIds = (syncOptions.items || []).map(function (i) {
                return i.Id || i;
            }).join(',');
        }

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

        var dialogOptionsQuery = {
            UserId: userId,
            ItemIds: (options.items || []).map(function(i) {
                return i.Id || i;
            }).join(','),

            ParentId: options.ParentId,
            Category: options.Category
        };

        ApiClient.getJSON(ApiClient.getUrl('Sync/Options', dialogOptionsQuery)).done(function (result) {

            var targets = result.Targets;

            var html = '<div data-role="panel" data-position="right" data-display="overlay" class="syncPanel" data-position-fixed="true" data-theme="a">';

            html += '<div>';

            html += '<div style="margin:1em 0 1.5em;">';
            html += '<h1 style="margin: 0;display:inline-block;vertical-align:middle;">' + Globalize.translate('SyncMedia') + '</h1>';
            html += '<a class="accentButton accentButton-g" style="display:inline-block;vertical-align:middle;margin-top:0;margin-left: 20px;" href="https://github.com/MediaBrowser/Wiki/wiki/Sync" target="_blank">';
            html += '<i class="fa fa-info-circle"></i>';
            html += Globalize.translate('ButtonHelp');
            html += '</a>';
            html += '</div>';

            html += '<form class="formSubmitSyncRequest">';

            if (result.Options.indexOf('Name') != -1) {

                html += '<p>';
                html += '<label for="txtSyncJobName">' + Globalize.translate('LabelSyncJobName') + '</label>';
                html += '<input type="text" id="txtSyncJobName" class="txtSyncJobName" required="required" />';
                html += '</p>';
            }

            html += '<div>';
            html += '<label for="selectSyncTarget">' + Globalize.translate('LabelSyncTo') + '</label>';
            html += '<select id="selectSyncTarget" required="required" data-mini="true">';

            html += targets.map(function (t) {

                return '<option value="' + t.Id + '">' + t.Name + '</option>';

            }).join('');
            html += '</select>';
            if (!targets.length) {
                html += '<div class="fieldDescription">' + Globalize.translate('LabelSyncNoTargetsHelp') + '</div>';
                html += '<div class="fieldDescription"><a href="https://github.com/MediaBrowser/Wiki/wiki/Sync" target="_blank">' + Globalize.translate('ButtonLearnMore') + '</a></div>';
            }
            html += '</div>';

            html += '<br/>';

            if (result.Options.indexOf('Quality') != -1) {
                html += '<div>';
                html += '<label for="selectQuality">' + Globalize.translate('LabelQuality') + '</label>';
                html += '<select id="selectQuality" data-mini="true" required="required">';
                html += '</select>';
                html += '<div class="fieldDescription">' + Globalize.translate('LabelSyncQualityHelp') + '</div>';
                html += '</div>';
            }

            //html += '<div data-role="collapsible" style="margin:1.5em 0">';
            //html += '<h2>' + Globalize.translate('HeaderSettings') + '</h2>';
            //html += '<div style="margin:0 -.5em 0 -.25em;">';

            if (result.Options.indexOf('SyncNewContent') != -1) {
                html += '<br/>';
                html += '<div>';
                html += '<label for="chkSyncNewContent">' + Globalize.translate('OptionAutomaticallySyncNewContent') + '</label>';
                html += '<input type="checkbox" id="chkSyncNewContent" data-mini="true" checked="checked" />';
                html += '<div class="fieldDescription">' + Globalize.translate('OptionAutomaticallySyncNewContentHelp') + '</div>';
                html += '</div>';
            }

            if (result.Options.indexOf('UnwatchedOnly') != -1) {
                html += '<br/>';
                html += '<div>';
                html += '<label for="chkUnwatchedOnly">' + Globalize.translate('OptionSyncUnwatchedVideosOnly') + '</label>';
                html += '<input type="checkbox" id="chkUnwatchedOnly" data-mini="true" />';
                html += '<div class="fieldDescription">' + Globalize.translate('OptionSyncUnwatchedVideosOnlyHelp') + '</div>';
                html += '</div>';
            }

            if (result.Options.indexOf('ItemLimit') != -1) {
                html += '<br/>';
                html += '<div>';
                html += '<label for="txtItemLimit">' + Globalize.translate('LabelItemLimit') + '</label>';
                html += '<input type="number" id="txtItemLimit" step="1" min="1" />';
                html += '<div class="fieldDescription">' + Globalize.translate('LabelItemLimitHelp') + '</div>';
                html += '</div>';
            }

            //html += '</div>';
            //html += '</div>';

            html += '<br/>';
            html += '<p>';
            html += '<button type="submit" data-icon="cloud" data-theme="b">' + Globalize.translate('ButtonSync') + '</button>';
            html += '</p>';

            html += '</form>';
            html += '</div>';
            html += '</div>';

            $(document.body).append(html);

            var elem = $('.syncPanel').panel({}).trigger('create').panel("open").on("panelclose", function () {
                $(this).off("panelclose").remove();
            });

            $('#selectSyncTarget', elem).on('change', function () {

                loadQualityOptions(elem, this.value, dialogOptionsQuery);

            }).trigger('change');

            $('form', elem).on('submit', function () {

                submitJob(userId, options, this);
                return false;
            });
        });
    }

    function loadQualityOptions(panel, targetId, dialogOptionsQuery) {

        dialogOptionsQuery.TargetId = targetId;

        ApiClient.getJSON(ApiClient.getUrl('Sync/Options', dialogOptionsQuery)).done(function (options) {

            $('#selectQuality', panel).html(options.QualityOptions.map(function (o) {

                var selectedAttribute = o.IsDefault ? ' selected="selected"' : '';
                return '<option value="' + o.Id + '"' + selectedAttribute + '>' + o.Name + '</option>';

            }).join('')).selectmenu('refresh');

        });

    }

    function isAvailable(item, user) {

        return item.SupportsSync;
    }

    window.SyncManager = {

        showMenu: showSyncMenu,

        isAvailable: isAvailable

    };

    function showSyncButtonsPerUser(page) {

        var apiClient = ConnectionManager.currentApiClient();

        if (!apiClient) {
            return;
        }

        Dashboard.getCurrentUser().done(function (user) {

            if (user.Policy.EnableSync) {
                $('.categorySyncButton', page).show();
            } else {
                $('.categorySyncButton', page).hide();
            }

        });
    }

    function onCategorySyncButtonClick(page, button) {

        var category = button.getAttribute('data-category');
        var parentId = LibraryMenu.getTopParentId();

        SyncManager.showMenu({
            ParentId: parentId,
            Category: category
        });
    }

    $(document).on('pageinit', ".libraryPage", function () {

        var page = this;

        $('.categorySyncButton', page).on('click', function () {

            onCategorySyncButtonClick(page, this);
        });

    }).on('pagebeforeshow', ".libraryPage", function () {

        var page = this;

        showSyncButtonsPerUser(page);

    });


})(window, jQuery);