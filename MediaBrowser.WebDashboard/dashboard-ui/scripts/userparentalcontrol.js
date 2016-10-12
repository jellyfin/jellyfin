define(['jQuery', 'datetime', 'listViewStyle', 'paper-icon-button-light'], function ($, datetime) {

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

        $('#selectMaxParentalRating', page).html(html);
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

        html += '<h3 class="checkboxListLabel">' + Globalize.translate('HeaderBlockItemsWithNoRating') + '</h3>';

        html += '<div class="checkboxList paperList checkboxList-paperList">';

        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var checkedAttribute = user.Policy.BlockUnratedItems.indexOf(item.value) != -1 ? ' checked="checked"' : '';

            html += '<label><input type="checkbox" is="emby-checkbox" class="chkUnratedItem" data-itemtype="' + item.value + '" type="checkbox"' + checkedAttribute + '><span>' + item.name + '</span></label>';
        }

        html += '</div>';

        $('.blockUnratedItems', page).html(html).trigger('create');
    }

    function loadUser(page, user, allParentalRatings) {

        LibraryMenu.setTitle(user.Name);

        loadUnratedItems(page, user);
        loadBlockedTags(page, user.Policy.BlockedTags);

        populateRatings(allParentalRatings, page);

        var ratingValue = "";

        if (user.Policy.MaxParentalRating) {

            for (var i = 0, length = allParentalRatings.length; i < length; i++) {

                var rating = allParentalRatings[i];

                if (user.Policy.MaxParentalRating >= rating.Value) {
                    ratingValue = rating.Value;
                }
            }
        }

        $('#selectMaxParentalRating', page).val(ratingValue);

        if (user.Policy.IsAdministrator) {
            $('.accessScheduleSection', page).hide();
        } else {
            $('.accessScheduleSection', page).show();
        }

        renderAccessSchedule(page, user.Policy.AccessSchedules || []);

        Dashboard.hideLoadingMsg();
    }

    function loadBlockedTags(page, tags) {

        var html = tags.map(function (h) {

            var li = '<div class="listItem">';

            li += '<div class="listItemBody">';
            li += '<h3 class="listItemBodyText">';
            li += h;
            li += '</h3>';
            li += '</div>';

            li += '<button type="button" is="paper-icon-button-light" class="blockedTag btnDeleteTag listItemButton" data-tag="' + h + '"><i class="md-icon">delete</i></button>';

            li += '</div>';

            return li;

        }).join('');

        if (html) {
            html = '<div class="paperList">' + html + '</div>';
        }

        var elem = $('.blockedTags', page).html(html).trigger('create');

        $('.btnDeleteTag', elem).on('click', function () {

            var tag = this.getAttribute('data-tag');

            var newTags = tags.filter(function (t) {
                return t != tag;
            });

            loadBlockedTags(page, newTags);
        });
    }

    function deleteAccessSchedule(page, schedules, index) {

        schedules.splice(index, 1);

        renderAccessSchedule(page, schedules);
    }

    function renderAccessSchedule(page, schedules) {

        var html = '';
        var index = 0;

        html += schedules.map(function (a) {

            var itemHtml = '';

            itemHtml += '<div class="liSchedule listItem" data-day="' + a.DayOfWeek + '" data-start="' + a.StartHour + '" data-end="' + a.EndHour + '">';

            itemHtml += '<div class="listItemBody two-line">';
            itemHtml += '<h3 class="listItemBodyText">';
            itemHtml += Globalize.translate('Option' + a.DayOfWeek);
            itemHtml += '</h3>';
            itemHtml += '<div class="listItemBodyText secondary">' + getDisplayTime(a.StartHour) + ' - ' + getDisplayTime(a.EndHour) + '</div>';
            itemHtml += '</div>';

            itemHtml += '<button type="button" is="paper-icon-button-light" class="btnDelete listItemButton" data-index="' + index + '"><i class="md-icon">delete</i></button>';

            itemHtml += '</div>';

            index++;

            return itemHtml;

        }).join('');

        var accessScheduleList = page.querySelector('.accessScheduleList');
        accessScheduleList.innerHTML = html;

        $('.btnDelete', accessScheduleList).on('click', function () {

            deleteAccessSchedule(page, schedules, parseInt(this.getAttribute('data-index')));
        });
    }

    function onSaveComplete(page) {

        Dashboard.hideLoadingMsg();

        require(['toast'], function (toast) {
            toast(Globalize.translate('SettingsSaved'));
        });
    }

    function saveUser(user, page) {

        user.Policy.MaxParentalRating = $('#selectMaxParentalRating', page).val() || null;

        user.Policy.BlockUnratedItems = $('.chkUnratedItem', page).get().filter(function (i) {

            return i.checked;

        }).map(function (i) {

            return i.getAttribute('data-itemtype');

        });

        user.Policy.AccessSchedules = getSchedulesFromPage(page);

        user.Policy.BlockedTags = getBlockedTagsFromPage(page);

        ApiClient.updateUserPolicy(user.Id, user.Policy).then(function () {
            onSaveComplete(page);
        });
    }

    window.UserParentalControlPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            ApiClient.getUser(userId).then(function (result) {
                saveUser(result, page);
            });

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

        return datetime.getDisplayTime(new Date(2000, 1, 1, hours, minutes, 0, 0));
    }

    function showSchedulePopup(page, schedule, index) {

        schedule = schedule || {};

        require(['components/accessschedule/accessschedule'], function (accessschedule) {
            accessschedule.show({
                schedule: schedule
            }).then(function (updatedSchedule) {

                var schedules = getSchedulesFromPage(page);

                if (index == -1) {
                    index = schedules.length;
                }

                schedules[index] = updatedSchedule;

                renderAccessSchedule(page, schedules);
            });
        });
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

    function getBlockedTagsFromPage(page) {

        return $('.blockedTag', page).map(function () {

            return this.getAttribute('data-tag');

        }).get();
    }

    function showBlockedTagPopup(page) {

        require(['prompt'], function (prompt) {

            prompt({
                label: Globalize.translate('LabelTag')

            }).then(function (value) {
                var tags = getBlockedTagsFromPage(page);

                if (tags.indexOf(value) == -1) {
                    tags.push(value);
                    loadBlockedTags(page, tags);
                }
            });

        });
    }

    $(document).on('pageinit', "#userParentalControlPage", function () {

        var page = this;


        $('.btnAddSchedule', page).on('click', function () {

            showSchedulePopup(page, {}, -1);
        });

        $('.btnAddBlockedTag', page).on('click', function () {

            showBlockedTagPopup(page);
        });

        $('.userParentalControlForm').off('submit', UserParentalControlPage.onSubmit).on('submit', UserParentalControlPage.onSubmit);

    }).on('pageshow', "#userParentalControlPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");
        var promise1 = ApiClient.getUser(userId);
        var promise2 = ApiClient.getParentalRatings();

        Promise.all([promise1, promise2]).then(function (responses) {

            loadUser(page, responses[0], responses[1]);

        });
    });

});