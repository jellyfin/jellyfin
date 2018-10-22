define(["globalize", "dom", "emby-checkbox", "emby-select", "emby-input"], function(globalize, dom) {
    "use strict";

    function populateLanguages(parent) {
        return ApiClient.getCultures().then(function(languages) {
            populateLanguagesIntoSelect(parent.querySelector("#selectLanguage"), languages), populateLanguagesIntoList(parent.querySelector(".subtitleDownloadLanguages"), languages)
        })
    }

    function populateLanguagesIntoSelect(select, languages) {
        var html = "";
        html += "<option value=''></option>";
        for (var i = 0, length = languages.length; i < length; i++) {
            var culture = languages[i];
            html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>"
        }
        select.innerHTML = html
    }

    function populateLanguagesIntoList(element, languages) {
        for (var html = "", i = 0, length = languages.length; i < length; i++) {
            var culture = languages[i];
            html += '<label><input type="checkbox" is="emby-checkbox" class="chkSubtitleLanguage" data-lang="' + culture.ThreeLetterISOLanguageName.toLowerCase() + '" /><span>' + culture.DisplayName + "</span></label>"
        }
        element.innerHTML = html
    }

    function populateCountries(select) {
        return ApiClient.getCountries().then(function(allCountries) {
            var html = "";
            html += "<option value=''></option>";
            for (var i = 0, length = allCountries.length; i < length; i++) {
                var culture = allCountries[i];
                html += "<option value='" + culture.TwoLetterISORegionName + "'>" + culture.DisplayName + "</option>"
            }
            select.innerHTML = html
        })
    }

    function populateRefreshInterval(select) {
        var html = "";
        html += "<option value='0'>" + globalize.translate("Never") + "</option>", html += [30, 60, 90].map(function(val) {
            return "<option value='" + val + "'>" + globalize.translate("EveryNDays", val) + "</option>"
        }).join(""), select.innerHTML = html
    }

    function renderMetadataReaders(page, plugins) {
        var html = "",
            elem = page.querySelector(".metadataReaders");
        if (plugins.length < 1) return elem.innerHTML = "", elem.classList.add("hide"), !1;
        html += '<h3 class="checkboxListLabel">' + globalize.translate("LabelMetadataReaders") + "</h3>", html += '<div class="checkboxList paperList checkboxList-paperList">';
        for (var i = 0, length = plugins.length; i < length; i++) {
            var plugin = plugins[i];
            html += '<div class="listItem localReaderOption sortableOption" data-pluginname="' + plugin.Name + '">', html += '<i class="listItemIcon md-icon">live_tv</i>', html += '<div class="listItemBody">', html += '<h3 class="listItemBodyText">', html += plugin.Name, html += "</h3>", html += "</div>", i > 0 ? html += '<button type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonUp") + '" class="btnSortableMoveUp btnSortable" data-pluginindex="' + i + '"><i class="md-icon">keyboard_arrow_up</i></button>' : plugins.length > 1 && (html += '<button type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonDown") + '" class="btnSortableMoveDown btnSortable" data-pluginindex="' + i + '"><i class="md-icon">keyboard_arrow_down</i></button>'), html += "</div>"
        }
        return html += "</div>", html += '<div class="fieldDescription">' + globalize.translate("LabelMetadataReadersHelp") + "</div>", plugins.length < 2 ? elem.classList.add("hide") : elem.classList.remove("hide"), elem.innerHTML = html, !0
    }

    function renderMetadataSavers(page, metadataSavers) {
        var html = "",
            elem = page.querySelector(".metadataSavers");
        if (!metadataSavers.length) return elem.innerHTML = "", elem.classList.add("hide"), !1;
        html += '<h3 class="checkboxListLabel">' + globalize.translate("LabelMetadataSavers") + "</h3>", html += '<div class="checkboxList paperList checkboxList-paperList">';
        for (var i = 0, length = metadataSavers.length; i < length; i++) {
            var plugin = metadataSavers[i];
            html += '<label><input type="checkbox" data-defaultenabled="' + plugin.DefaultEnabled + '" is="emby-checkbox" class="chkMetadataSaver" data-pluginname="' + plugin.Name + '" ' + !1 + "><span>" + plugin.Name + "</span></label>"
        }
        return html += "</div>", html += '<div class="fieldDescription" style="margin-top:.25em;">' + globalize.translate("LabelMetadataSaversHelp") + "</div>", elem.innerHTML = html, elem.classList.remove("hide"), !0
    }

    function getMetadataFetchersForTypeHtml(availableTypeOptions, libraryOptionsForType) {
        var html = "",
            plugins = availableTypeOptions.MetadataFetchers;
        if (plugins = getOrderedPlugins(plugins, libraryOptionsForType.MetadataFetcherOrder || []), !plugins.length) return html;
        html += '<div class="metadataFetcher" data-type="' + availableTypeOptions.Type + '">', html += '<h3 class="checkboxListLabel">' + globalize.translate("LabelTypeMetadataDownloaders", availableTypeOptions.Type) + "</h3>", html += '<div class="checkboxList paperList checkboxList-paperList">';
        for (var i = 0, length = plugins.length; i < length; i++) {
            var plugin = plugins[i];
            html += '<div class="listItem metadataFetcherItem sortableOption" data-pluginname="' + plugin.Name + '">';
            var isChecked = libraryOptionsForType.MetadataFetchers ? -1 !== libraryOptionsForType.MetadataFetchers.indexOf(plugin.Name) : plugin.DefaultEnabled,
                checkedHtml = isChecked ? ' checked="checked"' : "";
            html += '<label class="listItemCheckboxContainer"><input type="checkbox" is="emby-checkbox" class="chkMetadataFetcher" data-pluginname="' + plugin.Name + '" ' + checkedHtml + "><span></span></label>", html += '<div class="listItemBody">', html += '<h3 class="listItemBodyText">', html += plugin.Name, html += "</h3>", html += "</div>", i > 0 ? html += '<button type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonUp") + '" class="btnSortableMoveUp btnSortable" data-pluginindex="' + i + '"><i class="md-icon">keyboard_arrow_up</i></button>' : plugins.length > 1 && (html += '<button type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonDown") + '" class="btnSortableMoveDown btnSortable" data-pluginindex="' + i + '"><i class="md-icon">keyboard_arrow_down</i></button>'), html += "</div>"
        }
        return html += "</div>", html += '<div class="fieldDescription">' + globalize.translate("LabelMetadataDownloadersHelp") + "</div>", html += "</div>"
    }

    function getTypeOptions(allOptions, type) {
        for (var allTypeOptions = allOptions.TypeOptions || [], i = 0, length = allTypeOptions.length; i < length; i++) {
            var typeOptions = allTypeOptions[i];
            if (typeOptions.Type === type) return typeOptions
        }
        return null
    }

    function renderMetadataFetchers(page, availableOptions, libraryOptions) {
        for (var html = "", elem = page.querySelector(".metadataFetchers"), i = 0, length = availableOptions.TypeOptions.length; i < length; i++) {
            var availableTypeOptions = availableOptions.TypeOptions[i];
            html += getMetadataFetchersForTypeHtml(availableTypeOptions, getTypeOptions(libraryOptions, availableTypeOptions.Type) || {})
        }
        return elem.innerHTML = html, html ? (elem.classList.remove("hide"), page.querySelector(".fldAutoRefreshInterval").classList.remove("hide"), page.querySelector(".fldMetadataLanguage").classList.remove("hide"), page.querySelector(".fldMetadataCountry").classList.remove("hide")) : (elem.classList.add("hide"), page.querySelector(".fldAutoRefreshInterval").classList.add("hide"), page.querySelector(".fldMetadataLanguage").classList.add("hide"), page.querySelector(".fldMetadataCountry").classList.add("hide")), !0
    }

    function renderSubtitleFetchers(page, availableOptions, libraryOptions) {
        try {
            var html = "",
                elem = page.querySelector(".subtitleFetchers"),
                html = "",
                plugins = availableOptions.SubtitleFetchers;
            if (plugins = getOrderedPlugins(plugins, libraryOptions.SubtitleFetcherOrder || []), !plugins.length) return html;
            html += '<h3 class="checkboxListLabel">' + globalize.translate("LabelSubtitleDownloaders") + "</h3>", html += '<div class="checkboxList paperList checkboxList-paperList">';
            for (var i = 0, length = plugins.length; i < length; i++) {
                var plugin = plugins[i];
                html += '<div class="listItem subtitleFetcherItem sortableOption" data-pluginname="' + plugin.Name + '">';
                var isChecked = libraryOptions.DisabledSubtitleFetchers ? -1 === libraryOptions.DisabledSubtitleFetchers.indexOf(plugin.Name) : plugin.DefaultEnabled,
                    checkedHtml = isChecked ? ' checked="checked"' : "";
                html += '<label class="listItemCheckboxContainer"><input type="checkbox" is="emby-checkbox" class="chkSubtitleFetcher" data-pluginname="' + plugin.Name + '" ' + checkedHtml + "><span></span></label>", html += '<div class="listItemBody">', html += '<h3 class="listItemBodyText">', html += plugin.Name, html += "</h3>", "Open Subtitles" === plugin.Name && (html += '<div class="listItemBodyText secondary">', html += globalize.translate("OpenSubtitleInstructions"), html += "</div>"), html += "</div>", i > 0 ? html += '<button type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonUp") + '" class="btnSortableMoveUp btnSortable" data-pluginindex="' + i + '"><i class="md-icon">keyboard_arrow_up</i></button>' : plugins.length > 1 && (html += '<button type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonDown") + '" class="btnSortableMoveDown btnSortable" data-pluginindex="' + i + '"><i class="md-icon">keyboard_arrow_down</i></button>'), html += "</div>"
            }
            html += "</div>", html += '<div class="fieldDescription">' + globalize.translate("SubtitleDownloadersHelp") + "</div>", elem.innerHTML = html
        } catch (err) {
            alert(err)
        }
    }

    function getImageFetchersForTypeHtml(availableTypeOptions, libraryOptionsForType) {
        var html = "",
            plugins = availableTypeOptions.ImageFetchers;
        if (plugins = getOrderedPlugins(plugins, libraryOptionsForType.ImageFetcherOrder || []), !plugins.length) return html;
        html += '<div class="imageFetcher" data-type="' + availableTypeOptions.Type + '">', html += '<div class="flex align-items-center" style="margin:1.5em 0 .5em;">', html += '<h3 class="checkboxListLabel" style="margin:0;">' + globalize.translate("HeaderTypeImageFetchers", availableTypeOptions.Type) + "</h3>";
        var supportedImageTypes = availableTypeOptions.SupportedImageTypes || [];
        (supportedImageTypes.length > 1 || 1 === supportedImageTypes.length && "Primary" !== supportedImageTypes[0]) && (html += '<button is="emby-button" class="raised btnImageOptionsForType" type="button" style="margin-left:1.5em;font-size:90%;"><span>' + globalize.translate("HeaderFetcherSettings") + "</span></button>"), html += "</div>", html += '<div class="checkboxList paperList checkboxList-paperList">';
        for (var i = 0, length = plugins.length; i < length; i++) {
            var plugin = plugins[i];
            html += '<div class="listItem imageFetcherItem sortableOption" data-pluginname="' + plugin.Name + '">';
            var isChecked = libraryOptionsForType.ImageFetchers ? -1 !== libraryOptionsForType.ImageFetchers.indexOf(plugin.Name) : plugin.DefaultEnabled,
                checkedHtml = isChecked ? ' checked="checked"' : "";
            html += '<label class="listItemCheckboxContainer"><input type="checkbox" is="emby-checkbox" class="chkImageFetcher" data-pluginname="' + plugin.Name + '" ' + checkedHtml + "><span></span></label>", html += '<div class="listItemBody">', html += '<h3 class="listItemBodyText">', html += plugin.Name, html += "</h3>", html += "</div>", i > 0 ? html += '<button type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonUp") + '" class="btnSortableMoveUp btnSortable" data-pluginindex="' + i + '"><i class="md-icon">keyboard_arrow_up</i></button>' : plugins.length > 1 && (html += '<button type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonDown") + '" class="btnSortableMoveDown btnSortable" data-pluginindex="' + i + '"><i class="md-icon">keyboard_arrow_down</i></button>'), html += "</div>"
        }
        return html += "</div>", html += '<div class="fieldDescription">' + globalize.translate("LabelImageFetchersHelp") + "</div>", html += "</div>"
    }

    function renderImageFetchers(page, availableOptions, libraryOptions) {
        for (var html = "", elem = page.querySelector(".imageFetchers"), i = 0, length = availableOptions.TypeOptions.length; i < length; i++) {
            var availableTypeOptions = availableOptions.TypeOptions[i];
            html += getImageFetchersForTypeHtml(availableTypeOptions, getTypeOptions(libraryOptions, availableTypeOptions.Type) || {})
        }
        return elem.innerHTML = html, html ? (elem.classList.remove("hide"), page.querySelector(".chkDownloadImagesInAdvanceContainer").classList.remove("hide"), page.querySelector(".chkSaveLocalContainer").classList.remove("hide")) : (elem.classList.add("hide"), page.querySelector(".chkDownloadImagesInAdvanceContainer").classList.add("hide"), page.querySelector(".chkSaveLocalContainer").classList.add("hide")), !0
    }

    function populateMetadataSettings(parent, contentType, isNewLibrary) {
        var isNewLibrary = parent.classList.contains("newlibrary");
        return ApiClient.getJSON(ApiClient.getUrl("Libraries/AvailableOptions", {
            LibraryContentType: contentType,
            IsNewLibrary: isNewLibrary
        })).then(function(availableOptions) {
            currentAvailableOptions = availableOptions, parent.availableOptions = availableOptions, renderMetadataSavers(parent, availableOptions.MetadataSavers), renderMetadataReaders(parent, availableOptions.MetadataReaders), renderMetadataFetchers(parent, availableOptions, {}), renderSubtitleFetchers(parent, availableOptions, {}), renderImageFetchers(parent, availableOptions, {}), availableOptions.SubtitleFetchers.length ? parent.querySelector(".subtitleDownloadSettings").classList.remove("hide") : parent.querySelector(".subtitleDownloadSettings").classList.add("hide")
        }).catch(function() {
            return Promise.resolve()
        })
    }

    function adjustSortableListElement(elem) {
        var btnSortable = elem.querySelector(".btnSortable");
        elem.previousSibling ? (btnSortable.classList.add("btnSortableMoveUp"), btnSortable.classList.remove("btnSortableMoveDown"), btnSortable.querySelector("i").innerHTML = "keyboard_arrow_up") : (btnSortable.classList.remove("btnSortableMoveUp"), btnSortable.classList.add("btnSortableMoveDown"), btnSortable.querySelector("i").innerHTML = "keyboard_arrow_down")
    }

    function showImageOptionsForType(type) {
        require(["imageoptionseditor"], function(ImageOptionsEditor) {
            var typeOptions = getTypeOptions(currentLibraryOptions, type);
            typeOptions || (typeOptions = {
                Type: type
            }, currentLibraryOptions.TypeOptions.push(typeOptions));
            var availableOptions = getTypeOptions(currentAvailableOptions || {}, type);
            (new ImageOptionsEditor).show(type, typeOptions, availableOptions)
        })
    }

    function onImageFetchersContainerClick(e) {
        var btnImageOptionsForType = dom.parentWithClass(e.target, "btnImageOptionsForType");
        if (btnImageOptionsForType) {
            return void showImageOptionsForType(dom.parentWithClass(btnImageOptionsForType, "imageFetcher").getAttribute("data-type"))
        }
        onSortableContainerClick.call(this, e)
    }

    function onSortableContainerClick(e) {
        var btnSortable = dom.parentWithClass(e.target, "btnSortable");
        if (btnSortable) {
            var li = dom.parentWithClass(btnSortable, "sortableOption"),
                list = dom.parentWithClass(li, "paperList");
            if (btnSortable.classList.contains("btnSortableMoveDown")) {
                var next = li.nextSibling;
                next && (li.parentNode.removeChild(li), next.parentNode.insertBefore(li, next.nextSibling))
            } else {
                var prev = li.previousSibling;
                prev && (li.parentNode.removeChild(li), prev.parentNode.insertBefore(li, prev))
            }
            Array.prototype.forEach.call(list.querySelectorAll(".sortableOption"), adjustSortableListElement)
        }
    }

    function bindEvents(parent) {
        parent.querySelector(".metadataReaders").addEventListener("click", onSortableContainerClick), parent.querySelector(".subtitleFetchers").addEventListener("click", onSortableContainerClick), parent.querySelector(".metadataFetchers").addEventListener("click", onSortableContainerClick), parent.querySelector(".imageFetchers").addEventListener("click", onImageFetchersContainerClick)
    }

    function embed(parent, contentType, libraryOptions) {
        currentLibraryOptions = {
            TypeOptions: []
        }, currentAvailableOptions = null;
        var isNewLibrary = null == libraryOptions;
        return isNewLibrary && parent.classList.add("newlibrary"), new Promise(function(resolve, reject) {
            var xhr = new XMLHttpRequest;
            xhr.open("GET", "components/libraryoptionseditor/libraryoptionseditor.template.html", !0), xhr.onload = function(e) {
                var template = this.response;
                parent.innerHTML = globalize.translateDocument(template), populateRefreshInterval(parent.querySelector("#selectAutoRefreshInterval"));
                var promises = [populateLanguages(parent), populateCountries(parent.querySelector("#selectCountry"))];
                Promise.all(promises).then(function() {
                    return setContentType(parent, contentType).then(function() {
                        libraryOptions && setLibraryOptions(parent, libraryOptions), bindEvents(parent), resolve()
                    })
                })
            }, xhr.send()
        })
    }

    function setAdvancedVisible(parent, visible) {
        for (var elems = parent.querySelectorAll(".advanced"), i = 0, length = elems.length; i < length; i++) visible ? elems[i].classList.remove("advancedHide") : elems[i].classList.add("advancedHide")
    }

    function setContentType(parent, contentType) {
        return "homevideos" === contentType || "photos" === contentType ? parent.querySelector(".chkEnablePhotosContainer").classList.remove("hide") : parent.querySelector(".chkEnablePhotosContainer").classList.add("hide"), "tvshows" !== contentType && "movies" !== contentType && "homevideos" !== contentType && "musicvideos" !== contentType && "mixed" !== contentType && contentType ? parent.querySelector(".chapterSettingsSection").classList.add("hide") : parent.querySelector(".chapterSettingsSection").classList.remove("hide"), "tvshows" === contentType ? (parent.querySelector(".chkImportMissingEpisodesContainer").classList.remove("hide"), parent.querySelector(".chkAutomaticallyGroupSeriesContainer").classList.remove("hide"), parent.querySelector(".fldSeasonZeroDisplayName").classList.remove("hide"), parent.querySelector("#txtSeasonZeroName").setAttribute("required", "required")) : (parent.querySelector(".chkImportMissingEpisodesContainer").classList.add("hide"), parent.querySelector(".chkAutomaticallyGroupSeriesContainer").classList.add("hide"), parent.querySelector(".fldSeasonZeroDisplayName").classList.add("hide"), parent.querySelector("#txtSeasonZeroName").removeAttribute("required")), "games" === contentType || "books" === contentType || "boxsets" === contentType || "playlists" === contentType || "music" === contentType ? parent.querySelector(".chkEnableEmbeddedTitlesContainer").classList.add("hide") : parent.querySelector(".chkEnableEmbeddedTitlesContainer").classList.remove("hide"), populateMetadataSettings(parent, contentType)
    }

    function setSubtitleFetchersIntoOptions(parent, options) {
        options.DisabledSubtitleFetchers = Array.prototype.map.call(Array.prototype.filter.call(parent.querySelectorAll(".chkSubtitleFetcher"), function(elem) {
            return !elem.checked
        }), function(elem) {
            return elem.getAttribute("data-pluginname")
        }), options.SubtitleFetcherOrder = Array.prototype.map.call(parent.querySelectorAll(".subtitleFetcherItem"), function(elem) {
            return elem.getAttribute("data-pluginname")
        })
    }

    function setMetadataFetchersIntoOptions(parent, options) {
        for (var sections = parent.querySelectorAll(".metadataFetcher"), i = 0, length = sections.length; i < length; i++) {
            var section = sections[i],
                type = section.getAttribute("data-type"),
                typeOptions = getTypeOptions(options, type);
            typeOptions || (typeOptions = {
                Type: type
            }, options.TypeOptions.push(typeOptions)), typeOptions.MetadataFetchers = Array.prototype.map.call(Array.prototype.filter.call(section.querySelectorAll(".chkMetadataFetcher"), function(elem) {
                return elem.checked
            }), function(elem) {
                return elem.getAttribute("data-pluginname")
            }), typeOptions.MetadataFetcherOrder = Array.prototype.map.call(section.querySelectorAll(".metadataFetcherItem"), function(elem) {
                return elem.getAttribute("data-pluginname")
            })
        }
    }

    function setImageFetchersIntoOptions(parent, options) {
        for (var sections = parent.querySelectorAll(".imageFetcher"), i = 0, length = sections.length; i < length; i++) {
            var section = sections[i],
                type = section.getAttribute("data-type"),
                typeOptions = getTypeOptions(options, type);
            typeOptions || (typeOptions = {
                Type: type
            }, options.TypeOptions.push(typeOptions)), typeOptions.ImageFetchers = Array.prototype.map.call(Array.prototype.filter.call(section.querySelectorAll(".chkImageFetcher"), function(elem) {
                return elem.checked
            }), function(elem) {
                return elem.getAttribute("data-pluginname")
            }), typeOptions.ImageFetcherOrder = Array.prototype.map.call(section.querySelectorAll(".imageFetcherItem"), function(elem) {
                return elem.getAttribute("data-pluginname")
            })
        }
    }

    function setImageOptionsIntoOptions(parent, options) {
        for (var originalTypeOptions = (currentLibraryOptions || {}).TypeOptions || [], i = 0, length = originalTypeOptions.length; i < length; i++) {
            var originalTypeOption = originalTypeOptions[i],
                typeOptions = getTypeOptions(options, originalTypeOption.Type);
            typeOptions || (typeOptions = {
                Type: type
            }, options.TypeOptions.push(typeOptions)), originalTypeOption.ImageOptions && (typeOptions.ImageOptions = originalTypeOption.ImageOptions)
        }
    }

    function getLibraryOptions(parent) {
        var options = {
            EnableArchiveMediaFiles: !1,
            EnablePhotos: parent.querySelector(".chkEnablePhotos").checked,
            EnableRealtimeMonitor: parent.querySelector(".chkEnableRealtimeMonitor").checked,
            ExtractChapterImagesDuringLibraryScan: parent.querySelector(".chkExtractChaptersDuringLibraryScan").checked,
            EnableChapterImageExtraction: parent.querySelector(".chkExtractChapterImages").checked,
            DownloadImagesInAdvance: parent.querySelector("#chkDownloadImagesInAdvance").checked,
            EnableInternetProviders: !0,
            ImportMissingEpisodes: parent.querySelector("#chkImportMissingEpisodes").checked,
            SaveLocalMetadata: parent.querySelector("#chkSaveLocal").checked,
            EnableAutomaticSeriesGrouping: parent.querySelector(".chkAutomaticallyGroupSeries").checked,
            PreferredMetadataLanguage: parent.querySelector("#selectLanguage").value,
            MetadataCountryCode: parent.querySelector("#selectCountry").value,
            SeasonZeroDisplayName: parent.querySelector("#txtSeasonZeroName").value,
            AutomaticRefreshIntervalDays: parseInt(parent.querySelector("#selectAutoRefreshInterval").value),
            EnableEmbeddedTitles: parent.querySelector("#chkEnableEmbeddedTitles").checked,
            SkipSubtitlesIfEmbeddedSubtitlesPresent: parent.querySelector("#chkSkipIfGraphicalSubsPresent").checked,
            SkipSubtitlesIfAudioTrackMatches: parent.querySelector("#chkSkipIfAudioTrackPresent").checked,
            SaveSubtitlesWithMedia: parent.querySelector("#chkSaveSubtitlesLocally").checked,
            RequirePerfectSubtitleMatch: parent.querySelector("#chkRequirePerfectMatch").checked,
            MetadataSavers: Array.prototype.map.call(Array.prototype.filter.call(parent.querySelectorAll(".chkMetadataSaver"), function(elem) {
                return elem.checked
            }), function(elem) {
                return elem.getAttribute("data-pluginname")
            }),
            TypeOptions: []
        };
        return options.LocalMetadataReaderOrder = Array.prototype.map.call(parent.querySelectorAll(".localReaderOption"), function(elem) {
            return elem.getAttribute("data-pluginname")
        }), options.SubtitleDownloadLanguages = Array.prototype.map.call(Array.prototype.filter.call(parent.querySelectorAll(".chkSubtitleLanguage"), function(elem) {
            return elem.checked
        }), function(elem) {
            return elem.getAttribute("data-lang")
        }), setSubtitleFetchersIntoOptions(parent, options), setMetadataFetchersIntoOptions(parent, options), setImageFetchersIntoOptions(parent, options), setImageOptionsIntoOptions(parent, options), options
    }

    function getOrderedPlugins(plugins, configuredOrder) {
        return plugins = plugins.slice(0), plugins.sort(function(a, b) {
            return a = configuredOrder.indexOf(a.Name), b = configuredOrder.indexOf(b.Name), a < b ? -1 : a > b ? 1 : 0
        }), plugins
    }

    function setLibraryOptions(parent, options) {
        currentLibraryOptions = options, currentAvailableOptions = parent.availableOptions, parent.querySelector("#selectLanguage").value = options.PreferredMetadataLanguage || "", parent.querySelector("#selectCountry").value = options.MetadataCountryCode || "", parent.querySelector("#selectAutoRefreshInterval").value = options.AutomaticRefreshIntervalDays || "0", parent.querySelector("#txtSeasonZeroName").value = options.SeasonZeroDisplayName || "Specials", parent.querySelector(".chkEnablePhotos").checked = options.EnablePhotos, parent.querySelector(".chkEnableRealtimeMonitor").checked = options.EnableRealtimeMonitor, parent.querySelector(".chkExtractChaptersDuringLibraryScan").checked = options.ExtractChapterImagesDuringLibraryScan, parent.querySelector(".chkExtractChapterImages").checked = options.EnableChapterImageExtraction, parent.querySelector("#chkDownloadImagesInAdvance").checked = options.DownloadImagesInAdvance, parent.querySelector("#chkSaveLocal").checked = options.SaveLocalMetadata, parent.querySelector("#chkImportMissingEpisodes").checked = options.ImportMissingEpisodes, parent.querySelector(".chkAutomaticallyGroupSeries").checked = options.EnableAutomaticSeriesGrouping, parent.querySelector("#chkEnableEmbeddedTitles").checked = options.EnableEmbeddedTitles, parent.querySelector("#chkSkipIfGraphicalSubsPresent").checked = options.SkipSubtitlesIfEmbeddedSubtitlesPresent, parent.querySelector("#chkSaveSubtitlesLocally").checked = options.SaveSubtitlesWithMedia, parent.querySelector("#chkSkipIfAudioTrackPresent").checked = options.SkipSubtitlesIfAudioTrackMatches, parent.querySelector("#chkRequirePerfectMatch").checked = options.RequirePerfectSubtitleMatch, Array.prototype.forEach.call(parent.querySelectorAll(".chkMetadataSaver"), function(elem) {
            elem.checked = options.MetadataSavers ? -1 !== options.MetadataSavers.indexOf(elem.getAttribute("data-pluginname")) : "true" === elem.getAttribute("data-defaultenabled")
        }), Array.prototype.forEach.call(parent.querySelectorAll(".chkSubtitleLanguage"), function(elem) {
            elem.checked = !!options.SubtitleDownloadLanguages && -1 !== options.SubtitleDownloadLanguages.indexOf(elem.getAttribute("data-lang"))
        }), renderMetadataReaders(parent, getOrderedPlugins(parent.availableOptions.MetadataReaders, options.LocalMetadataReaderOrder || [])), renderMetadataFetchers(parent, parent.availableOptions, options), renderImageFetchers(parent, parent.availableOptions, options), renderSubtitleFetchers(parent, parent.availableOptions, options)
    }
    var currentLibraryOptions, currentAvailableOptions;
    return {
        embed: embed,
        setContentType: setContentType,
        getLibraryOptions: getLibraryOptions,
        setLibraryOptions: setLibraryOptions,
        setAdvancedVisible: setAdvancedVisible
    }
});