define(['dialogHelper', 'loading', 'connectionManager', 'globalize', 'actionsheet', 'paper-checkbox', 'emby-input', 'paper-icon-button-light', 'emby-select', 'emby-button', 'listViewStyle', 'material-icons'],
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

        function mapChannel(tunerChannelNumber, providerChannelNumber) {

            alert('coming soon.');
        }

        function onChannelsElementClick(e) {

            var btnMap = parentWithClass(e.target, 'btnMap');

            if (!btnMap) {
                return;
            }

            var channelNumber = btnMap.getAttribute('data-number');

            var menuItems = currentMappingOptions.ProviderChannels.map(function (m) {

                return {
                    name: m.Name,
                    id: m.Id,
                    selected: m.Id == channelNumber
                };
            });

            actionsheet.show({
                positionTo: btnMap,
                items: menuItems

            }).then(function (newChannelNumber) {
                mapChannel(channelNumber, newChannelNumber);
            });
        }

        function getChannelMappingOptions(serverId, providerId) {

            return connectionManager.getApiClient(serverId).getJSON(ApiClient.getUrl('LiveTv/ChannelMappingOptions', {
                providerId: providerId
            }));
        }

        function getTunerChannelHtml(channel, providerName) {

            var html = '';

            html += '<div class="listItem">';

            html += '<button is="emby-button" type="button" class="fab listItemIcon mini" style="background:#52B54B;"><iron-icon icon="dvr"></iron-icon></button>';

            html += '<div class="listItemBody">';
            html += '<h3>';
            html += channel.Name;
            html += '</h3>';

            if (channel.ProviderChannelNumber || channel.ProviderChannelName) {
                html += '<div class="secondary">';
                html += (channel.ProviderChannelNumber || '') + ' ' + (channel.ProviderChannelName || '') + ' - ' + providerName;
                html += '</div>';
            }

            html += '</div>';

            html += '<button class="btnMap" is="paper-icon-button-light" type="button" data-number="' + channel.Number + '"><iron-icon icon="mode-edit"></iron-icon></button>';

            html += '</div>';

            return html;
        }

        function getEditorHtml() {

            var html = '';

            html += '<div class="dialogContent">';
            html += '<div class="dialogContentInner centeredContent">';
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

            html += '<div class="dialogHeader" style="margin:0 0 2em;">';
            html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">arrow_back</i></button>';
            html += '<div class="dialogHeaderTitle">';
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