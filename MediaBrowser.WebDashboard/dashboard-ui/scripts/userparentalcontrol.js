(function ($, window, document) {

    function populateRatings(allParentalRatings, page) {

        var html = "";

        html += "<option value=''></option>";

        var ratings = [];
        var i, length, rating;

        for (i = 0, length = allParentalRatings.length; i < length; i++) {

            rating = allParentalRatings[i];

            if (ratings.length) {

                var lastRating = ratings[ratings.length - 1];

                if (lastRating.Value === rating.Value) {

                    lastRating.Name += "/" + rating.Name;
                    continue;
                }

            }

            ratings.push({ Name: rating.Name, Value: rating.Value });
        }

        for (i = 0, length = ratings.length; i < length; i++) {

            rating = ratings[i];

            html += "<option value='" + rating.Value + "'>" + rating.Name + "</option>";
        }

        $('#selectMaxParentalRating', page).html(html).selectmenu("refresh");
    }

    function loadUnratedItems(page, user) {

        var items = [
            { name: Globalize.translate('OptionBlockBooks'), value: 'Book' },
            { name: Globalize.translate('OptionBlockGames'), value: 'Game' },
            { name: Globalize.translate('OptionBlockChannelContent'), value: 'ChannelContent' },
            { name: Globalize.translate('OptionBlockLiveTvChannels'), value: 'LiveTvChannel' },
            { name: Globalize.translate('OptionBlockLiveTvPrograms'), value: 'LiveTvProgram' },
            { name: Globalize.translate('OptionBlockMovies'), value: 'Movie' },
            { name: Globalize.translate('OptionBlockMusic'), value: 'Music' },
            { name: Globalize.translate('OptionBlockTrailers'), value: 'Trailer' },
            { name: Globalize.translate('OptionBlockTvShows'), value: 'Series' },
            { name: Globalize.translate('OptionBlockOthers'), value: 'Other' }
        ];

        var html = '';

        html += '<fieldset data-role="controlgroup">';

        html += '<legend>' + Globalize.translate('HeaderBlockItemsWithNoRating') + '</legend>';

        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var id = 'unratedItem' + i;

            var checkedAttribute = user.Configuration.BlockUnratedItems.indexOf(item.value) != -1 ? ' checked="checked"' : '';

            html += '<input class="chkUnratedItem" data-itemtype="' + item.value + '" type="checkbox" id="' + id + '"' + checkedAttribute + ' />';
            html += '<label for="' + id + '">' + item.name + '</label>';
        }

        html += '</fieldset>';

        $('.blockUnratedItems', page).html(html).trigger('create');
    }

    function loadUser(page, user, allParentalRatings) {

        Dashboard.setPageTitle(user.Name);

        loadUnratedItems(page, user);
        loadTags(page, user.Configuration.BlockedTags);

        populateRatings(allParentalRatings, page);

        var ratingValue = "";

        if (user.Configuration.MaxParentalRating) {

            for (var i = 0, length = allParentalRatings.length; i < length; i++) {

                var rating = allParentalRatings[i];

                if (user.Configuration.MaxParentalRating >= rating.Value) {
                    ratingValue = rating.Value;
                }
            }
        }

        $('#selectMaxParentalRating', page).val(ratingValue).selectmenu("refresh");

        if (user.Configuration.IsAdministrator) {
            $('.accessScheduleSection', page).hide();
        } else {
            $('.accessScheduleSection', page).show();
        }

        renderAccessSchedule(page, user.Configuration.AccessSchedules || []);

        Dashboard.hideLoadingMsg();
    }

    function loadTags(page, tags) {

        var html = '<ul data-role="listview" data-inset="true" data-split-icon="delete">' + tags.map(function (h) {

            var li = '<li>';

            li += '<a href="#">';

            li += '<div style="font-weight:normal;">' + h + '</div>';

            li += '</a>';

            li += '<a class="blockedTag btnDeleteTag" href="#" data-tag="' + h + '"></a>';

            li += '</li>';

            return li;

        }).join('') + '</ul>';

        var elem = $('.blockedTags', page).html(html).trigger('create');

        $('.btnDeleteTag', elem).on('click', function () {

            var tag = this.getAttribute('data-tag');

            var newTags = tags.filter(function (t) {
                return t != tag;
            });

            loadTags(page, newTags);
        });
    }

    function deleteAccessSchedule(page, schedules, index) {

        schedules.splice(index, 1);

        renderAccessSchedule(page, schedules);
    }

    function renderAccessSchedule(page, schedules) {

        var html = '<ul data-role="listview" data-inset="true" data-split-icon="minus">';
        var index = 0;

        html += schedules.map(function (a) {

            var itemHtml = '';

            itemHtml += '<li class="liSchedule" data-day="' + a.DayOfWeek + '" data-start="' + a.StartHour + '" data-end="' + a.EndHour + '">';

            itemHtml += '<a href="#">';
            itemHtml += '<h3>' + a.DayOfWeek + '</h3>';
            itemHtml += '<p>' + getDisplayTime(a.StartHour) + ' - ' + getDisplayTime(a.EndHour) + '</p>';
            itemHtml += '</a>';

            itemHtml += '<a href="#" data-icon="delete" class="btnDelete" data-index="' + index + '">';
            itemHtml += '</a>';

            itemHtml += '</li>';

            index++;

            return itemHtml;

        }).join('');

        html += '</ul>';

        var elem = $('.accessScheduleList', page).html(html).trigger('create');

        $('.btnDelete', elem).on('click', function () {

            deleteAccessSchedule(page, schedules, parseInt(this.getAttribute('data-index')));
        });
    }

    function onSaveComplete(page) {

        Dashboard.hideLoadingMsg();

        Dashboard.alert(Globalize.translate('SettingsSaved'));
    }

    function saveUser(user, page) {

        user.Configuration.MaxParentalRating = $('#selectMaxParentalRating', page).val() || null;

        user.Configuration.BlockUnratedItems = $('.chkUnratedItem:checked', page).map(function () {

            return this.getAttribute('data-itemtype');

        }).get();

        user.Configuration.AccessSchedules = getSchedulesFromPage(page);

        user.Configuration.BlockedTags = getTagsFromPage(page);

        ApiClient.updateUserConfiguration(user.Id, user.Configuration).done(function () {
            onSaveComplete(page);
        });
    }

    window.UserParentalControlPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            ApiClient.getUser(userId).done(function (result) {
                saveUser(result, page);
            });

            // Disable default form submission
            return false;
        },

        onScheduleFormSubmit: function () {

            var page = $(this).parents('.page');

            saveSchedule(page);

            // Disable default form submission
            return false;
        },

        onTagFormSubmit: function() {
            
            var page = $(this).parents('.page');

            saveTag(page);

            // Disable default form submission
            return false;
        }
    };

    function getDisplayTime(hours) {

        var minutes = 0;

        var pct = hours % 1;

        if (pct) {
            minutes = parseInt(pct * 60);
        }

        return new Date(2000, 1, 1, hours, minutes, 0, 0).toLocaleTimeString();
    }

    function populateHours(page) {

        var html = '';

        for (var i = 0; i < 24; i++) {

            html += '<option value="' + i + '">' + getDisplayTime(i) + '</option>';
        }

        html += '<option value="24">' + getDisplayTime(0) + '</option>';

        $('#selectStart', page).html(html).selectmenu('refresh');
        $('#selectEnd', page).html(html).selectmenu('refresh');
    }

    function showSchedulePopup(page, schedule, index) {

        schedule = schedule || {};

        $('#popupSchedule', page).popup('open');

        $('#fldScheduleIndex', page).val(index);

        $('#selectDay', page).val(schedule.DayOfWeek || 'Sunday').selectmenu('refresh');
        $('#selectStart', page).val(schedule.StartHour || 0).selectmenu('refresh');
        $('#selectEnd', page).val(schedule.EndHour || 0).selectmenu('refresh');
    }

    function saveSchedule(page) {

        var schedule = {
            DayOfWeek: $('#selectDay', page).val(),
            StartHour: $('#selectStart', page).val(),
            EndHour: $('#selectEnd', page).val()
        };

        if (parseFloat(schedule.StartHour) >= parseFloat(schedule.EndHour)) {

            alert(Globalize.translate('ErrorMessageStartHourGreaterThanEnd'));

            return;
        }

        var schedules = getSchedulesFromPage(page);

        var index = parseInt($('#fldScheduleIndex', page).val());

        if (index == -1) {
            index = schedules.length;
        }

        schedules[index] = schedule;

        renderAccessSchedule(page, schedules);

        $('#popupSchedule', page).popup('close');
    }

    function saveTag(page) {

        var tag = $('#txtTag', page).val();
        var tags = getTagsFromPage(page);

        if (tags.indexOf(tag) == -1) {
            tags.push(tag);
            loadTags(page, tags);
        }

        $('#popupTag', page).popup('close');
    }

    function getSchedulesFromPage(page) {

        return $('.liSchedule', page).map(function () {

            return {
                DayOfWeek: this.getAttribute('data-day'),
                StartHour: this.getAttribute('data-start'),
                EndHour: this.getAttribute('data-end')
            };

        }).get();
    }

    function getTagsFromPage(page) {

        return $('.blockedTag', page).map(function () {

            return this.getAttribute('data-tag');

        }).get();
    }

    function showTagPopup(page) {

        $('#popupTag', page).popup('open');
        $('#txtTag', page).val('').focus();
    }

    $(document).on('pageinit', "#userParentalControlPage", function () {

        var page = this;


        $('.btnAddSchedule', page).on('click', function () {

            showSchedulePopup(page, {}, -1);
        });


        $('.btnAddTag', page).on('click', function () {

            showTagPopup(page);
        });

        populateHours(page);

    }).on('pageshow', "#userParentalControlPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");

        var promise1;

        if (!userId) {

            var deferred = $.Deferred();

            deferred.resolveWith(null, [{
                Configuration: {}
            }]);

            promise1 = deferred.promise();
        } else {

            promise1 = ApiClient.getUser(userId);
        }

        var promise2 = ApiClient.getParentalRatings();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadUser(page, response1[0] || response1, response2[0]);

        });
    });

})(jQuery, window, document);