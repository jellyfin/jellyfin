(function ($, document, window) {

    var currentProfile;

    var currentSubProfile;
    var isSubProfileNew;

    var allText = Globalize.translate('LabelAll');

    function loadProfile(page) {

        Dashboard.showLoadingMsg();

        var promise1 = getProfile();
        var promise2 = ApiClient.getUsers();

        $.when(promise1, promise2).done(function (response1, response2) {

            currentProfile = response1[0];

            renderProfile(page, currentProfile, response2[0]);

            Dashboard.hideLoadingMsg();

        });
    }

    function getProfile() {

        var id = getParameterByName('id');
        var url = id ? 'Dlna/Profiles/' + id :
            'Dlna/Profiles/Default';

        return ApiClient.getJSON(ApiClient.getUrl(url));
    }

    function renderProfile(page, profile, users) {

        $('#txtName', page).val(profile.Name);

        $('.chkMediaType', page).each(function () {
            this.checked = (profile.SupportedMediaTypes || '').split(',').indexOf(this.getAttribute('data-value')) != -1;

        }).checkboxradio('refresh');

        $('#chkEnableAlbumArtInDidl', page).checked(profile.EnableAlbumArtInDidl).checkboxradio('refresh');

        var idInfo = profile.Identification || {};

        $('#txtInfoFriendlyName', page).val(profile.FriendlyName || '');
        $('#txtInfoModelName', page).val(profile.ModelName || '');
        $('#txtInfoModelNumber', page).val(profile.ModelNumber || '');
        $('#txtInfoModelDescription', page).val(profile.ModelDescription || '');
        $('#txtInfoModelUrl', page).val(profile.ModelUrl || '');
        $('#txtInfoManufacturer', page).val(profile.Manufacturer || '');
        $('#txtInfoManufacturerUrl', page).val(profile.ManufacturerUrl || '');
        $('#txtInfoSerialNumber', page).val(profile.SerialNumber || '');

        $('#txtIdFriendlyName', page).val(idInfo.FriendlyName || '');
        $('#txtIdModelName', page).val(idInfo.ModelName || '');
        $('#txtIdModelNumber', page).val(idInfo.ModelNumber || '');
        $('#txtIdModelDescription', page).val(idInfo.ModelDescription || '');
        $('#txtIdModelUrl', page).val(idInfo.ModelUrl || '');
        $('#txtIdManufacturer', page).val(idInfo.Manufacturer || '');
        $('#txtIdManufacturerUrl', page).val(idInfo.ManufacturerUrl || '');
        $('#txtIdSerialNumber', page).val(idInfo.SerialNumber || '');
        $('#txtIdDeviceDescription', page).val(idInfo.DeviceDescription || '');

        $('#txtAlbumArtPn', page).val(profile.AlbumArtPn || '');
        $('#txtAlbumArtMaxWidth', page).val(profile.MaxAlbumArtWidth || '');
        $('#txtAlbumArtMaxHeight', page).val(profile.MaxAlbumArtHeight || '');
        $('#txtIconMaxWidth', page).val(profile.MaxIconWidth || '');
        $('#txtIconMaxHeight', page).val(profile.MaxIconHeight || '');

        $('#chkIgnoreTranscodeByteRangeRequests', page).checked(profile.IgnoreTranscodeByteRangeRequests).checkboxradio('refresh');
        $('#txtMaxAllowedBitrate', page).val(profile.MaxBitrate || '');

        $('#chkRequiresPlainFolders', page).checked(profile.RequiresPlainFolders).checkboxradio('refresh');
        $('#chkRequiresPlainVideoItems', page).checked(profile.RequiresPlainVideoItems).checkboxradio('refresh');

        $('#txtProtocolInfo', page).val(profile.ProtocolInfo || '');
        $('#txtXDlnaCap', page).val(profile.XDlnaCap || '');
        $('#txtXDlnaDoc', page).val(profile.XDlnaDoc || '');
        $('#txtSonyAggregationFlags', page).val(profile.SonyAggregationFlags || '');

        profile.DirectPlayProfiles = (profile.DirectPlayProfiles || []);
        profile.TranscodingProfiles = (profile.TranscodingProfiles || []);
        profile.ContainerProfiles = (profile.ContainerProfiles || []);
        profile.CodecProfiles = (profile.CodecProfiles || []);
        profile.ResponseProfiles = (profile.ResponseProfiles || []);

        var usersHtml = '<option></option>' + users.map(function (u) {
            return '<option value="' + u.Id + '">' + u.Name + '</option>';
        }).join('');
        $('#selectUser', page).html(usersHtml).val(profile.UserId || '').selectmenu("refresh");

        renderSubProfiles(page, profile);
    }

    function renderSubProfiles(page, profile) {

        renderDirectPlayProfiles(page, profile.DirectPlayProfiles);
        renderTranscodingProfiles(page, profile.TranscodingProfiles);
        renderContainerProfiles(page, profile.ContainerProfiles);
        renderCodecProfiles(page, profile.CodecProfiles);
        renderResponseProfiles(page, profile.ResponseProfiles);
    }

    function saveDirectPlayProfile(page) {

        currentSubProfile.Type = $('#selectDirectPlayProfileType', page).val();
        currentSubProfile.Container = $('#txtDirectPlayContainer', page).val();
        currentSubProfile.AudioCodec = $('#txtDirectPlayAudioCodec', page).val();
        currentSubProfile.VideoCodec = $('#txtDirectPlayVideoCodec', page).val();

        if (isSubProfileNew) {

            currentProfile.DirectPlayProfiles.push(currentSubProfile);
        }

        renderSubProfiles(page, currentProfile);

        currentSubProfile = null;

        $('#popupEditDirectPlayProfile', page).popup('close');
    }

    function renderDirectPlayProfiles(page, profiles) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

        var currentType;

        for (var i = 0, length = profiles.length; i < length; i++) {

            var profile = profiles[i];

            if (profile.Type !== currentType) {

                html += '<li data-role="list-divider">' + profile.Type + '</li>';
                currentType = profile.Type;
            }

            html += '<li>';
            html += '<a data-profileindex="' + i + '" class="lnkEditSubProfile" href="#">';

            html += '<p>' + Globalize.translate('ValueContainer').replace('{0}', (profile.Container || allText)) + '</p>';

            if (profile.Type == 'Video') {

                html += '<p>' + Globalize.translate('ValueVideoCodec').replace('{0}', (profile.VideoCodec || allText)) + '</p>';
                html += '<p>' + Globalize.translate('ValueAudioCodec').replace('{0}', (profile.AudioCodec || allText)) + '</p>';

            } else if (profile.Type == 'Audio') {
                html += '<p>' + Globalize.translate('ValueCodec').replace('{0}', (profile.AudioCodec || allText)) + '</p>';
            }

            html += '</a>';

            html += '<a href="#" data-icon="delete" class="btnDeleteProfile" data-profileindex="' + i + '">Delete</a>';

            html += '</li>';
        }

        html += '</ul>';

        var elem = $('.directPlayProfiles', page).html(html).trigger('create');

        $('.btnDeleteProfile', elem).on('click', function () {

            var index = this.getAttribute('data-profileindex');
            deleteDirectPlayProfile(page, index);
        });

        $('.lnkEditSubProfile', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-profileindex'));

            editDirectPlayProfile(page, currentProfile.DirectPlayProfiles[index]);
        });
    }

    function deleteDirectPlayProfile(page, index) {

        currentProfile.DirectPlayProfiles.splice(index, 1);

        renderDirectPlayProfiles(page, currentProfile.DirectPlayProfiles);

    }

    function editDirectPlayProfile(page, directPlayProfile) {

        isSubProfileNew = directPlayProfile == null;
        directPlayProfile = directPlayProfile || {};
        currentSubProfile = directPlayProfile;

        var popup = $('#popupEditDirectPlayProfile', page);

        $('#selectDirectPlayProfileType', popup).val(directPlayProfile.Type || 'Video').selectmenu('refresh').trigger('change');
        $('#txtDirectPlayContainer', popup).val(directPlayProfile.Container || '');
        $('#txtDirectPlayAudioCodec', popup).val(directPlayProfile.AudioCodec || '');
        $('#txtDirectPlayVideoCodec', popup).val(directPlayProfile.VideoCodec || '');

        popup.popup('open');
    }

    function renderTranscodingProfiles(page, profiles) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

        var currentType;

        for (var i = 0, length = profiles.length; i < length; i++) {

            var profile = profiles[i];

            if (profile.Type !== currentType) {

                html += '<li data-role="list-divider">' + profile.Type + '</li>';
                currentType = profile.Type;
            }

            html += '<li>';
            html += '<a data-profileindex="' + i + '" class="lnkEditSubProfile" href="#">';

            html += '<p>Protocol: ' + (profile.Protocol || 'Http') + '</p>';
            html += '<p>' + Globalize.translate('ValueContainer').replace('{0}', (profile.Container || allText)) + '</p>';

            if (profile.Type == 'Video') {
                html += '<p>' + Globalize.translate('ValueVideoCodec').replace('{0}', (profile.VideoCodec || allText)) + '</p>';
                html += '<p>' + Globalize.translate('ValueAudioCodec').replace('{0}', (profile.AudioCodec || allText)) + '</p>';
            } else if (profile.Type == 'Audio') {
                html += '<p>' + Globalize.translate('ValueCodec').replace('{0}', (profile.AudioCodec || allText)) + '</p>';
            }

            html += '</a>';

            html += '<a href="#" data-icon="delete" class="btnDeleteProfile" data-profileindex="' + i + '">Delete</a>';

            html += '</li>';
        }

        html += '</ul>';

        var elem = $('.transcodingProfiles', page).html(html).trigger('create');

        $('.btnDeleteProfile', elem).on('click', function () {

            var index = this.getAttribute('data-profileindex');
            deleteTranscodingProfile(page, index);
        });

        $('.lnkEditSubProfile', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-profileindex'));

            editTranscodingProfile(page, currentProfile.TranscodingProfiles[index]);
        });
    }

    function editTranscodingProfile(page, transcodingProfile) {

        isSubProfileNew = transcodingProfile == null;
        transcodingProfile = transcodingProfile || {};
        currentSubProfile = transcodingProfile;

        var popup = $('#transcodingProfilePopup', page);

        $('#selectTranscodingProfileType', popup).val(transcodingProfile.Type || 'Video').selectmenu('refresh').trigger('change');
        $('#txtTranscodingContainer', popup).val(transcodingProfile.Container || '');
        $('#txtTranscodingAudioCodec', popup).val(transcodingProfile.AudioCodec || '');
        $('#txtTranscodingVideoCodec', popup).val(transcodingProfile.VideoCodec || '');

        $('#txtTranscodingVideoProfile', popup).val(transcodingProfile.VideoProfile || '');
        $('#chkEnableMpegtsM2TsMode', popup).checked(transcodingProfile.EnableMpegtsM2TsMode || false).checkboxradio('refresh');
        $('#chkEstimateContentLength', popup).checked(transcodingProfile.EstimateContentLength || false).checkboxradio('refresh');
        $('#chkReportByteRangeRequests', popup).checked(transcodingProfile.TranscodeSeekInfo == 'Bytes').checkboxradio('refresh');

        $('.radioTabButton:first', popup).checked(true).checkboxradio('refresh').trigger('change');

        popup.popup('open');
    }

    function deleteTranscodingProfile(page, index) {

        currentProfile.TranscodingProfiles.splice(index, 1);

        renderTranscodingProfiles(page, currentProfile.TranscodingProfiles);

    }

    function saveTranscodingProfile(page) {

        currentSubProfile.Type = $('#selectTranscodingProfileType', page).val();
        currentSubProfile.Container = $('#txtTranscodingContainer', page).val();
        currentSubProfile.AudioCodec = $('#txtTranscodingAudioCodec', page).val();
        currentSubProfile.VideoCodec = $('#txtTranscodingVideoCodec', page).val();

        currentSubProfile.VideoProfile = $('#txtTranscodingVideoProfile', page).val();
        currentSubProfile.EnableMpegtsM2TsMode = $('#chkEnableMpegtsM2TsMode', page).checked();
        currentSubProfile.EstimateContentLength = $('#chkEstimateContentLength', page).checked();
        currentSubProfile.TranscodeSeekInfo = $('#chkReportByteRangeRequests', page).checked() ? 'Bytes' : 'Auto';

        if (isSubProfileNew) {

            currentProfile.TranscodingProfiles.push(currentSubProfile);
        }

        renderSubProfiles(page, currentProfile);

        currentSubProfile = null;

        $('#transcodingProfilePopup', page).popup('close');
    }

    function renderContainerProfiles(page, profiles) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

        var currentType;

        for (var i = 0, length = profiles.length; i < length; i++) {

            var profile = profiles[i];

            if (profile.Type !== currentType) {

                html += '<li data-role="list-divider">' + profile.Type + '</li>';
                currentType = profile.Type;
            }

            html += '<li>';
            html += '<a data-profileindex="' + i + '" class="lnkEditSubProfile" href="#">';

            html += '<p>' + Globalize.translate('ValueContainer').replace('{0}', (profile.Container || allText)) + '</p>';

            if (profile.Conditions && profile.Conditions.length) {

                html += '<p>';

                html += Globalize.translate('ValueConditions').replace('{0}', profile.Conditions.map(function (c) {
                    return c.Property;
                }).join(', '));

                html += '</p>';
            }

            html += '</a>';

            html += '<a href="#" data-icon="delete" class="btnDeleteProfile" data-profileindex="' + i + '">Delete</a>';

            html += '</li>';
        }

        html += '</ul>';

        var elem = $('.containerProfiles', page).html(html).trigger('create');

        $('.btnDeleteProfile', elem).on('click', function () {

            var index = this.getAttribute('data-profileindex');
            deleteContainerProfile(page, index);
        });

        $('.lnkEditSubProfile', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-profileindex'));

            editContainerProfile(page, currentProfile.ContainerProfiles[index]);
        });
    }

    function deleteContainerProfile(page, index) {

        currentProfile.ContainerProfiles.splice(index, 1);

        renderContainerProfiles(page, currentProfile.ContainerProfiles);

    }

    function editContainerProfile(page, containerProfile) {

        isSubProfileNew = containerProfile == null;
        containerProfile = containerProfile || {};
        currentSubProfile = containerProfile;

        var popup = $('#containerProfilePopup', page);

        $('#selectContainerProfileType', popup).val(containerProfile.Type || 'Video').selectmenu('refresh').trigger('change');
        $('#txtContainerProfileContainer', popup).val(containerProfile.Container || '');

        $('.radioTabButton:first', popup).checked(true).checkboxradio('refresh').trigger('change');

        popup.popup('open');
    }

    function saveContainerProfile(page) {

        currentSubProfile.Type = $('#selectContainerProfileType', page).val();
        currentSubProfile.Container = $('#txtContainerProfileContainer', page).val();

        if (isSubProfileNew) {

            currentProfile.ContainerProfiles.push(currentSubProfile);
        }

        renderSubProfiles(page, currentProfile);

        currentSubProfile = null;

        $('#containerProfilePopup', page).popup('close');
    }

    function renderCodecProfiles(page, profiles) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

        var currentType;

        for (var i = 0, length = profiles.length; i < length; i++) {

            var profile = profiles[i];

            var type = profile.Type.replace("VideoAudio", "Video Audio");

            if (type !== currentType) {

                html += '<li data-role="list-divider">' + type + '</li>';
                currentType = type;
            }

            html += '<li>';
            html += '<a data-profileindex="' + i + '" class="lnkEditSubProfile" href="#">';

            html += '<p>' + Globalize.translate('ValueCodec').replace('{0}', (profile.Codec || allText)) + '</p>';

            if (profile.Conditions && profile.Conditions.length) {

                html += '<p>';

                html += Globalize.translate('ValueConditions').replace('{0}', profile.Conditions.map(function (c) {
                    return c.Property;
                }).join(', '));

                html += '</p>';
            }

            html += '</a>';

            html += '<a href="#" data-icon="delete" class="btnDeleteProfile" data-profileindex="' + i + '">Delete</a>';

            html += '</li>';
        }

        html += '</ul>';

        var elem = $('.codecProfiles', page).html(html).trigger('create');

        $('.btnDeleteProfile', elem).on('click', function () {

            var index = this.getAttribute('data-profileindex');
            deleteCodecProfile(page, index);
        });

        $('.lnkEditSubProfile', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-profileindex'));

            editCodecProfile(page, currentProfile.CodecProfiles[index]);
        });
    }

    function deleteCodecProfile(page, index) {

        currentProfile.CodecProfiles.splice(index, 1);

        renderCodecProfiles(page, currentProfile.CodecProfiles);

    }

    function editCodecProfile(page, codecProfile) {

        isSubProfileNew = codecProfile == null;
        codecProfile = codecProfile || {};
        currentSubProfile = codecProfile;

        var popup = $('#codecProfilePopup', page);

        $('#selectCodecProfileType', popup).val(codecProfile.Type || 'Video').selectmenu('refresh').trigger('change');
        $('#txtCodecProfileCodec', popup).val(codecProfile.Codec || '');

        $('.radioTabButton:first', popup).checked(true).checkboxradio('refresh').trigger('change');

        popup.popup('open');
    }

    function saveCodecProfile(page) {

        currentSubProfile.Type = $('#selectCodecProfileType', page).val();
        currentSubProfile.Codec = $('#txtCodecProfileCodec', page).val();

        if (isSubProfileNew) {

            currentProfile.CodecProfiles.push(currentSubProfile);
        }

        renderSubProfiles(page, currentProfile);

        currentSubProfile = null;

        $('#codecProfilePopup', page).popup('close');
    }

    function renderResponseProfiles(page, profiles) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

        var currentType;

        for (var i = 0, length = profiles.length; i < length; i++) {

            var profile = profiles[i];

            if (profile.Type !== currentType) {

                html += '<li data-role="list-divider">' + profile.Type + '</li>';
                currentType = profile.Type;
            }

            html += '<li>';
            html += '<a data-profileindex="' + i + '" class="lnkEditSubProfile" href="#">';

            html += '<p>' + Globalize.translate('ValueContainer').replace('{0}', (profile.Container || allText)) + '</p>';

            if (profile.Type == 'Video') {
                html += '<p>' + Globalize.translate('ValueVideoCodec').replace('{0}', (profile.VideoCodec || allText)) + '</p>';
                html += '<p>' + Globalize.translate('ValueAudioCodec').replace('{0}', (profile.AudioCodec || allText)) + '</p>';
            } else if (profile.Type == 'Audio') {
                html += '<p>' + Globalize.translate('ValueCodec').replace('{0}', (profile.AudioCodec || allText)) + '</p>';
            }

            if (profile.Conditions && profile.Conditions.length) {

                html += '<p>';

                html += Globalize.translate('ValueConditions').replace('{0}', profile.Conditions.map(function (c) {
                    return c.Property;
                }).join(', '));

                html += '</p>';
            }

            html += '</a>';

            html += '<a href="#" data-icon="delete" class="btnDeleteProfile" data-profileindex="' + i + '">Delete</a>';

            html += '</li>';
        }

        html += '</ul>';

        var elem = $('.mediaProfiles', page).html(html).trigger('create');

        $('.btnDeleteProfile', elem).on('click', function () {

            var index = this.getAttribute('data-profileindex');
            deleteResponseProfile(page, index);
        });

        $('.lnkEditSubProfile', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-profileindex'));

            editResponseProfile(page, currentProfile.ResponseProfiles[index]);
        });
    }

    function deleteResponseProfile(page, index) {

        currentProfile.ResponseProfiles.splice(index, 1);

        renderResponseProfiles(page, currentProfile.ResponseProfiles);
    }

    function editResponseProfile(page, responseProfile) {

        isSubProfileNew = responseProfile == null;
        responseProfile = responseProfile || {};
        currentSubProfile = responseProfile;

        var popup = $('#responseProfilePopup', page);

        $('#selectResponseProfileType', popup).val(responseProfile.Type || 'Video').selectmenu('refresh').trigger('change');
        $('#txtResponseProfileContainer', popup).val(responseProfile.Container || '');
        $('#txtResponseProfileAudioCodec', popup).val(responseProfile.AudioCodec || '');
        $('#txtResponseProfileVideoCodec', popup).val(responseProfile.VideoCodec || '');

        $('.radioTabButton:first', popup).checked(true).checkboxradio('refresh').trigger('change');

        popup.popup('open');
    }

    function saveResponseProfile(page) {

        currentSubProfile.Type = $('#selectResponseProfileType', page).val();
        currentSubProfile.Container = $('#txtResponseProfileContainer', page).val();
        currentSubProfile.AudioCodec = $('#txtResponseProfileAudioCodec', page).val();
        currentSubProfile.VideoCodec = $('#txtResponseProfileVideoCodec', page).val();

        if (isSubProfileNew) {

            currentProfile.ResponseProfiles.push(currentSubProfile);
        }

        renderSubProfiles(page, currentProfile);

        currentSubProfile = null;

        $('#responseProfilePopup', page).popup('close');
    }

    function saveProfile(page, profile) {

        updateProfile(page, profile);

        var id = getParameterByName('id');

        if (id) {

            ApiClient.ajax({
                type: "POST",
                url: ApiClient.getUrl("Dlna/Profiles/" + id),
                data: JSON.stringify(profile),
                contentType: "application/json"
            }).done(function () {

                Dashboard.alert('Settings saved.');
            });

        } else {

            ApiClient.ajax({
                type: "POST",
                url: ApiClient.getUrl("Dlna/Profiles"),
                data: JSON.stringify(profile),
                contentType: "application/json"
            }).done(function () {

                Dashboard.navigate('dlnaprofiles.html');

            });

        }

        Dashboard.hideLoadingMsg();
    }

    function updateProfile(page, profile) {

        profile.Name = $('#txtName', page).val();
        profile.EnableAlbumArtInDidl = $('#chkEnableAlbumArtInDidl', page).checked();

        profile.SupportedMediaTypes = $('.chkMediaType:checked', page).get().map(function (c) {
            return c.getAttribute('data-value');
        }).join(',');

        profile.Identification = profile.Identification || {};

        profile.FriendlyName = $('#txtInfoFriendlyName', page).val();
        profile.ModelName = $('#txtInfoModelName', page).val();
        profile.ModelNumber = $('#txtInfoModelNumber', page).val();
        profile.ModelDescription = $('#txtInfoModelDescription', page).val();
        profile.ModelUrl = $('#txtInfoModelUrl', page).val();
        profile.Manufacturer = $('#txtInfoManufacturer', page).val();
        profile.ManufacturerUrl = $('#txtInfoManufacturerUrl', page).val();
        profile.SerialNumber = $('#txtInfoSerialNumber', page).val();

        profile.Identification.FriendlyName = $('#txtIdFriendlyName', page).val();
        profile.Identification.ModelName = $('#txtIdModelName', page).val();
        profile.Identification.ModelNumber = $('#txtIdModelNumber', page).val();
        profile.Identification.ModelDescription = $('#txtIdModelDescription', page).val();
        profile.Identification.ModelUrl = $('#txtIdModelUrl', page).val();
        profile.Identification.Manufacturer = $('#txtIdManufacturer', page).val();
        profile.Identification.ManufacturerUrl = $('#txtIdManufacturerUrl', page).val();
        profile.Identification.SerialNumber = $('#txtIdSerialNumber', page).val();
        profile.Identification.DeviceDescription = $('#txtIdDeviceDescription', page).val();

        profile.AlbumArtPn = $('#txtAlbumArtPn', page).val();
        profile.MaxAlbumArtWidth = $('#txtAlbumArtMaxWidth', page).val();
        profile.MaxAlbumArtHeight = $('#txtAlbumArtMaxHeight', page).val();
        profile.MaxIconWidth = $('#txtIconMaxWidth', page).val();
        profile.MaxIconHeight = $('#txtIconMaxHeight', page).val();

        profile.RequiresPlainFolders = $('#chkRequiresPlainFolders', page).checked();
        profile.RequiresPlainVideoItems = $('#chkRequiresPlainVideoItems', page).checked();

        profile.IgnoreTranscodeByteRangeRequests = $('#chkIgnoreTranscodeByteRangeRequests', page).checked();
        profile.MaxBitrate = $('#txtMaxAllowedBitrate', page).val();

        profile.ProtocolInfo = $('#txtProtocolInfo', page).val();
        profile.XDlnaCap = $('#txtXDlnaCap', page).val();
        profile.XDlnaDoc = $('#txtXDlnaDoc', page).val();
        profile.SonyAggregationFlags = $('#txtSonyAggregationFlags', page).val();

        profile.UserId = $('#selectUser', page).val();
    }

    $(document).on('pageinit', "#dlnaProfilePage", function () {

        var page = this;

        $('.radioTabButton', page).on('change', function () {

            var elem = $('.' + this.value, page);
            elem.siblings('.tabContent').hide();

            elem.show();
        });

        $('#selectDirectPlayProfileType', page).on('change', function () {

            if (this.value == 'Video') {
                $('#fldDirectPlayVideoCodec', page).show();
            } else {
                $('#fldDirectPlayVideoCodec', page).hide();
            }

            if (this.value == 'Photo') {
                $('#fldDirectPlayAudioCodec', page).hide();
            } else {
                $('#fldDirectPlayAudioCodec', page).show();
            }

        });

        $('#selectTranscodingProfileType', page).on('change', function () {

            if (this.value == 'Video') {
                $('#fldTranscodingVideoCodec', page).show();
                $('#fldEnableMpegtsM2TsMode', page).show();
                $('#fldVideoProfile', page).show();
            } else {
                $('#fldTranscodingVideoCodec', page).hide();
                $('#fldEnableMpegtsM2TsMode', page).hide();
                $('#fldVideoProfile', page).hide();
            }

            if (this.value == 'Photo') {
                $('#fldTranscodingAudioCodec', page).hide();

                $('#fldEstimateContentLength', page).hide();
                $('#fldReportByteRangeRequests', page).hide();

            } else {
                $('#fldTranscodingAudioCodec', page).show();

                $('#fldEstimateContentLength', page).show();
                $('#fldReportByteRangeRequests', page).show();
            }

        });

        $('#selectResponseProfileType', page).on('change', function () {

            if (this.value == 'Video') {
                $('#fldResponseProfileVideoCodec', page).show();
            } else {
                $('#fldResponseProfileVideoCodec', page).hide();
            }

            if (this.value == 'Photo') {
                $('#fldResponseProfileAudioCodec', page).hide();
            } else {
                $('#fldResponseProfileAudioCodec', page).show();
            }

        });

        $('.btnAddDirectPlayProfile', page).on('click', function () {

            editDirectPlayProfile(page);

        });

        $('.btnAddTranscodingProfile', page).on('click', function () {

            editTranscodingProfile(page);

        });

        $('.btnAddContainerProfile', page).on('click', function () {

            editContainerProfile(page);

        });

        $('.btnAddCodecProfile', page).on('click', function () {

            editCodecProfile(page);

        });

        $('.btnAddResponseProfile', page).on('click', function () {

            editResponseProfile(page);

        });

    }).on('pageshow', "#dlnaProfilePage", function () {

        var page = this;

        loadProfile(page);

    }).on('pagebeforeshow', "#dlnaProfilePage", function () {

        var page = this;

        $('.radioTabButton', page).checked(false).checkboxradio('refresh');
        $('#radioInfo', page).checked(true).checkboxradio('refresh').trigger('change');

    });

    window.DlnaProfilePage = {
        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;
            var page = $(form).parents('.page');

            saveProfile(page, currentProfile);

            return false;
        },

        onDirectPlayFormSubmit: function () {

            var form = this;
            var page = $(form).parents('.page');

            saveDirectPlayProfile(page);

            return false;
        },

        onTranscodingProfileFormSubmit: function () {

            var form = this;
            var page = $(form).parents('.page');

            saveTranscodingProfile(page);

            return false;

        },

        onContainerProfileFormSubmit: function () {
            var form = this;
            var page = $(form).parents('.page');

            saveContainerProfile(page);

            return false;

        },

        onCodecProfileFormSubmit: function () {
            var form = this;
            var page = $(form).parents('.page');

            saveCodecProfile(page);

            return false;
        },

        onResponseProfileFormSubmit: function () {
            var form = this;
            var page = $(form).parents('.page');

            saveResponseProfile(page);

            return false;
        }
    };

})(jQuery, document, window);
