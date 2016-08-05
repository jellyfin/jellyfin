define(['jQuery', 'listViewStyle'], function ($) {

    function loadProfiles(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl("Dlna/ProfileInfos")).then(function (result) {

            renderUserProfiles(page, result);
            renderSystemProfiles(page, result);

            Dashboard.hideLoadingMsg();
        });

    }

    function renderUserProfiles(page, profiles) {

        renderProfiles(page, page.querySelector('.customProfiles'), profiles.filter(function (p) {
            return p.Type == 'User';
        }));
    }

    function renderSystemProfiles(page, profiles) {

        renderProfiles(page, page.querySelector('.systemProfiles'), profiles.filter(function (p) {
            return p.Type == 'System';
        }));
    }

    function renderProfiles(page, element, profiles) {

        var html = '';

        if (profiles.length) {
            html += '<div class="paperList">';
        }

        for (var i = 0, length = profiles.length; i < length; i++) {

            var profile = profiles[i];

            html += '<div class="listItem">';

            html += "<a item-icon class='clearLink listItemIconContainer' href='dlnaprofile.html?id=" + profile.Id + "'>";
            html += '<i class="md-icon listItemIcon">dvr</i>';
            html += "</a>";

            html += '<div class="listItemBody">';
            html += "<a class='clearLink' href='dlnaprofile.html?id=" + profile.Id + "'>";

            html += "<div>" + profile.Name + "</div>";
            //html += "<div secondary>" + task.Description + "</div>";

            html += "</a>";
            html += '</div>';

            if (profile.Type == 'User') {
                html += '<button type="button" is="paper-icon-button-light" class="btnDeleteProfile" data-profileid="' + profile.Id + '" title="' + Globalize.translate('ButtonDelete') + '"><i class="md-icon">delete</i></button>';
            }

            html += '</div>';
        }

        if (profiles.length) {
            html += '</div>';
        }

        element.innerHTML = html;

        $('.btnDeleteProfile', element).on('click', function () {

            var id = this.getAttribute('data-profileid');
            deleteProfile(page, id);
        });
    }

    function deleteProfile(page, id) {

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmProfileDeletion'), Globalize.translate('HeaderConfirmProfileDeletion')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("Dlna/Profiles/" + id)

                }).then(function () {

                    Dashboard.hideLoadingMsg();

                    loadProfiles(page);
                });
            });
        });
    }

    function getTabs() {
        return [
        {
            href: 'dlnasettings.html',
            name: Globalize.translate('TabSettings')
        },
         {
             href: 'dlnaprofiles.html',
             name: Globalize.translate('TabProfiles')
         }];
    }

    $(document).on('pageshow', "#dlnaProfilesPage", function () {

        LibraryMenu.setTabs('dlna', 1, getTabs);
        var page = this;

        loadProfiles(page);

    });

});
