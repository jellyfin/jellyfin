define(['dialogHelper', 'loading', 'connectionManager', 'globalize', 'actionsheet', 'paper-checkbox', 'emby-input', 'paper-icon-button-light', 'emby-button', 'listViewStyle', 'material-icons', 'formDialogStyle'],
function (dialogHelper, loading, connectionManager, globalize, actionsheet) {

    return function (options) {

        var self = this;

        var currentMappingOptions;

        function parentWithClass(elem, className) {

            while (!elem.classList || !elem.classList.contains(className)) {
                elem = elem.parentNode;

                if (!elem) {
                    return null;
                }
            }

            return elem;
        }

        function mapChannel(button, tunerChannelNumber, providerChannelNumber) {

            loading.show();

            var providerId = options.providerId;
            var apiClient = connectionManager.getApiClient(options.serverId);

            apiClient.ajax({
                type: 'POST',
                url: ApiClient.getUrl('LiveTv/ChannelMappings'),
                data: {
                    providerId: providerId,
                    tunerChannelNumber: tunerChannelNumber,
                    providerChannelNumber: providerChannelNumber
                },
                dataType: 'json'

            }).then(function (mapping) {

                var listItem = parentWithClass(button, 'listItem');

                button.setAttribute('data-providernumber', mapping.ProviderChannelNumber);
                listItem.querySelector('.secondary').innerHTML = getMappingSecondaryName(mapping, currentMappingOptions.ProviderName);
                loading.hide();
            });
        }

        function onChannelsElementClick(e) {

            var btnMap = parentWithClass(e.target, 'btnMap');

            if (!btnMap) {
                return;
            }

            var tunerChannelNumber = btnMap.getAttribute('data-number');
            var providerChannelNumber = btnMap.getAttribute('data-providernumber');

            var menuItems = currentMappingOptions.ProviderChannels.map(function (m) {

                return {
                    name: m.Name,
                    id: m.Id,
                    selected: m.Id.toLowerCase() == providerChannelNumber.toLowerCase()
                };
            });

            actionsheet.show({
                positionTo: btnMap,
                items: menuItems

            }).then(function (newChannelNumber) {
                mapChannel(btnMap, tunerChannelNumber, newChannelNumber);
            });
        }

        function getChannelMappingOptions(serverId, providerId) {

            var apiClient = connectionManager.getApiClient(serverId);
            return apiClient.getJSON(apiClient.getUrl('LiveTv/ChannelMappingOptions', {
                providerId: providerId
            }));
        }

        function getMappingSecondaryName(mapping, providerName) {

            return (mapping.ProviderChannelNumber || '') + ' ' + (mapping.ProviderChannelName || '') + ' - ' + providerName;
        }

        function getTunerChannelHtml(channel, providerName) {

            var html = '';

            html += '<div class="listItem">';

            html += '<i class="md-icon listItemIcon">dvr</i>';

            html += '<div class="listItemBody two-line">';
            html += '<h3 class="listItemBodyText">';
            html += channel.Name;
            html += '</h3>';

            html += '<div class="secondary listItemBodyText">';
            if (channel.ProviderChannelNumber || channel.ProviderChannelName) {
                html += getMappingSecondaryName(channel, providerName);
            }
            html += '</div>';

            html += '</div>';

            html += '<button class="btnMap autoSize" is="paper-icon-button-light" type="button" data-number="' + channel.Number + '" data-providernumber="' + channel.ProviderChannelNumber + '"><i class="md-icon">mode_edit</i></button>';

            html += '</div>';

            return html;
        }

        function getEditorHtml() {

            var html = '';

            html += '<div class="formDialogContent">';
            html += '<div class="dialogContentInner dialog-content-centered">';
            html += '<form style="margin:auto;">';

            html += '<h1>' + globalize.translate('HeaderChannels') + '</h1>';

            html += '<div class="channels paperList">';
            html += '</div>';

            html += '</form>';
            html += '</div>';
            html += '</div>';

            return html;
        }

        function initEditor(dlg, options) {

            getChannelMappingOptions(options.serverId, options.providerId).then(function (result) {

                currentMappingOptions = result;

                var channelsElement = dlg.querySelector('.channels');

                channelsElement.innerHTML = result.TunerChannels.map(function (channel) {
                    return getTunerChannelHtml(channel, result.ProviderName);
                }).join('');

                channelsElement.addEventListener('click', onChannelsElementClick);
            });
        }

        self.show = function () {

            var dialogOptions = {
                removeOnClose: true
            };

            dialogOptions.size = 'small';

            var dlg = dialogHelper.createDialog(dialogOptions);

            dlg.classList.add('formDialog');
            dlg.classList.add('ui-body-a');
            dlg.classList.add('background-theme-a');

            var html = '';
            var title = globalize.translate('MapChannels');

            html += '<div class="formDialogHeader">';
            html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
            html += '<div class="formDialogHeaderTitle">';
            html += title;
            html += '</div>';

            html += '</div>';

            html += getEditorHtml();

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            initEditor(dlg, options);

            dlg.querySelector('.btnCancel').addEventListener('click', function () {

                dialogHelper.close(dlg);
            });

            return new Promise(function (resolve, reject) {

                dlg.addEventListener('close', resolve);
                dialogHelper.open(dlg);
            });
        };
    };
});