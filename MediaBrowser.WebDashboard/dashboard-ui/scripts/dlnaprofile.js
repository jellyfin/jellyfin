(function ($, document, window) {

    function loadProfile(page) {

        Dashboard.showLoadingMsg();

        getProfile().done(function (result) {

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

    }

    $(document).on('pageinit', "#dlnaProfilePage", function () {

        var page = this;

        $('.radioProfileTab', page).on('change', function () {

            $('.profileTab', page).hide();
            $('.' + this.value, page).show();

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

            getProfile().done(function (profile) {

                saveProfile(page, profile);
            });

            return false;
        }
    };

})(jQuery, document, window);
