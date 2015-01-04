(function ($, window, document) {

    function loadMediaFolders(page, mediaFolders) {

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>' + Globalize.translate('HeaderMediaFolders') + '</legend>';

        for (var i = 0, length = mediaFolders.length; i < length; i++) {

            var folder = mediaFolders[i];

            var id = 'mediaFolder' + i;

            var checkedAttribute = ' checked="checked"';

            html += '<input class="chkMediaFolder" data-foldername="' + folder.Id + '" type="checkbox" id="' + id + '"' + checkedAttribute + ' />';
            html += '<label for="' + id + '">' + folder.Name + '</label>';
        }

        html += '</fieldset>';

        $('.mediaFolderAccess', page).html(html).trigger('create');
    }

    function loadChannels(page, channels) {

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>' + Globalize.translate('HeaderChannels') + '</legend>';

        for (var i = 0, length = channels.length; i < length; i++) {

            var folder = channels[i];

            var id = 'channels' + i;

            var checkedAttribute = ' checked="checked"';

            html += '<input class="chkChannel" data-foldername="' + folder.Id + '" type="checkbox" id="' + id + '"' + checkedAttribute + ' />';
            html += '<label for="' + id + '">' + folder.Name + '</label>';
        }

        html += '</fieldset>';

        $('.channelAccess', page).show().html(html).trigger('create');

        if (channels.length) {
            $('.channelAccessContainer', page).show();
        } else {
            $('.channelAccessContainer', page).hide();
        }
    }

    function loadUser(page) {

        $('#txtUserName', page).val('');

        Dashboard.showLoadingMsg();

        var promise4 = ApiClient.getJSON(ApiClient.getUrl("Library/MediaFolders", { IsHidden: false }));

        var promise5 = ApiClient.getJSON(ApiClient.getUrl("Channels"));

        $.when(promise4, promise5).done(function (response4, response5) {

            loadMediaFolders(page, response4[0].Items);
            loadChannels(page, response5[0].Items);

            Dashboard.hideLoadingMsg();
        });
    }

    function saveUser(page) {

        var name = $('#txtUserName', page).val();

        ApiClient.createUser(name).done(function (user) {

            user.Policy.BlockedMediaFolders = $('.chkMediaFolder:not(:checked)', page).map(function () {

                return this.getAttribute('data-foldername');

            }).get();

            user.Policy.BlockedChannels = $('.chkChannel:not(:checked)', page).map(function () {

                return this.getAttribute('data-foldername');

            }).get();

            ApiClient.updateUserPolicy(user.Id, user.Policy).done(function () {
                Dashboard.navigate("useredit.html?userId=" + user.Id);
            });
        });
    }

    function newUserPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $(this).parents('.page');

            Dashboard.showLoadingMsg();

            saveUser(page);

            // Disable default form submission
            return false;
        };
    }

    function loadData(page) {

        loadUser(page);
    }

    window.NewUserPage = new newUserPage();

    $(document).on('pageshow', "#newUserPage", function () {

        var page = this;

        loadData(page);

    });

})(jQuery, window, document);