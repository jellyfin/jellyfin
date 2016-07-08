define(['apphost', 'jQuery', 'paper-icon-button-light'], function (appHost, $) {

    var currentDialogOptions;

    function submitJob(dlg, userId, syncOptions, form, dialogHelper) {

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

            require(['toast'], function (toast) {
                toast(Globalize.translate('MessagePleaseSelectDeviceToSyncTo'));
            });
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

            dialogHelper.close(dlg);
            require(['toast'], function (toast) {
                toast(Globalize.translate('MessageSyncJobCreated'));
            });
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

            require(['emby-checkbox', 'emby-input', 'emby-collapse'], function () {

                appHost.appInfo().then(function (appInfo) {
                    renderFormInternal(options, appInfo, resolve);
                });
            });
        });
    }

    function renderFormInternal(options, appInfo, resolve) {

        var elem = options.elem;
        var dialogOptions = options.dialogOptions;

        var targets = dialogOptions.Targets;

        var html = '';

        if (options.showName || dialogOptions.Options.indexOf('Name') != -1) {

            html += '<div class="inputContainer">';
            html += '<input is="emby-input" type="text" id="txtSyncJobName" class="txtSyncJobName" required="required" label="' + Globalize.translate('LabelSyncJobName') + '"/>';
            html += '</div>';
            html += '<br/>';
        }

        html += '<div>';
        if (options.readOnlySyncTarget) {
            html += '<div class="inputContainer">';
            html += '<input is="emby-input" type="text" id="selectSyncTarget" readonly label="' + Globalize.translate('LabelSyncTo') + '"/>';
            html += '</div>';
        } else {
            html += '<label for="selectSyncTarget" class="selectLabel">' + Globalize.translate('LabelSyncTo') + '</label>';
            html += '<select id="selectSyncTarget" required="required" data-mini="true">';

            html += targets.map(function (t) {

                var isSelected = t.Id == appInfo.deviceId;
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
        html += '<label for="selectProfile" class="selectLabel">' + Globalize.translate('LabelProfile') + '</label>';
        html += '<select id="selectProfile" data-mini="true">';
        html += '</select>';
        html += '<div class="fieldDescription profileDescription"></div>';
        html += '</div>';

        html += '<div class="fldQuality" style="display:none;">';
        html += '<br/>';
        html += '<label for="selectQuality" class="selectLabel">' + Globalize.translate('LabelQuality') + '</label>';
        html += '<select id="selectQuality" data-mini="true" required="required">';
        html += '</select>';
        html += '<div class="fieldDescription qualityDescription"></div>';
        html += '</div>';

        html += '<div class="fldBitrate" style="display:none;">';
        html += '<br/>';
        html += '<div class="inputContainer">';
        html += '<input is="emby-input" type="number" step=".1" min=".1" id="txtBitrate" label="' + Globalize.translate('LabelBitrateMbps') + '"/>';
        html += '</div>';
        html += '</div>';

        if (dialogOptions.Options.indexOf('UnwatchedOnly') != -1) {
            html += '<br/>';
            html += '<div class="checkboxContainer">';
            html += '<label>';
            html += '<input is="emby-checkbox" type="checkbox" id="chkUnwatchedOnly"/>';
            html += '<span>' + Globalize.translate('OptionSyncUnwatchedVideosOnly') + '</span>';
            html += '</label>';
            html += '<div class="fieldDescription checkboxFieldDescription">' + Globalize.translate('OptionSyncUnwatchedVideosOnlyHelp') + '</div>';
            html += '</div>';
        }

        if (dialogOptions.Options.indexOf('SyncNewContent') != -1 ||
            dialogOptions.Options.indexOf('ItemLimit') != -1) {

            html += '<div is="emby-collapse" title="' + Globalize.translate('HeaderAdvanced') + '">';
            html += '<div class="collapseContent">';
            if (dialogOptions.Options.indexOf('SyncNewContent') != -1) {
                html += '<br/>';
                html += '<div class="checkboxContainer">';
                html += '<label>';
                html += '<input is="emby-checkbox" type="checkbox" id="chkSyncNewContent"/>';
                html += '<span>' + Globalize.translate('OptionAutomaticallySyncNewContent') + '</span>';
                html += '</label>';
                html += '<div class="fieldDescription checkboxFieldDescription">' + Globalize.translate('OptionAutomaticallySyncNewContentHelp') + '</div>';
                html += '</div>';
            }

            if (dialogOptions.Options.indexOf('ItemLimit') != -1) {
                html += '<div class="inputContainer">';
                html += '<input is="emby-input" type="number" step="1" min="1" id="txtItemLimit" label="' + Globalize.translate('LabelItemLimit') + '"/>';
                html += '<div class="fieldDescription">' + Globalize.translate('LabelItemLimitHelp') + '</div>';
                html += '</div>';
            }
            html += '</div>';
            html += '</div>';
            html += '<br/>';
        }

        //html += '</div>';
        //html += '</div>';

        $(elem).html(html);

        $('#selectSyncTarget', elem).on('change', function () {

            loadQualityOptions(elem, this.value, options.dialogOptionsFn).then(resolve);

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

        require(['dialogHelper'], function (dialogHelper) {

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

                var dlg = dialogHelper.createDialog({
                    size: 'small',
                    removeOnClose: true,
                    autoFocus: false
                });

                dlg.classList.add('ui-body-a');
                dlg.classList.add('background-theme-a');
                dlg.classList.add('popupEditor');

                var html = '';
                html += '<div class="dialogHeader" style="margin:0 0 2em;">';
                html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
                html += '<div class="dialogHeaderTitle">';
                html += Globalize.translate('SyncMedia');
                html += '</div>';

                html += '<a href="https://github.com/MediaBrowser/Wiki/wiki/Sync" target="_blank" class="clearLink" style="margin-top:0;display:inline-block;vertical-align:middle;margin-left:auto;"><button is="emby-button" type="button" class="mini"><i class="md-icon">info</i><span>' + Globalize.translate('ButtonHelp') + '</span></button></a>';

                html += '</div>';

                html += '<form class="formSubmitSyncRequest" style="margin: auto;">';

                html += '<div class="formFields"></div>';

                html += '<p>';
                html += '<button is="emby-button" type="submit" class="raised submit block"><i class="md-icon">sync</i><span>' + Globalize.translate('ButtonSync') + '</span></button>';
                html += '</p>';

                html += '</form>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                dialogHelper.open(dlg);

                $('form', dlg).on('submit', function () {

                    submitJob(dlg, userId, options, this, dialogHelper);
                    return false;
                });

                $('.btnCancel', dlg).on('click', function () {
                    dialogHelper.close(dlg);
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

    function setQualityFieldVisible(form, visible) {

        if (visible) {
            $('.fldQuality', form).show();
            $('#selectQuality', form).attr('required', 'required');
        } else {
            $('.fldQuality', form).hide();
            $('#selectQuality', form).removeAttr('required');
        }
    }

    function onProfileChange(form, profileId) {

        var options = currentDialogOptions || {};
        var option = (options.ProfileOptions || []).filter(function (o) {
            return o.Id == profileId;
        })[0];

        var qualityOptions = options.QualityOptions || [];

        if (option) {
            $('.profileDescription', form).html(option.Description || '');
            setQualityFieldVisible(form, qualityOptions.length > 0 && option.EnableQualityOptions && options.Options.indexOf('Quality') != -1);
        } else {
            $('.profileDescription', form).html('');
            setQualityFieldVisible(form, qualityOptions.length > 0 && options.Options.indexOf('Quality') != -1);
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

    function loadQualityOptions(form, targetId, dialogOptionsFn) {

        return dialogOptionsFn(targetId).then(function (options) {

            return renderTargetDialogOptions(form, options);
        });
    }

    return {

        showMenu: showSyncMenu,
        renderForm: renderForm,
        setJobValues: setJobValues
    };
});