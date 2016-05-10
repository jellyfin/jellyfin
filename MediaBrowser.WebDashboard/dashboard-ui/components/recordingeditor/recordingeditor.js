define(['dialogHelper', 'loading', 'jQuery', 'paper-checkbox', 'paper-input', 'emby-collapsible', 'paper-button', 'paper-icon-button-light'], function (dialogHelper, loading, $) {

    var currentDialog;
    var recordingUpdated = false;
    var currentItemId;

    function renderTimer(context, item) {

        var programInfo = item.ProgramInfo || {};

        $('.itemName', context).html(item.Name);

        $('.itemEpisodeName', context).html(programInfo.EpisodeTitle || '');

        $('.itemCommunityRating', context).html(LibraryBrowser.getRatingHtml(programInfo));

        LibraryBrowser.renderGenres($('.itemGenres', context), programInfo);
        LibraryBrowser.renderOverview(context.querySelectorAll('.itemOverview'), programInfo);

        if (programInfo.ImageTags && programInfo.ImageTags.Primary) {

            var imgUrl = ApiClient.getScaledImageUrl(programInfo.Id, {
                maxWidth: 200,
                maxHeight: 200,
                tag: programInfo.ImageTags.Primary,
                type: "Primary"
            });

            $('.timerPageImageContainer', context).css("display", "inline-block")
                .html('<img src="' + imgUrl + '" style="max-width:200px;max-height:200px;" />');

        } else {
            $('.timerPageImageContainer', context).hide();
        }

        $('.itemMiscInfo', context).html(LibraryBrowser.getMiscInfoHtml(programInfo));

        $('#txtPrePaddingMinutes', context).val(item.PrePaddingSeconds / 60);
        $('#txtPostPaddingMinutes', context).val(item.PostPaddingSeconds / 60);

        if (item.Status == 'New') {
            $('.timerStatus', context).hide();
        } else {
            $('.timerStatus', context).show().html('Status:&nbsp;&nbsp;&nbsp;' + item.Status);
        }

        loading.hide();
    }

    function closeDialog(isSubmitted) {

        recordingUpdated = isSubmitted;
        dialogHelper.close(currentDialog);
    }

    function onSubmit(e) {

        loading.show();

        var form = this;

        ApiClient.getLiveTvTimer(currentItemId).then(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingMinutes', form).val() * 60;
            item.PostPaddingSeconds = $('#txtPostPaddingMinutes', form).val() * 60;
            ApiClient.updateLiveTvTimer(item).then(function () {
                loading.hide();
                require(['toast'], function (toast) {
                    toast(Globalize.translate('MessageRecordingSaved'));
                    closeDialog(true);
                });
            });
        });

        e.preventDefault();

        // Disable default form submission
        return false;
    }

    function init(context) {

        context.querySelector('.btnCancel').addEventListener('click', function () {

            closeDialog(false);
        });

        context.querySelector('form').addEventListener('submit', onSubmit);

        context.querySelector('.btnHeaderSave').addEventListener('click', function (e) {

            context.querySelector('.btnSave').click();
        });
    }

    function reload(context, id) {

        loading.show();
        currentItemId = id;

        ApiClient.getLiveTvTimer(id).then(function (result) {

            renderTimer(context, result);
            loading.hide();
        });
    }

    function showEditor(itemId) {

        return new Promise(function (resolve, reject) {

            recordingUpdated = false;
            loading.show();

            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'components/recordingeditor/recordingeditor.template.html', true);

            xhr.onload = function (e) {

                var template = this.response;
                var dlg = dialogHelper.createDialog({
                    removeOnClose: true,
                    size: 'small'
                });

                dlg.classList.add('ui-body-b');
                dlg.classList.add('background-theme-b');

                dlg.classList.add('formDialog');

                var html = '';

                html += Globalize.translateDocument(template);

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                dialogHelper.open(dlg);

                currentDialog = dlg;

                dlg.addEventListener('close', function () {

                    if (recordingUpdated) {
                        resolve();
                    } else {
                        reject();
                    }
                });

                init(dlg);

                reload(dlg, itemId);
            }

            xhr.send();
        });
    }

    return {
        show: showEditor
    };
});