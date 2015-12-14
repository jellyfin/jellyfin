(function (window, $) {

    var currentDialogOptions;

    function submitJob(dlg, userId, syncOptions, form) {

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

            ParentId: syncOptions.ParentId,
            Category: syncOptions.Category
        };

        setJobValues(options, form);

        if (syncOptions.items && syncOptions.items.length) {
            options.ItemIds = (syncOptions.items || []).map(function (i) {
                return i.Id || i;
            }).join(',');
        }

        ApiClient.ajax({

            type: "POST",
            url: ApiClient.getUrl("Sync/Jobs"),
            data: JSON.stringify(options),
            contentType: "application/json",
            dataType: 'json'

        }).then(function () {

            PaperDialogHelper.close(dlg);
            $(window.SyncManager).trigger('jobsubmit');
            Dashboard.alert(Globalize.translate('MessageSyncJobCreated'));
        });
    }

    function setJobValues(job, form) {

        var bitrate = $('#txtBitrate', form).val() || null;

        if (bitrate) {
            bitrate = parseFloat(bitrate) * 1000000;
        }

        job.Name = $('#txtSyncJobName', form).val();
        job.Quality = $('#selectQuality', form).val() || null;
        job.Profile = $('#selectProfile', form).val() || null;
        job.Bitrate = bitrate;
        job.ItemLimit = $('#txtItemLimit', form).val() || null;
        job.SyncNewContent = $('#chkSyncNewContent', form).checked();
        job.UnwatchedOnly = $('#chkUnwatchedOnly', form).checked();
    }

    function renderForm(options) {

        return new Promise(function (resolve, reject) {

            require(['paper-checkbox', 'paper-input'], function () {
                renderFormInternal(options);
                resolve();
            });
        });
    }

    function renderFormInternal(options) {

        var elem = options.elem;
        var dialogOptions = options.dialogOptions;

        var targets = dialogOptions.Targets;

        var html = '';

        if (options.showName || dialogOptions.Options.indexOf('Name') != -1) {

            html += '<div>';
            html += '<paper-input type="text" id="txtSyncJobName" class="txtSyncJobName" required="required" label="' + Globalize.translate('LabelSyncJobName') + '"></paper-input>';
            html += '</div>';
            html += '<br/>';
        }

        html += '<div>';
        if (options.readOnlySyncTarget) {
            html += '<paper-input type="text" id="selectSyncTarget" readonly label="' + Globalize.translate('LabelSyncTo') + '"></paper-input>';
        } else {
            html += '<label for="selectSyncTarget">' + Globalize.translate('LabelSyncTo') + '</label>';
            html += '<select id="selectSyncTarget" required="required" data-mini="true">';

            html += targets.map(function (t) {

                var isSelected = t.Id == AppInfo.deviceId;
                var selectedHtml = isSelected ? ' selected="selected"' : '';
                return '<option' + selectedHtml + ' value="' + t.Id + '">' + t.Name + '</option>';

            }).join('');
            html += '</select>';
            if (!targets.length) {
                html += '<div class="fieldDescription">' + Globalize.translate('LabelSyncNoTargetsHelp') + '</div>';
                html += '<div class="fieldDescription"><a href="https://github.com/MediaBrowser/Wiki/wiki/Sync" target="_blank">' + Globalize.translate('ButtonLearnMore') + '</a></div>';
            }
        }
        html += '</div>';

        html += '<div class="fldProfile" style="display:none;">';
        html += '<br/>';
        html += '<label for="selectProfile">' + Globalize.translate('LabelProfile') + '</label>';
        html += '<select id="selectProfile" data-mini="true">';
        html += '</select>';
        html += '<div class="fieldDescription profileDescription"></div>';
        html += '</div>';

        html += '<div class="fldQuality" style="display:none;">';
        html += '<br/>';
        html += '<label for="selectQuality">' + Globalize.translate('LabelQuality') + '</label>';
        html += '<select id="selectQuality" data-mini="true" required="required">';
        html += '</select>';
        html += '<div class="fieldDescription qualityDescription"></div>';
        html += '</div>';

        html += '<div class="fldBitrate" style="display:none;">';
        html += '<br/>';
        html += '<div>';
        html += '<paper-input type="number" step=".1" min=".1" id="txtBitrate" label="' + Globalize.translate('LabelBitrateMbps') + '"></paper-input>';
        html += '</div>';
        html += '</div>';

        if (dialogOptions.Options.indexOf('UnwatchedOnly') != -1) {
            html += '<br/>';
            html += '<div>';
            html += '<paper-checkbox id="chkUnwatchedOnly">' + Globalize.translate('OptionSyncUnwatchedVideosOnly') + '</paper-checkbox>';
            html += '<div class="fieldDescription paperCheckboxFieldDescription">' + Globalize.translate('OptionSyncUnwatchedVideosOnlyHelp') + '</div>';
            html += '</div>';
        }

        if (dialogOptions.Options.indexOf('SyncNewContent') != -1 ||
            dialogOptions.Options.indexOf('ItemLimit') != -1) {

            html += '<br/>';
            html += '<div data-role="collapsible" data-mini="true">';
            html += '<h2>' + Globalize.translate('HeaderAdvanced') + '</h2>';
            html += '<div style="padding:0 0 1em;">';
            if (dialogOptions.Options.indexOf('SyncNewContent') != -1) {
                html += '<br/>';
                html += '<div>';
                html += '<paper-checkbox id="chkSyncNewContent" checked>' + Globalize.translate('OptionAutomaticallySyncNewContent') + '</paper-checkbox>';
                html += '<div class="fieldDescription paperCheckboxFieldDescription">' + Globalize.translate('OptionAutomaticallySyncNewContentHelp') + '</div>';
                html += '</div>';
            }

            if (dialogOptions.Options.indexOf('ItemLimit') != -1) {
                html += '<div>';
                html += '<paper-input type="number" step="1" min="1" id="txtItemLimit" label="' + Globalize.translate('LabelItemLimit') + '"></paper-input>';
                html += '<div class="fieldDescription">' + Globalize.translate('LabelItemLimitHelp') + '</div>';
                html += '</div>';
            }
            html += '</div>';
            html += '</div>';
        }

        //html += '</div>';
        //html += '</div>';

        $(elem).html(html);

        $('#selectSyncTarget', elem).on('change', function () {

            loadQualityOptions(elem, this.value, options.dialogOptionsFn);

        }).trigger('change');

        $('#selectProfile', elem).on('change', function () {

            onProfileChange(elem, this.value);

        }).trigger('change');

        $('#selectQuality', elem).on('change', function () {

            onQualityChange(elem, this.value);

        }).trigger('change');

    }

    function showSyncMenu(options) {

        requirejs(["registrationservices"], function () {
            RegistrationServices.validateFeature('sync').then(function () {
                showSyncMenuInternal(options);
            });
        });
    }

    function showSyncMenuInternal(options) {

        require(['components/paperdialoghelper', 'paper-fab'], function (paperDialogHelper) {

            var userId = Dashboard.getCurrentUserId();

            var dialogOptionsQuery = {
                UserId: userId,
                ItemIds: (options.items || []).map(function (i) {
                    return i.Id || i;
                }).join(','),

                ParentId: options.ParentId,
                Category: options.Category
            };

            ApiClient.getJSON(ApiClient.getUrl('Sync/Options', dialogOptionsQuery)).then(function (dialogOptions) {

                currentDialogOptions = dialogOptions;

                var dlg = paperDialogHelper.createDialog({
                    size: 'small',
                    theme: 'a',
                    removeOnClose: true
                });

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<paper-fab icon="arrow-back" mini class="btnCancel"></paper-fab>';
                html += '</h2>';

                html += '<div>';

                html += '<form class="formSubmitSyncRequest" style="margin: auto;">';

                html += '<div style="margin:1em 0 1.5em;">';
                html += '<h1 style="margin: 0;display:inline-block;vertical-align:middle;">' + Globalize.translate('SyncMedia') + '</h1>';

                html += '<a href="https://github.com/MediaBrowser/Wiki/wiki/Sync" target="_blank" class="clearLink" style="margin-top:0;display:inline-block;vertical-align:middle;margin-left:1em;"><paper-button raised class="secondary mini"><iron-icon icon="info"></iron-icon><span>' + Globalize.translate('ButtonHelp') + '</span></paper-button></a>';
                html += '</div>';

                html += '<div class="formFields"></div>';

                html += '<p>';
                html += '<button type="submit" data-role="none" class="clearButton"><paper-button raised class="submit block"><iron-icon icon="sync"></iron-icon><span>' + Globalize.translate('ButtonSync') + '</span></paper-button></button>';
                html += '</p>';

                html += '</form>';
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                paperDialogHelper.open(dlg);

                $('form', dlg).on('submit', function () {

                    submitJob(dlg, userId, options, this);
                    return false;
                });

                $('.btnCancel', dlg).on('click', function () {
                    paperDialogHelper.close(dlg);
                });

                renderForm({
                    elem: $('.formFields', dlg),
                    dialogOptions: dialogOptions,
                    dialogOptionsFn: getTargetDialogOptionsFn(dialogOptionsQuery)
                });
            });

        });
    }

    function getTargetDialogOptionsFn(query) {

        return function (targetId) {

            query.TargetId = targetId;
            return ApiClient.getJSON(ApiClient.getUrl('Sync/Options', query));
        };
    }

    function onProfileChange(form, profileId) {

        var options = currentDialogOptions || {};
        var option = (options.ProfileOptions || []).filter(function (o) {
            return o.Id == profileId;
        })[0];

        if (option) {
            $('.profileDescription', form).html(option.Description || '');
            setQualityFieldVisible(form, options.QualityOptions.length > 0 && option.EnableQualityOptions && options.Options.indexOf('Quality') != -1);
        } else {
            $('.profileDescription', form).html('');
            setQualityFieldVisible(form, options.QualityOptions.length > 0 && options.Options.indexOf('Quality') != -1);
        }
    }

    function onQualityChange(form, qualityId) {

        var options = currentDialogOptions || {};
        var option = (options.QualityOptions || []).filter(function (o) {
            return o.Id == qualityId;
        })[0];

        if (option) {
            $('.qualityDescription', form).html(option.Description || '');
        } else {
            $('.qualityDescription', form).html('');
        }

        if (qualityId == 'custom') {
            $('.fldBitrate', form).show();
            $('#txtBitrate', form).attr('required', 'required');
        } else {
            $('.fldBitrate', form).hide();
            $('#txtBitrate', form).removeAttr('required').val('');
        }
    }

    function loadQualityOptions(form, targetId, dialogOptionsFn) {

        dialogOptionsFn(targetId).then(function (options) {

            renderTargetDialogOptions(form, options);
        });
    }

    function setQualityFieldVisible(form, visible) {

        if (visible) {
            $('.fldQuality', form).show();
            $('#selectQuality', form).attr('required', 'required');
        } else {
            $('.fldQuality', form).hide();
            $('#selectQuality', form).removeAttr('required');
        }
    }

    function renderTargetDialogOptions(form, options) {

        currentDialogOptions = options;

        if (options.ProfileOptions.length && options.Options.indexOf('Profile') != -1) {
            $('.fldProfile', form).show();
            $('#selectProfile', form).attr('required', 'required');
        } else {
            $('.fldProfile', form).hide();
            $('#selectProfile', form).removeAttr('required');
        }

        setQualityFieldVisible(options.QualityOptions.length > 0);

        $('#selectProfile', form).html(options.ProfileOptions.map(function (o) {

            var selectedAttribute = o.IsDefault ? ' selected="selected"' : '';
            return '<option value="' + o.Id + '"' + selectedAttribute + '>' + o.Name + '</option>';

        }).join('')).trigger('change');

        $('#selectQuality', form).html(options.QualityOptions.map(function (o) {

            var selectedAttribute = o.IsDefault ? ' selected="selected"' : '';
            return '<option value="' + o.Id + '"' + selectedAttribute + '>' + o.Name + '</option>';

        }).join('')).trigger('change');
    }

    function isAvailable(item, user) {

        if (AppInfo.isNativeApp && !Dashboard.capabilities().SupportsSync) {
            return false;
        }

        return item.SupportsSync;
    }

    window.SyncManager = {

        showMenu: showSyncMenu,
        isAvailable: isAvailable,
        renderForm: renderForm,
        setJobValues: setJobValues
    };

    function showSyncButtonsPerUser(page) {

        var apiClient = window.ApiClient;

        if (!apiClient || !apiClient.getCurrentUserId()) {
            return;
        }

        Dashboard.getCurrentUser().then(function (user) {

            var item = {
                SupportsSync: true
            };

            if (isAvailable(item)) {
                $('.categorySyncButton', page).removeClass('hide');
            } else {
                $('.categorySyncButton', page).addClass('hide');
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

    }).on('pageshow', ".libraryPage", function () {

        var page = this;

        if (!Dashboard.isServerlessPage()) {
            showSyncButtonsPerUser(page);
        }

    });


})(window, jQuery);