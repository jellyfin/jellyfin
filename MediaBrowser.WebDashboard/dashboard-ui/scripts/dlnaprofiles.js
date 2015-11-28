(function ($, document, window) {

    function loadProfiles(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl("Dlna/ProfileInfos")).then(function (result) {

            renderProfiles(page, result);

            Dashboard.hideLoadingMsg();
        });

    }

    function renderProfiles(page, profiles) {

        renderUserProfiles(page, profiles);
        renderSystemProfiles(page, profiles);
    }

    function renderUserProfiles(page, profiles) {

        profiles = profiles.filter(function (p) {
            return p.Type == 'User';
        });

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

        for (var i = 0, length = profiles.length; i < length; i++) {

            var profile = profiles[i];

            html += '<li>';
            html += '<a href="dlnaprofile.html?id=' + profile.Id + '">';
            html += profile.Name;
            html += '</a>';

            html += '<a href="#" data-icon="delete" class="btnDeleteProfile" data-profileid="' + profile.Id + '">' + Globalize.translate('Delete') + '</a>';

            html += '</li>';
        }

        html += '</ul>';

        var elem = $('.customProfiles', page).html(html).trigger('create');

        $('.btnDeleteProfile', elem).on('click', function () {

            var id = this.getAttribute('data-profileid');
            deleteProfile(page, id);
        });
    }

    function renderSystemProfiles(page, profiles) {

        profiles = profiles.filter(function (p) {
            return p.Type == 'System';
        });

        var html = '';

        html += '<ul data-role="listview" data-inset="true">';

        for (var i = 0, length = profiles.length; i < length; i++) {

            var profile = profiles[i];

            html += '<li>';
            html += '<a href="dlnaprofile.html?id=' + profile.Id + '">';
            html += profile.Name;
            html += '</a>';
            html += '</li>';
        }

        html += '</ul>';

        $('.systemProfiles', page).html(html).trigger('create');
    }

    function deleteProfile(page, id) {

        Dashboard.confirm(Globalize.translate('MessageConfirmProfileDeletion'), Globalize.translate('HeaderConfirmProfileDeletion'), function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("Dlna/Profiles/" + id)

                }).then(function () {

                    Dashboard.hideLoadingMsg();

                    loadProfiles(page);
                });
            }

        });

    }

    $(document).on('pageshow', "#dlnaProfilesPage", function () {

        var page = this;

        loadProfiles(page);

    });

})(jQuery, document, window);
