(function ($, document, window) {

    var currentProfile;

    var currentSubProfile;
    var isSubProfileNew;

    function loadProfile(page) {

        Dashboard.showLoadingMsg();

        getProfile().done(function (result) {

            currentProfile = result;

            renderProfile(page, result);

            Dashboard.hideLoadingMsg();
        });
    }

    function getProfile() {

        var id = getParameterByName('id');
        var url = id ? 'Dlna/Profiles/' + id :
            'Dlna/Profiles/Default';

        return $.getJSON(ApiClient.getUrl(url));
    }

    function renderProfile(page, profile) {

        if (profile.ProfileType == 'User' || !profile.Id) {
            $('.btnSave', page).show();
        } else {
            $('.btnSave', page).hide();
        }

        $('#txtName', page).val(profile.Name);

        $('.chkMediaType', page).each(function () {
            this.checked = (profile.SupportedMediaTypes || '').split(',').indexOf(this.getAttribute('data-value')) != -1;

        }).checkboxradio('refresh');

        $('#chkEnableAlbumArtInDidl', page).checked(profile.EnableAlbumArtInDidl).checkboxradio('refresh');

        var idInfo = profile.Identification || {};

        $('#txtIdFriendlyName', page).val(idInfo.FriendlyName || '');
        $('#txtIdModelName', page).val(idInfo.ModelName || '');
        $('#txtIdModelNumber', page).val(idInfo.ModelNumber || '');
        $('#txtIdModelDescription', page).val(idInfo.ModelDescription || '');
        $('#txtIdModelUrl', page).val(idInfo.ModelUrl || '');
        $('#txtIdManufacturer', page).val(idInfo.Manufacturer || '');
        $('#txtIdManufacturerUrl', page).val(idInfo.ManufacturerUrl || '');
        $('#txtIdSerialNumber', page).val(idInfo.SerialNumber || '');
        $('#txtIdDeviceDescription', page).val(idInfo.DeviceDescription || '');

        profile.DirectPlayProfiles = (profile.DirectPlayProfiles || []);
        profile.TranscodingProfiles = (profile.TranscodingProfiles || []);
        profile.ContainerProfiles = (profile.ContainerProfiles || []);
        profile.CodecProfiles = (profile.CodecProfiles || []);
        profile.ResponseProfiles = (profile.ResponseProfiles || []);

        renderSubProfiles(page, profile);
    }

    function renderSubProfiles(page, profile) {

        renderDirectPlayProfiles(page, profile.DirectPlayProfiles);
        renderTranscodingProfiles(page, profile.TranscodingProfiles);
        renderContainerProfiles(page, profile.ContainerProfiles);
        renderCodecProfiles(page, profile.CodecProfiles);
        renderResponseProfiles(page, profile.ResponseProfiles);
    }

    function editDirectPlayProfile(page, directPlayProfile) {

        isSubProfileNew = directPlayProfile == null;
        directPlayProfile = directPlayProfile || {};
        currentSubProfile = directPlayProfile;

        var popup = $('#popupEditDirectPlayProfile', page).popup('open');

        $('#selectDirectPlayProfileType', popup).val(directPlayProfile.Type || 'Video').selectmenu('refresh').trigger('change');
        $('#txtDirectPlayContainer', popup).val(directPlayProfile.Container || '');
        $('#txtDirectPlayAudioCodec', popup).val(directPlayProfile.AudioCodec || '');
        $('#txtDirectPlayVideoCodec', popup).val(directPlayProfile.VideoCodec || '');
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

            html += '<p>Container: ' + (profile.Container || 'All') + '</p>';

            if (profile.Type == 'Video') {
                html += '<p>Video Codec: ' + (profile.VideoCodec || 'All') + '</p>';
                html += '<p>Audio Codec: ' + (profile.AudioCodec || 'All') + '</p>';
            } else if (profile.Type == 'Audio') {
                html += '<p>Codec: ' + (profile.AudioCodec || 'All') + '</p>';
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
            html += '<p>Container: ' + (profile.Container || 'All') + '</p>';

            if (profile.Type == 'Video') {
                html += '<p>Video Codec: ' + (profile.VideoCodec || 'All') + '</p>';
                html += '<p>Audio Codec: ' + (profile.AudioCodec || 'All') + '</p>';
            } else if (profile.Type == 'Audio') {
                html += '<p>Codec: ' + (profile.AudioCodec || 'All') + '</p>';
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

        var popup = $('#transcodingProfilePopup', page).popup('open');

        $('#selectTranscodingProfileType', popup).val(transcodingProfile.Type || 'Video').selectmenu('refresh').trigger('change');
        $('#txtTranscodingContainer', popup).val(transcodingProfile.Container || '');
        $('#txtTranscodingAudioCodec', popup).val(transcodingProfile.AudioCodec || '');
        $('#txtTranscodingVideoCodec', popup).val(transcodingProfile.VideoCodec || '');

        $('#txtTranscodingVideoProfile', popup).val(transcodingProfile.VideoProfile || '');
        $('#chkEnableMpegtsM2TsMode', popup).checked(transcodingProfile.EnableMpegtsM2TsMode).checkboxradio('refresh');
        $('#chkEstimateContentLength', popup).checked(transcodingProfile.EstimateContentLength).checkboxradio('refresh');
        $('#chkReportByteRangeRequests', popup).checked(transcodingProfile.TranscodeSeekInfo == 'Bytes').checkboxradio('refresh');
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

            html += '<p>Container: ' + (profile.Container || 'All') + '</p>';

            if (profile.Conditions && profile.Conditions.length) {

                html += '<p>Conditions: ';
                html += profile.Conditions.map(function (c) {
                    return c.Property;
                }).join(', ');
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
    }

    function deleteContainerProfile(page, index) {

        currentProfile.ContainerProfiles.splice(index, 1);

        renderContainerProfiles(page, currentProfile.ContainerProfiles);

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

            html += '<p>Codec: ' + (profile.Codec || 'All') + '</p>';

            if (profile.Conditions && profile.Conditions.length) {

                html += '<p>Conditions: ';
                html += profile.Conditions.map(function (c) {
                    return c.Property;
                }).join(', ');
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
    }

    function deleteCodecProfile(page, index) {

        currentProfile.CodecProfiles.splice(index, 1);

        renderCodecProfiles(page, currentProfile.CodecProfiles);

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

            html += '<p>Container: ' + (profile.Container || 'All') + '</p>';

            if (profile.Type == 'Video') {
                html += '<p>Video Codec: ' + (profile.VideoCodec || 'All') + '</p>';
                html += '<p>Audio Codec: ' + (profile.AudioCodec || 'All') + '</p>';
            } else if (profile.Type == 'Audio') {
                html += '<p>Codec: ' + (profile.AudioCodec || 'All') + '</p>';
            }

            if (profile.Conditions && profile.Conditions.length) {

                html += '<p>Conditions: ';
                html += profile.Conditions.map(function (c) {
                    return c.Property;
                }).join(', ');
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
    }

    function deleteResponseProfile(page, index) {

        currentProfile.ResponseProfiles.splice(index, 1);

        renderResponseProfiles(page, currentProfile.ResponseProfiles);

    }

    function saveProfile(page, profile) {

        updateProfile(page, profile);

        var id = getParameterByName('id');

        if (id) {

            $.ajax({
                type: "POST",
                url: ApiClient.getUrl("Dlna/Profiles/" + id),
                data: JSON.stringify(profile),
                contentType: "application/json"
            }).done(function () {

                Dashboard.alert('Settings saved.');
            });

        } else {

            $.ajax({
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

        profile.Identification.FriendlyName = $('#txtIdFriendlyName', page).val();
        profile.Identification.ModelName = $('#txtIdModelName', page).val();
        profile.Identification.ModelNumber = $('#txtIdModelNumber', page).val();
        profile.Identification.ModelDescription = $('#txtIdModelDescription', page).val();
        profile.Identification.ModelUrl = $('#txtIdModelUrl', page).val();
        profile.Identification.Manufacturer = $('#txtIdManufacturer', page).val();
        profile.Identification.ManufacturerUrl = $('#txtIdManufacturerUrl', page).val();
        profile.Identification.SerialNumber = $('#txtIdSerialNumber', page).val();
        profile.Identification.DeviceDescription = $('#txtIdDeviceDescription', page).val();
    }

    $(document).on('pageinit', "#dlnaProfilePage", function () {

        var page = this;

        $('.radioProfileTab', page).on('change', function () {

            $('.profileTab', page).hide();
            $('.' + this.value, page).show();

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

        $('.btnAddDirectPlayProfile', page).on('click', function () {

            editDirectPlayProfile(page);

        });

        $('.btnAddTranscodingProfile', page).on('click', function () {

            editTranscodingProfile(page);

        });

    }).on('pageshow', "#dlnaProfilePage", function () {

        var page = this;

        loadProfile(page);

    }).on('pagebeforeshow', "#dlnaProfilePage", function () {

        var page = this;

        $('.radioSeriesTimerTab', page).checked(false).checkboxradio('refresh');
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

        }
    };

})(jQuery, document, window);
