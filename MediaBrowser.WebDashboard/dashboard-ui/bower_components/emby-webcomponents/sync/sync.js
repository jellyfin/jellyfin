define(['apphost', 'globalize', 'connectionManager', 'layoutManager', 'shell', 'focusManager', 'scrollHelper', 'appSettings', 'paper-icon-button-light', 'formDialogStyle'], function (appHost, globalize, connectionManager, layoutManager, shell, focusManager, scrollHelper, appSettings) {

    var currentDialogOptions;

    function submitJob(dlg, apiClient, userId, syncOptions, form, dialogHelper) {

        if (!userId) {
            throw new Error('userId cannot be null');
        }

        if (!syncOptions) {
            throw new Error('syncOptions cannot be null');
        }

        if (!form) {
            throw new Error('form cannot be null');
        }

        var selectSyncTarget = form.querySelector('#selectSyncTarget');
        var target = selectSyncTarget ? selectSyncTarget.value : null;

        if (!target) {

            require(['toast'], function (toast) {
                toast(globalize.translate('sharedcomponents#PleaseSelectDeviceToSyncTo'));
            });
            return false;
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

        apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl("Sync/Jobs"),
            data: JSON.stringify(options),
            contentType: "application/json",
            dataType: 'json'

        }).then(function () {

            dialogHelper.close(dlg);
            require(['toast'], function (toast) {

                var msg = target == apiClient.deviceId() ? globalize.translate('sharedcomponents#DownloadScheduled') : globalize.translate('sharedcomponents#SyncJobCreated');

                toast(msg);
            });
        });

        return true;
    }

    function submitQuickSyncJob(apiClient, userId, targetId, syncOptions) {

        if (!userId) {
            throw new Error('userId cannot be null');
        }

        if (!syncOptions) {
            throw new Error('syncOptions cannot be null');
        }

        if (!targetId) {
            throw new Error('targetId cannot be null');
        }

        var options = {

            userId: userId,
            TargetId: targetId,

            ParentId: syncOptions.ParentId,
            Category: syncOptions.Category,
            Quality: syncOptions.Quality,
            Bitrate: syncOptions.Bitrate
        };

        if (syncOptions.items && syncOptions.items.length) {
            options.ItemIds = (syncOptions.items || []).map(function (i) {
                return i.Id || i;
            }).join(',');
        }

        return apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl("Sync/Jobs"),
            data: JSON.stringify(options),
            contentType: "application/json",
            dataType: 'json'

        }).then(function () {

            require(['toast'], function (toast) {

                var msg = targetId == apiClient.deviceId() ? globalize.translate('sharedcomponents#DownloadScheduled') : globalize.translate('sharedcomponents#SyncJobCreated');

                toast(msg);
            });
        });
    }

    function setJobValues(job, form) {

        var txtBitrate = form.querySelector('#txtBitrate');
        var bitrate = txtBitrate ? txtBitrate.value : null;

        if (bitrate) {
            bitrate = parseFloat(bitrate) * 1000000;
        }
        job.Bitrate = bitrate;

        var txtSyncJobName = form.querySelector('#txtSyncJobName');
        if (txtSyncJobName) {
            job.Name = txtSyncJobName.value;
        }

        var selectQuality = form.querySelector('#selectQuality');
        if (selectQuality) {
            job.Quality = selectQuality.value;
        }

        var selectProfile = form.querySelector('#selectProfile');
        if (selectProfile) {
            job.Profile = selectProfile.value;
        }

        var txtItemLimit = form.querySelector('#txtItemLimit');
        if (txtItemLimit) {
            job.ItemLimit = txtItemLimit.value || null;
        }

        var chkSyncNewContent = form.querySelector('#chkSyncNewContent');
        if (chkSyncNewContent) {
            job.SyncNewContent = chkSyncNewContent.checked;
        }

        var chkUnwatchedOnly = form.querySelector('#chkUnwatchedOnly');
        if (chkUnwatchedOnly) {
            job.UnwatchedOnly = chkUnwatchedOnly.checked;
        }
    }

    function renderForm(options) {

        return new Promise(function (resolve, reject) {

            require(['emby-checkbox', 'emby-input', 'emby-select'], function () {

                appHost.appInfo().then(function (appInfo) {
                    renderFormInternal(options, appInfo, resolve);
                });
            });
        });
    }

    function onHelpLinkClick(e) {

        shell.openUrl(this.href);

        e.preventDefault();
        return false;
    }

    function renderFormInternal(options, appInfo, resolve) {

        var elem = options.elem;
        var dialogOptions = options.dialogOptions;

        var targets = dialogOptions.Targets;

        var html = '';

        var targetContainerClass = options.isLocalSync ? ' hide' : '';

        if (options.showName || dialogOptions.Options.indexOf('Name') != -1) {

            html += '<div class="inputContainer' + targetContainerClass + '">';
            html += '<input is="emby-input" type="text" id="txtSyncJobName" class="txtSyncJobName" required="required" label="' + globalize.translate('sharedcomponents#LabelSyncJobName') + '"/>';
            html += '</div>';
        }

        if (options.readOnlySyncTarget) {
            html += '<div class="inputContainer' + targetContainerClass + '">';
            html += '<input is="emby-input" type="text" id="selectSyncTarget" readonly label="' + globalize.translate('sharedcomponents#LabelSyncTo') + '"/>';
            html += '</div>';
        } else {
            html += '<div class="selectContainer' + targetContainerClass + '">';
            html += '<select is="emby-select" id="selectSyncTarget" required="required" label="' + globalize.translate('sharedcomponents#LabelSyncTo') + '">';

            html += targets.map(function (t) {

                var isSelected = t.Id == appInfo.deviceId;
                var selectedHtml = isSelected ? ' selected="selected"' : '';
                return '<option' + selectedHtml + ' value="' + t.Id + '">' + t.Name + '</option>';

            }).join('');
            html += '</select>';
            if (!targets.length) {
                html += '<div class="fieldDescription">' + globalize.translate('sharedcomponents#LabelSyncNoTargetsHelp') + '</div>';
                html += '<div class="fieldDescription"><a class="lnkLearnMore" href="https://github.com/MediaBrowser/Wiki/wiki/Sync" target="_blank">' + globalize.translate('sharedcomponents#LearnMore') + '</a></div>';
            }
            html += '</div>';
        }

        html += '<div class="fldProfile selectContainer hide">';
        html += '<select is="emby-select" id="selectProfile" label="' + globalize.translate('sharedcomponents#LabelProfile') + '">';
        html += '</select>';
        html += '<div class="fieldDescription profileDescription"></div>';
        html += '</div>';

        html += '<div class="fldQuality selectContainer hide">';
        html += '<select is="emby-select" id="selectQuality" data-mini="true" required="required" label="' + globalize.translate('sharedcomponents#LabelQuality') + '">';
        html += '</select>';
        html += '<div class="fieldDescription qualityDescription"></div>';
        html += '</div>';

        html += '<div class="fldBitrate inputContainer hide">';
        html += '<input is="emby-input" type="number" step=".1" min=".1" id="txtBitrate" label="' + globalize.translate('sharedcomponents#LabelBitrateMbps') + '"/>';
        html += '</div>';

        if (dialogOptions.Options.indexOf('UnwatchedOnly') != -1) {
            html += '<div class="checkboxContainer checkboxContainer-withDescription">';
            html += '<label>';
            html += '<input is="emby-checkbox" type="checkbox" id="chkUnwatchedOnly"/>';
            html += '<span>' + globalize.translate('sharedcomponents#SyncUnwatchedVideosOnly') + '</span>';
            html += '</label>';
            html += '<div class="fieldDescription checkboxFieldDescription">' + globalize.translate('sharedcomponents#SyncUnwatchedVideosOnlyHelp') + '</div>';
            html += '</div>';
        }

        if (dialogOptions.Options.indexOf('SyncNewContent') != -1) {
            html += '<div class="checkboxContainer checkboxContainer-withDescription">';
            html += '<label>';
            html += '<input is="emby-checkbox" type="checkbox" id="chkSyncNewContent"/>';
            html += '<span>' + globalize.translate('sharedcomponents#AutomaticallySyncNewContent') + '</span>';
            html += '</label>';
            html += '<div class="fieldDescription checkboxFieldDescription">' + globalize.translate('sharedcomponents#AutomaticallySyncNewContentHelp') + '</div>';
            html += '</div>';
        }

        if (dialogOptions.Options.indexOf('ItemLimit') != -1) {
            html += '<div class="inputContainer">';
            html += '<input is="emby-input" type="number" step="1" min="1" id="txtItemLimit" label="' + globalize.translate('sharedcomponents#LabelItemLimit') + '"/>';
            html += '<div class="fieldDescription">' + globalize.translate('sharedcomponents#LabelItemLimitHelp') + '</div>';
            html += '</div>';
        }

        //html += '</div>';
        //html += '</div>';

        elem.innerHTML = html;

        var selectSyncTarget = elem.querySelector('#selectSyncTarget');
        if (selectSyncTarget) {
            selectSyncTarget.addEventListener('change', function () {
                loadQualityOptions(elem, this.value, options.dialogOptionsFn).then(resolve);
            });
            selectSyncTarget.dispatchEvent(new CustomEvent('change', {
                bubbles: true
            }));
        }

        var selectProfile = elem.querySelector('#selectProfile');
        if (selectProfile) {
            selectProfile.addEventListener('change', function () {
                onProfileChange(elem, this.value);
            });
            selectProfile.dispatchEvent(new CustomEvent('change', {
                bubbles: true
            }));
        }

        var selectQuality = elem.querySelector('#selectQuality');
        if (selectQuality) {
            selectQuality.addEventListener('change', function () {
                onQualityChange(elem, this.value);
            });
            selectQuality.dispatchEvent(new CustomEvent('change', {
                bubbles: true
            }));
        }

        var lnkLearnMore = elem.querySelector('.lnkLearnMore');
        if (lnkLearnMore) {
            lnkLearnMore.addEventListener('click', onHelpLinkClick);
        }

        // This isn't ideal, but allow time for the change handlers above to run
        setTimeout(function () {
            focusManager.autoFocus(elem);
        }, 100);
    }

    function showSyncMenu(options) {

        return new Promise(function (resolve, reject) {

            require(["registrationservices", 'dialogHelper', 'formDialogStyle'], function (registrationServices, dialogHelper) {
                registrationServices.validateFeature('sync').then(function () {

                    showSyncMenuInternal(dialogHelper, options).then(resolve, reject);

                }, reject);
            });
        });
    }

    function enableAutoSync(options) {

        if (!options.isLocalSync) {
            return false;
        }

        var firstItem = (options.items || [])[0] || {};

        if (firstItem.Type == 'Audio') {
            return true;
        }
        if (firstItem.Type == 'MusicAlbum') {
            return true;
        }
        if (firstItem.Type == 'MusicArtist') {
            return true;
        }
        if (firstItem.Type == 'MusicGenre') {
            return true;
        }

        return false;
    }

    function showSyncMenuInternal(dialogHelper, options) {

        var apiClient = connectionManager.getApiClient(options.serverId);
        var userId = apiClient.getCurrentUserId();

        if (enableAutoSync(options)) {

            return submitQuickSyncJob(apiClient, userId, apiClient.deviceId(), {
                items: options.items,
                Quality: 'custom',
                Bitrate: appSettings.maxStaticMusicBitrate()
            });
        }

        var dialogOptionsQuery = {
            UserId: userId,
            ItemIds: (options.items || []).map(function (i) {
                return i.Id || i;
            }).join(','),

            ParentId: options.ParentId,
            Category: options.Category
        };

        return apiClient.getJSON(apiClient.getUrl('Sync/Options', dialogOptionsQuery)).then(function (dialogOptions) {

            currentDialogOptions = dialogOptions;

            var dlgElementOptions = {
                removeOnClose: true,
                scrollY: false,
                autoFocus: false
            };

            if (layoutManager.tv) {
                dlgElementOptions.size = 'fullscreen';
            } else {
                dlgElementOptions.size = 'small';
            }

            var dlg = dialogHelper.createDialog(dlgElementOptions);

            dlg.classList.add('formDialog');

            var html = '';
            html += '<div class="formDialogHeader">';
            html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
            html += '<div class="formDialogHeaderTitle">';
            html += globalize.translate('sharedcomponents#Sync');
            html += '</div>';

            html += '<a href="https://github.com/MediaBrowser/Wiki/wiki/Sync" target="_blank" class="clearLink lnkHelp" style="margin-top:0;display:inline-block;vertical-align:middle;margin-left:auto;"><button is="emby-button" type="button" class="mini"><i class="md-icon">info</i><span>' + globalize.translate('sharedcomponents#Help') + '</span></button></a>';

            html += '</div>';

            html += '<div class="formDialogContent smoothScrollY" style="padding-top:2em;">';
            html += '<div class="dialogContentInner dialog-content-centered">';

            html += '<form class="formSubmitSyncRequest" style="margin: auto;">';

            html += '<div class="formFields"></div>';

            html += '<p>';
            html += '<button is="emby-button" type="submit" class="raised submit block"><i class="md-icon">sync</i><span>' + globalize.translate('sharedcomponents#Sync') + '</span></button>';
            html += '</p>';

            html += '</form>';

            html += '</div>';
            html += '</div>';

            dlg.innerHTML = html;

            dlg.querySelector('.lnkHelp').addEventListener('click', onHelpLinkClick);

            document.body.appendChild(dlg);
            var submitted = false;

            dlg.querySelector('form').addEventListener('submit', function (e) {

                submitted = submitJob(dlg, apiClient, userId, options, this, dialogHelper);

                e.preventDefault();
                return false;
            });

            dlg.querySelector('.btnCancel').addEventListener('click', function () {
                dialogHelper.close(dlg);
            });

            if (layoutManager.tv) {
                scrollHelper.centerFocus.on(dlg.querySelector('.formDialogContent'), false);
            }

            var promise = dialogHelper.open(dlg);

            renderForm({
                elem: dlg.querySelector('.formFields'),
                dialogOptions: dialogOptions,
                dialogOptionsFn: getTargetDialogOptionsFn(apiClient, dialogOptionsQuery),
                isLocalSync: options.isLocalSync
            });

            return promise.then(function () {
                if (submitted) {
                    return Promise.resolve();
                }
                return Promise.reject();
            });
        });
    }

    function getTargetDialogOptionsFn(apiClient, query) {

        return function (targetId) {

            query.TargetId = targetId;
            return apiClient.getJSON(apiClient.getUrl('Sync/Options', query));
        };
    }

    function setQualityFieldVisible(form, visible) {

        var fldQuality = form.querySelector('.fldQuality');
        var selectQuality = form.querySelector('#selectQuality');

        if (visible) {
            if (fldQuality) {
                fldQuality.classList.remove('hide');
            }
            if (selectQuality) {
                selectQuality.setAttribute('required', 'required');
            }
        } else {
            if (fldQuality) {
                fldQuality.classList.add('hide');
            }
            if (selectQuality) {
                selectQuality.removeAttribute('required');
            }
        }
    }

    function onProfileChange(form, profileId) {

        var options = currentDialogOptions || {};
        var option = (options.ProfileOptions || []).filter(function (o) {
            return o.Id == profileId;
        })[0];

        var qualityOptions = options.QualityOptions || [];

        if (option) {
            form.querySelector('.profileDescription').innerHTML = option.Description || '';
            setQualityFieldVisible(form, qualityOptions.length > 0 && option.EnableQualityOptions && options.Options.indexOf('Quality') != -1);
        } else {
            form.querySelector('.profileDescription').innerHTML = '';
            setQualityFieldVisible(form, qualityOptions.length > 0 && options.Options.indexOf('Quality') != -1);
        }
    }

    function onQualityChange(form, qualityId) {

        var options = currentDialogOptions || {};
        var option = (options.QualityOptions || []).filter(function (o) {
            return o.Id == qualityId;
        })[0];

        var qualityDescription = form.querySelector('.qualityDescription');

        if (option) {
            qualityDescription.innerHTML = option.Description || '';
        } else {
            qualityDescription.innerHTML = '';
        }

        var fldBitrate = form.querySelector('.fldBitrate');
        var txtBitrate = form.querySelector('#txtBitrate');

        if (qualityId == 'custom') {

            if (fldBitrate) {
                fldBitrate.classList.remove('hide');
            }
            if (txtBitrate) {
                txtBitrate.setAttribute('required', 'required');
            }
        } else {
            if (fldBitrate) {
                fldBitrate.classList.add('hide');
            }
            if (txtBitrate) {
                txtBitrate.removeAttribute('required');
            }
        }
    }

    function renderTargetDialogOptions(form, options) {

        currentDialogOptions = options;

        var fldProfile = form.querySelector('.fldProfile');
        var selectProfile = form.querySelector('#selectProfile');

        if (options.ProfileOptions.length && options.Options.indexOf('Profile') != -1) {
            if (fldProfile) {
                fldProfile.classList.remove('hide');
            }
            if (selectProfile) {
                selectProfile.setAttribute('required', 'required');
            }
        } else {
            if (fldProfile) {
                fldProfile.classList.add('hide');
            }
            if (selectProfile) {
                selectProfile.removeAttribute('required');
            }
        }

        setQualityFieldVisible(form, options.QualityOptions.length > 0);

        if (selectProfile) {
            selectProfile.innerHTML = options.ProfileOptions.map(function (o) {

                var selectedAttribute = o.IsDefault ? ' selected="selected"' : '';
                return '<option value="' + o.Id + '"' + selectedAttribute + '>' + o.Name + '</option>';

            }).join('');

            selectProfile.dispatchEvent(new CustomEvent('change', {
                bubbles: true
            }));
        }

        var selectQuality = form.querySelector('#selectQuality');
        if (selectQuality) {
            selectQuality.innerHTML = options.QualityOptions.map(function (o) {

                var selectedAttribute = o.IsDefault ? ' selected="selected"' : '';
                return '<option value="' + o.Id + '"' + selectedAttribute + '>' + o.Name + '</option>';

            }).join('');

            selectQuality.dispatchEvent(new CustomEvent('change', {
                bubbles: true
            }));
        }
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