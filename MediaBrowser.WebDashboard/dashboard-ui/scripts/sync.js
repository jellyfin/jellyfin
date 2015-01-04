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

            Quality: $('#selectQuality', form).val(),

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

        ApiClient.getJSON(ApiClient.getUrl('Sync/Options', {

            UserId: userId,
            ItemIds: (options.items || []).map(function (i) {
                return i.Id || i;
            }).join(','),

            ParentId: options.ParentId,
            Category: options.Category

        })).done(function (result) {

            var targets = result.Targets;

            var html = '<div data-role="panel" data-position="right" data-display="overlay" class="syncPanel" data-position-fixed="true" data-theme="a">';

            html += '<div>';
            html += '<h1 style="margin-top:.5em;">' + Globalize.translate('SyncMedia') + '</h1>';

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

            html += '</div>';

            html += '<br/>';

            html += '<div>';
            html += '<label for="selectQuality">' + Globalize.translate('LabelQuality') + '</label>';
            html += '<select id="selectQuality" data-mini="true">';
            html += '<option value="High">' + Globalize.translate('OptionHigh') + '</option>';
            html += '<option value="Medium">' + Globalize.translate('OptionMedium') + '</option>';
            html += '<option value="Low">' + Globalize.translate('OptionLow') + '</option>';
            html += '</select>';
            html += '</div>';

            //html += '<div data-role="collapsible" style="margin:1.5em 0">';
            //html += '<h2>' + Globalize.translate('HeaderSettings') + '</h2>';
            //html += '<div style="margin:0 -.5em 0 -.25em;">';

            if (result.Options.indexOf('UnwatchedOnly') != -1) {
                html += '<br/>';
                html += '<div>';
                html += '<label for="chkUnwatchedOnly">' + Globalize.translate('OptionSyncUnwatchedVideosOnly') + '</label>';
                html += '<input type="checkbox" id="chkUnwatchedOnly" data-mini="true" />';
                html += '<div class="fieldDescription">' + Globalize.translate('OptionSyncUnwatchedVideosOnlyHelp') + '</div>';
                html += '</div>';
            }

            if (result.Options.indexOf('SyncNewContent') != -1) {
                html += '<br/>';
                html += '<div>';
                html += '<label for="chkSyncNewContent">' + Globalize.translate('OptionAutomaticallySyncNewContent') + '</label>';
                html += '<input type="checkbox" id="chkSyncNewContent" data-mini="true" />';
                html += '<div class="fieldDescription">' + Globalize.translate('OptionAutomaticallySyncNewContentHelp') + '</div>';
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

            $('form', elem).on('submit', function () {

                submitJob(userId, options, this);
                return false;
            });
        });
    }

    function showUnwatchedFilter(items) {

        return items.filter(function (i) {

            return i.MediaType == "Video" || i.IsFolder || i.Type == "Person" || i.Type == "Genre" || i.Type == "MusicGenre" || i.Type == "GameGenre" || i.Type == "Studio" || i.Type == "MusicArtist";

        }).length > 0;
    }

    function showItemLimit(items) {

        return items.length > 1 || items.filter(function (i) {

            return i.IsFolder || i.Type == "Person" || i.Type == "Genre" || i.Type == "MusicGenre" || i.Type == "GameGenre" || i.Type == "Studio" || i.Type == "MusicArtist";

        }).length > 0;
    }

    function showSyncNew(items) {

        return items.filter(function (i) {

            return i.IsFolder || i.Type == "Person" || i.Type == "Genre" || i.Type == "MusicGenre" || i.Type == "GameGenre" || i.Type == "Studio" || i.Type == "MusicArtist";

        }).length > 0;
    }

    function isAvailable(item, user) {

        return false;
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
                $('.categorySyncButton', page).hide();
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