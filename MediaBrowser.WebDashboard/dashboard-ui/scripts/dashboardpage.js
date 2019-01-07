define(["datetime", "events", "itemHelper", "serverNotifications", "dom", "globalize", "loading", "connectionManager", "playMethodHelper", "cardBuilder", "imageLoader", "components/activitylog", "humanedate", "listViewStyle", "emby-linkbutton", "flexStyles", "emby-button", "emby-itemscontainer"], function(datetime, events, itemHelper, serverNotifications, dom, globalize, loading, connectionManager, playMethodHelper, cardBuilder, imageLoader, ActivityLog) {
    "use strict";

    function onConnectionHelpClick(e) {
        return e.preventDefault(), !1
    }

    function buttonEnabled(elem, enabled) {
        enabled ? (elem.setAttribute("disabled", ""), elem.removeAttribute("disabled")) : elem.setAttribute("disabled", "disabled")
    }

    function onEditServerNameClick(e) {
        var page = dom.parentWithClass(this, "page");
        return require(["prompt"], function(prompt) {
            prompt({
                label: globalize.translate("LabelFriendlyServerName"),
                description: globalize.translate("LabelFriendlyServerNameHelp"),
                value: page.querySelector(".serverNameHeader").innerHTML,
                confirmText: globalize.translate("ButtonSave")
            }).then(function(value) {
                loading.show(), ApiClient.getServerConfiguration().then(function(config) {
                    config.ServerName = value, ApiClient.updateServerConfiguration(config).then(function() {
                        page.querySelector(".serverNameHeader").innerHTML = value, loading.hide()
                    })
                })
            })
        }), e.preventDefault(), !1
    }

    function showPlaybackInfo(btn, session) {
        require(["alert"], function(alert) {
            var showTranscodeReasons, title, text = [],
                displayPlayMethod = playMethodHelper.getDisplayPlayMethod(session),
                isDirectStream = "DirectStream" === displayPlayMethod,
                isTranscode = "Transcode" === displayPlayMethod;
            isDirectStream ? (title = globalize.translate("sharedcomponents#DirectStreaming"), text.push(globalize.translate("sharedcomponents#DirectStreamHelp1")), text.push("<br/>"), text.push(globalize.translate("sharedcomponents#DirectStreamHelp2"))) : isTranscode && (title = globalize.translate("sharedcomponents#Transcoding"), text.push(globalize.translate("sharedcomponents#MediaIsBeingConverted")), session.TranscodingInfo && session.TranscodingInfo.TranscodeReasons && session.TranscodingInfo.TranscodeReasons.length && (text.push("<br/>"), text.push(globalize.translate("sharedcomponents#LabelReasonForTranscoding")), showTranscodeReasons = !0)), showTranscodeReasons && session.TranscodingInfo.TranscodeReasons.forEach(function(t) {
                text.push(globalize.translate("sharedcomponents#" + t))
            }), alert({
                text: text.join("<br/>"),
                title: title
            })
        })
    }

    function showSendMessageForm(btn, session) {
        require(["prompt"], function(prompt) {
            prompt({
                title: globalize.translate("HeaderSendMessage"),
                label: globalize.translate("LabelMessageText"),
                confirmText: globalize.translate("ButtonSend")
            }).then(function(text) {
                if (text) {
                    connectionManager.getApiClient(session.ServerId).sendMessageCommand(session.Id, {
                        Text: text,
                        TimeoutMs: 5e3
                    })
                }
            })
        })
    }

    function showOptionsMenu(btn, session) {
        require(["actionsheet"], function(actionsheet) {
            var menuItems = [];
            return session.ServerId && session.DeviceId !== connectionManager.deviceId() && menuItems.push({
                name: globalize.translate("SendMessage"),
                id: "sendmessage"
            }), session.TranscodingInfo && session.TranscodingInfo.TranscodeReasons && session.TranscodingInfo.TranscodeReasons.length && menuItems.push({
                name: globalize.translate("ViewPlaybackInfo"),
                id: "transcodinginfo"
            }), actionsheet.show({
                items: menuItems,
                positionTo: btn
            }).then(function(id) {
                switch (id) {
                    case "sendmessage":
                        showSendMessageForm(btn, session);
                        break;
                    case "transcodinginfo":
                        showPlaybackInfo(btn, session)
                }
            })
        })
    }

    function onActiveDevicesClick(e) {
        var btn = dom.parentWithClass(e.target, "sessionCardButton");
        if (btn) {
            var card = dom.parentWithClass(btn, "card");
            if (card) {
                var sessionId = card.id,
                    session = (DashboardPage.sessionsList || []).filter(function(s) {
                        return "session" + s.Id === sessionId
                    })[0];
                session && (btn.classList.contains("btnCardOptions") ? showOptionsMenu(btn, session) : btn.classList.contains("btnSessionInfo") ? showPlaybackInfo(btn, session) : btn.classList.contains("btnSessionSendMessage") ? showSendMessageForm(btn, session) : btn.classList.contains("btnSessionStop") ? connectionManager.getApiClient(session.ServerId).sendPlayStateCommand(session.Id, "Stop") : btn.classList.contains("btnSessionPlayPause") && session.PlayState && connectionManager.getApiClient(session.ServerId).sendPlayStateCommand(session.Id, "PlayPause"))
            }
        }
    }

    function filterSessions(sessions) {
        for (var list = [], minActiveDate = (new Date).getTime() - 9e5, i = 0, length = sessions.length; i < length; i++) {
            var session = sessions[i];
            if (session.NowPlayingItem || session.UserId) {
                datetime.parseISO8601Date(session.LastActivityDate, !0).getTime() >= minActiveDate && list.push(session)
            }
        }
        return list
    }

    function getPluginSecurityInfo() {
        var apiClient = window.ApiClient;
        return apiClient ? connectionManager.getRegistrationInfo("themes", apiClient, {
            viewOnly: !0
        }).then(function(result) {
            return {
                IsMBSupporter: !0
            }
        }, function() {
            return {
                IsMBSupporter: !1
            }
        }) : Promise.reject()
    }

    function refreshActiveRecordings(view, apiClient) {
        apiClient.getLiveTvRecordings({
            UserId: Dashboard.getCurrentUserId(),
            IsInProgress: !0,
            Fields: "CanDelete,PrimaryImageAspectRatio",
            EnableTotalRecordCount: !1,
            EnableImageTypes: "Primary,Thumb,Backdrop"
        }).then(function(result) {
            var itemsContainer = view.querySelector(".activeRecordingItems");
            if (!result.Items.length) return view.querySelector(".activeRecordingsSection").classList.add("hide"), void(itemsContainer.innerHTML = "");
            view.querySelector(".activeRecordingsSection").classList.remove("hide");
            itemsContainer.innerHTML = cardBuilder.getCardsHtml({
                items: result.Items,
                shape: "auto",
                defaultShape: "backdrop",
                showTitle: !0,
                showParentTitle: !0,
                coverImage: !0,
                cardLayout: !1,
                centerText: !0,
                preferThumb: "auto",
                overlayText: !1,
                overlayMoreButton: !0,
                action: "none",
                centerPlayButton: !0
            }), imageLoader.lazyChildren(itemsContainer)
        })
    }

    function renderHasPendingRestart(view, apiClient, hasPendingRestart) {
    }

    function reloadSystemInfo(view, apiClient) {
        apiClient.getSystemInfo().then(function(systemInfo) {
            view.querySelector(".serverNameHeader").innerHTML = systemInfo.ServerName;
            var localizedVersion = globalize.translate("LabelVersionNumber", systemInfo.Version);
            systemInfo.SystemUpdateLevel && "Release" != systemInfo.SystemUpdateLevel && (localizedVersion += " " + globalize.translate("Option" + systemInfo.SystemUpdateLevel).toLowerCase()), systemInfo.CanSelfRestart ? view.querySelector("#btnRestartServer").classList.remove("hide") : view.querySelector("#btnRestartServer").classList.add("hide"), view.querySelector("#appVersionNumber").innerHTML = localizedVersion, systemInfo.SupportsHttps ? view.querySelector("#ports").innerHTML = globalize.translate("LabelRunningOnPorts", systemInfo.HttpServerPortNumber, systemInfo.HttpsPortNumber) : view.querySelector("#ports").innerHTML = globalize.translate("LabelRunningOnPort", systemInfo.HttpServerPortNumber), DashboardPage.renderUrls(view, systemInfo), DashboardPage.renderPendingInstallations(view, systemInfo), systemInfo.CanSelfUpdate ? (view.querySelector("#btnUpdateApplicationContainer").classList.remove("hide"), view.querySelector("#btnManualUpdateContainer").classList.add("hide")) : (view.querySelector("#btnUpdateApplicationContainer").classList.add("hide"), view.querySelector("#btnManualUpdateContainer").classList.remove("hide")), "synology" == systemInfo.PackageName ? view.querySelector("#btnManualUpdateContainer").innerHTML = globalize.translate("SynologyUpdateInstructions") : view.querySelector("#btnManualUpdateContainer").innerHTML = '<a href="https://github.com/jellyfin/jellyfin/download" target="_blank">' + globalize.translate("PleaseUpdateManually") + "</a>", DashboardPage.renderPaths(view, systemInfo), renderHasPendingRestart(view, apiClient, systemInfo.HasPendingRestart)
        })
    }

    function renderInfo(view, sessions, forceUpdate) {
        sessions = filterSessions(sessions), renderActiveConnections(view, sessions), DashboardPage.renderPluginUpdateInfo(view, forceUpdate), loading.hide()
    }

    function pollForInfo(view, apiClient, forceUpdate) {
        apiClient.getSessions({
            ActiveWithinSeconds: 960
        }).then(function(sessions) {
            renderInfo(view, sessions, forceUpdate)
        }), apiClient.getScheduledTasks().then(function(tasks) {
            renderRunningTasks(view, tasks)
        })
    }

    function renderActiveConnections(view, sessions) {
        var html = "";
        DashboardPage.sessionsList = sessions;
        var parentElement = view.querySelector(".activeDevices"),
            cardElem = parentElement.querySelector(".card");
        cardElem && cardElem.classList.add("deadSession");
        for (var i = 0, length = sessions.length; i < length; i++) {
            var session = sessions[i],
                rowId = "session" + session.Id,
                elem = view.querySelector("#" + rowId);
            if (elem) DashboardPage.updateSession(elem, session);
            else {
                var nowPlayingItem = session.NowPlayingItem,
                    className = "scalableCard card activeSession backdropCard backdropCard-scalable";
                session.TranscodingInfo && session.TranscodingInfo.CompletionPercentage && (className += " transcodingSession"), html += '<div class="' + className + '" id="' + rowId + '">', html += '<div class="cardBox visualCardBox">', html += '<div class="cardScalable visualCardBox-cardScalable">', html += '<div class="cardPadder cardPadder-backdrop"></div>', html += '<div class="cardContent">';
                var imgUrl = DashboardPage.getNowPlayingImageUrl(nowPlayingItem);
                imgUrl ? (html += '<div class="sessionNowPlayingContent sessionNowPlayingContent-withbackground"', html += ' data-src="' + imgUrl + '" style="display:inline-block;background-image:url(\'' + imgUrl + "');\"") : html += '<div class="sessionNowPlayingContent"', html += "></div>", html += '<div class="sessionNowPlayingInnerContent">', html += '<div class="sessionAppInfo">';
                var clientImage = DashboardPage.getClientImage(session);
                clientImage && (html += clientImage), html += '<div class="sessionAppName" style="display:inline-block;">', html += '<div class="sessionDeviceName">' + session.DeviceName + "</div>", html += '<div class="sessionAppSecondaryText">' + DashboardPage.getAppSecondaryText(session) + "</div>", html += "</div>", html += "</div>", html += '<div class="sessionNowPlayingTime">' + DashboardPage.getSessionNowPlayingTime(session) + "</div>", session.TranscodingInfo && session.TranscodingInfo.Framerate ? html += '<div class="sessionTranscodingFramerate">' + session.TranscodingInfo.Framerate + " fps</div>" : html += '<div class="sessionTranscodingFramerate"></div>';
                var nowPlayingName = DashboardPage.getNowPlayingName(session);
                if (html += '<div class="sessionNowPlayingInfo" data-imgsrc="' + nowPlayingName.image + '">', html += nowPlayingName.html, html += "</div>", nowPlayingItem && nowPlayingItem.RunTimeTicks) {
                    html += '<progress class="playbackProgress" min="0" max="100" value="' + 100 * (session.PlayState.PositionTicks || 0) / nowPlayingItem.RunTimeTicks + '"></progress>'
                } else html += '<progress class="playbackProgress hide" min="0" max="100"></progress>';
                session.TranscodingInfo && session.TranscodingInfo.CompletionPercentage ? html += '<progress class="transcodingProgress" min="0" max="100" value="' + session.TranscodingInfo.CompletionPercentage.toFixed(1) + '"></progress>' : html += '<progress class="transcodingProgress hide" min="0" max="100"></progress>', html += "</div>", html += "</div>", html += "</div>", html += '<div class="sessionCardFooter cardFooter">', html += '<div class="sessionCardButtons flex align-items-center justify-content-center">';
                var btnCssClass;
                btnCssClass = session.ServerId && session.NowPlayingItem && session.SupportsRemoteControl && session.DeviceId !== connectionManager.deviceId() ? "" : " hide", html += '<button is="paper-icon-button-light" class="sessionCardButton btnSessionPlayPause paper-icon-button-light ' + btnCssClass + '"><i class="md-icon">&#xE034;</i></button>', html += '<button is="paper-icon-button-light" class="sessionCardButton btnSessionStop paper-icon-button-light ' + btnCssClass + '"><i class="md-icon">&#xE047;</i></button>', btnCssClass = session.TranscodingInfo && session.TranscodingInfo.TranscodeReasons && session.TranscodingInfo && session.TranscodingInfo.TranscodeReasons.length ? "" : " hide", html += '<button is="paper-icon-button-light" class="sessionCardButton btnSessionInfo paper-icon-button-light ' + btnCssClass + '" title="' + globalize.translate("ViewPlaybackInfo") + '"><i class="md-icon">&#xE88E;</i></button>', btnCssClass = session.ServerId && -1 !== session.SupportedCommands.indexOf("DisplayMessage") && session.DeviceId !== connectionManager.deviceId() ? "" : " hide", html += '<button is="paper-icon-button-light" class="sessionCardButton btnSessionSendMessage paper-icon-button-light ' + btnCssClass + '" title="' + globalize.translate("SendMessage") + '"><i class="md-icon">&#xE0C9;</i></button>', html += "</div>", html += '<div class="sessionNowPlayingStreamInfo" style="padding:.5em 0 1em;">', html += DashboardPage.getSessionNowPlayingStreamInfo(session), html += "</div>", html += '<div class="flex align-items-center justify-content-center">';
                var userImage = DashboardPage.getUserImage(session);
                html += userImage ? '<img style="height:1.71em;border-radius:50px;margin-right:.5em;" src="' + userImage + '" />' : '<div style="height:1.71em;"></div>', html += '<div class="sessionUserName" style="text-transform:uppercase;">', html += DashboardPage.getUsersHtml(session) || "&nbsp;", html += "</div>", html += "</div>", html += "</div>", html += "</div>", html += "</div>"
            }
        }
        parentElement.insertAdjacentHTML("beforeend", html);
        var deadSessionElem = parentElement.querySelector(".deadSession");
        deadSessionElem && deadSessionElem.parentNode.removeChild(deadSessionElem)
    }

    function renderRunningTasks(view, tasks) {
        var html = "";
        tasks = tasks.filter(function(t) {
            return "Idle" != t.State && !t.IsHidden
        }), tasks.length ? view.querySelector(".runningTasksContainer").classList.remove("hide") : view.querySelector(".runningTasksContainer").classList.add("hide"), tasks.filter(function(t) {
            return t.Key == DashboardPage.systemUpdateTaskKey
        }).length ? buttonEnabled(view.querySelector("#btnUpdateApplication"), !1) : buttonEnabled(view.querySelector("#btnUpdateApplication"), !0);
        for (var i = 0, length = tasks.length; i < length; i++) {
            var task = tasks[i];
            if (html += "<p>", html += task.Name + "<br/>", "Running" == task.State) {
                var progress = (task.CurrentProgressPercentage || 0).toFixed(1);
                html += '<progress max="100" value="' + progress + '" title="' + progress + '%">', html += progress + "%", html += "</progress>", html += "<span style='color:#009F00;margin-left:5px;margin-right:5px;'>" + progress + "%</span>", html += '<button type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonStop") + '" onclick="DashboardPage.stopTask(this, \'' + task.Id + '\');" class="autoSize"><i class="md-icon">cancel</i></button>'
            } else "Cancelling" == task.State && (html += '<span style="color:#cc0000;">' + globalize.translate("LabelStopping") + "</span>");
            html += "</p>"
        }
        view.querySelector("#divRunningTasks").innerHTML = html
    }
    return window.DashboardPage = {
            newsStartIndex: 0,
            renderPaths: function(page, systemInfo) {
                page.querySelector("#cachePath").innerHTML = systemInfo.CachePath, page.querySelector("#logPath").innerHTML = systemInfo.LogPath, page.querySelector("#transcodingTemporaryPath").innerHTML = systemInfo.TranscodingTempPath, page.querySelector("#metadataPath").innerHTML = systemInfo.InternalMetadataPath
            },
            reloadNews: function(page) {
                var query = {
                    StartIndex: DashboardPage.newsStartIndex,
                    Limit: 4
                };
            },
            startInterval: function(apiClient) {
                apiClient.sendMessage("SessionsStart", "0,1500"), apiClient.sendMessage("ScheduledTasksInfoStart", "0,1000")
            },
            stopInterval: function(apiClient) {
                apiClient.sendMessage("SessionsStop"), apiClient.sendMessage("ScheduledTasksInfoStop")
            },
            getSessionNowPlayingStreamInfo: function(session) {
                var html = "",
                    showTranscodingInfo = !1,
                    displayPlayMethod = playMethodHelper.getDisplayPlayMethod(session);
                if ("DirectStream" === displayPlayMethod ? (html += globalize.translate("sharedcomponents#DirectStreaming"), !0) : "Transcode" == displayPlayMethod ? (html += globalize.translate("sharedcomponents#Transcoding"), session.TranscodingInfo && session.TranscodingInfo.Framerate && (html += " (" + session.TranscodingInfo.Framerate + " fps)"), showTranscodingInfo = !0, !0) : "DirectPlay" == displayPlayMethod && (html += globalize.translate("sharedcomponents#DirectPlaying")), showTranscodingInfo) {
                    var line = [];
                    session.TranscodingInfo && (session.TranscodingInfo.Bitrate && (session.TranscodingInfo.Bitrate > 1e6 ? line.push((session.TranscodingInfo.Bitrate / 1e6).toFixed(1) + " Mbps") : line.push(Math.floor(session.TranscodingInfo.Bitrate / 1e3) + " kbps")), session.TranscodingInfo.Container && line.push(session.TranscodingInfo.Container), session.TranscodingInfo.VideoCodec && line.push(session.TranscodingInfo.VideoCodec), session.TranscodingInfo.AudioCodec && session.TranscodingInfo.AudioCodec != session.TranscodingInfo.Container && line.push(session.TranscodingInfo.AudioCodec)), line.length && (html += " - " + line.join(" "))
                }
                return html || "&nbsp;"
            },
            getSessionNowPlayingTime: function(session) {
                var nowPlayingItem = session.NowPlayingItem,
                    html = "";
                return nowPlayingItem ? (session.PlayState.PositionTicks ? html += datetime.getDisplayRunningTime(session.PlayState.PositionTicks) : html += "--:--:--", html += " / ", nowPlayingItem && nowPlayingItem.RunTimeTicks ? html += datetime.getDisplayRunningTime(nowPlayingItem.RunTimeTicks) : html += "--:--:--", html) : html
            },
            getAppSecondaryText: function(session) {
                return session.Client + " " + session.ApplicationVersion
            },
            getNowPlayingName: function(session) {
                var imgUrl = "",
                    nowPlayingItem = session.NowPlayingItem;
                if (!nowPlayingItem) return {
                    html: "Last seen " + humane_date(session.LastActivityDate),
                    image: imgUrl
                };
                var topText = itemHelper.getDisplayName(nowPlayingItem),
                    bottomText = "";
                return nowPlayingItem.Artists && nowPlayingItem.Artists.length ? (bottomText = topText, topText = nowPlayingItem.Artists[0]) : nowPlayingItem.SeriesName || nowPlayingItem.Album ? (bottomText = topText, topText = nowPlayingItem.SeriesName || nowPlayingItem.Album) : nowPlayingItem.ProductionYear && (bottomText = nowPlayingItem.ProductionYear), nowPlayingItem.ImageTags && nowPlayingItem.ImageTags.Logo ? imgUrl = ApiClient.getScaledImageUrl(nowPlayingItem.Id, {
                    tag: nowPlayingItem.ImageTags.Logo,
                    maxHeight: 24,
                    maxWidth: 130,
                    type: "Logo"
                }) : nowPlayingItem.ParentLogoImageTag && (imgUrl = ApiClient.getScaledImageUrl(nowPlayingItem.ParentLogoItemId, {
                    tag: nowPlayingItem.ParentLogoImageTag,
                    maxHeight: 24,
                    maxWidth: 130,
                    type: "Logo"
                })), imgUrl && (topText = '<img src="' + imgUrl + '" style="max-height:24px;max-width:130px;" />'), {
                    html: bottomText ? topText + "<br/>" + bottomText : topText,
                    image: imgUrl
                }
            },
            getUsersHtml: function(session) {
                var html = [];
                session.UserId && html.push(session.UserName);
                for (var i = 0, length = session.AdditionalUsers.length; i < length; i++) html.push(session.AdditionalUsers[i].UserName);
                return html.join(", ")
            },
            getUserImage: function(session) {
                return session.UserId && session.UserPrimaryImageTag ? ApiClient.getUserImageUrl(session.UserId, {
                    tag: session.UserPrimaryImageTag,
                    height: 24,
                    type: "Primary"
                }) : null
            },
            updateSession: function(row, session) {
                row.classList.remove("deadSession");
                var nowPlayingItem = session.NowPlayingItem;
                nowPlayingItem ? row.classList.add("playingSession") : row.classList.remove("playingSession"), session.ServerId && -1 !== session.SupportedCommands.indexOf("DisplayMessage") && session.DeviceId !== connectionManager.deviceId() ? row.querySelector(".btnSessionSendMessage").classList.remove("hide") : row.querySelector(".btnSessionSendMessage").classList.add("hide"), session.TranscodingInfo && session.TranscodingInfo.TranscodeReasons && session.TranscodingInfo && session.TranscodingInfo.TranscodeReasons.length ? row.querySelector(".btnSessionInfo").classList.remove("hide") : row.querySelector(".btnSessionInfo").classList.add("hide");
                var btnSessionPlayPause = row.querySelector(".btnSessionPlayPause");
                session.ServerId && nowPlayingItem && session.SupportsRemoteControl && session.DeviceId !== connectionManager.deviceId() ? (btnSessionPlayPause.classList.remove("hide"), row.querySelector(".btnSessionStop").classList.remove("hide")) : (btnSessionPlayPause.classList.add("hide"), row.querySelector(".btnSessionStop").classList.add("hide")), session.PlayState && session.PlayState.IsPaused ? btnSessionPlayPause.querySelector("i").innerHTML = "&#xE037;" : btnSessionPlayPause.querySelector("i").innerHTML = "&#xE034;", row.querySelector(".sessionNowPlayingStreamInfo").innerHTML = DashboardPage.getSessionNowPlayingStreamInfo(session), row.querySelector(".sessionNowPlayingTime").innerHTML = DashboardPage.getSessionNowPlayingTime(session), row.querySelector(".sessionUserName").innerHTML = DashboardPage.getUsersHtml(session) || "&nbsp;", row.querySelector(".sessionAppSecondaryText").innerHTML = DashboardPage.getAppSecondaryText(session), row.querySelector(".sessionTranscodingFramerate").innerHTML = session.TranscodingInfo && session.TranscodingInfo.Framerate ? session.TranscodingInfo.Framerate + " fps" : "";
                var nowPlayingName = DashboardPage.getNowPlayingName(session),
                    nowPlayingInfoElem = row.querySelector(".sessionNowPlayingInfo");
                nowPlayingName.image && nowPlayingName.image == nowPlayingInfoElem.getAttribute("data-imgsrc") || (nowPlayingInfoElem.innerHTML = nowPlayingName.html, nowPlayingInfoElem.setAttribute("data-imgsrc", nowPlayingName.image || ""));
                var playbackProgressElem = row.querySelector(".playbackProgress");
                if (playbackProgressElem)
                    if (nowPlayingItem && nowPlayingItem.RunTimeTicks) {
                        var position = session.PlayState.PositionTicks || 0,
                            value = 100 * position / nowPlayingItem.RunTimeTicks;
                        playbackProgressElem.classList.remove("hide"), playbackProgressElem.value = value
                    } else playbackProgressElem.classList.add("hide");
                var transcodingProgress = row.querySelector(".transcodingProgress");
                session.TranscodingInfo && session.TranscodingInfo.CompletionPercentage ? (row.classList.add("transcodingSession"), transcodingProgress.value = session.TranscodingInfo.CompletionPercentage, transcodingProgress.classList.remove("hide")) : (transcodingProgress.classList.add("hide"), row.classList.remove("transcodingSession"));
                var imgUrl = DashboardPage.getNowPlayingImageUrl(nowPlayingItem) || "",
                    imgElem = row.querySelector(".sessionNowPlayingContent");
                imgUrl != imgElem.getAttribute("data-src") && (imgElem.style.backgroundImage = imgUrl ? "url('" + imgUrl + "')" : "", imgElem.setAttribute("data-src", imgUrl), imgUrl ? imgElem.classList.add("sessionNowPlayingContent-withbackground") : imgElem.classList.remove("sessionNowPlayingContent-withbackground"))
            },
            getClientImage: function(connection) {
                var iconUrl = (connection.Client.toLowerCase(), connection.DeviceName.toLowerCase(), connection.AppIconUrl);
                return iconUrl ? (-1 === iconUrl.indexOf("://") && (iconUrl = ApiClient.getUrl(iconUrl)), "<img src='" + iconUrl + "' />") : null
            },
            getNowPlayingImageUrl: function(item) {
                if (item && item.BackdropImageTags && item.BackdropImageTags.length) return ApiClient.getScaledImageUrl(item.Id, {
                    type: "Backdrop",
                    width: 275,
                    tag: item.BackdropImageTags[0]
                });
                if (item && item.ParentBackdropImageTags && item.ParentBackdropImageTags.length) return ApiClient.getScaledImageUrl(item.ParentBackdropItemId, {
                    type: "Backdrop",
                    width: 275,
                    tag: item.ParentBackdropImageTags[0]
                });
                if (item && item.BackdropImageTag) return ApiClient.getScaledImageUrl(item.BackdropItemId, {
                    type: "Backdrop",
                    width: 275,
                    tag: item.BackdropImageTag
                });
                var imageTags = (item || {}).ImageTags || {};
                return item && imageTags.Thumb ? ApiClient.getScaledImageUrl(item.Id, {
                    type: "Thumb",
                    width: 275,
                    tag: imageTags.Thumb
                }) : item && item.ParentThumbImageTag ? ApiClient.getScaledImageUrl(item.ParentThumbItemId, {
                    type: "Thumb",
                    width: 275,
                    tag: item.ParentThumbImageTag
                }) : item && item.ThumbImageTag ? ApiClient.getScaledImageUrl(item.ThumbItemId, {
                    type: "Thumb",
                    width: 275,
                    tag: item.ThumbImageTag
                }) : item && imageTags.Primary ? ApiClient.getScaledImageUrl(item.Id, {
                    type: "Primary",
                    width: 275,
                    tag: imageTags.Primary
                }) : item && item.PrimaryImageTag ? ApiClient.getScaledImageUrl(item.PrimaryImageItemId, {
                    type: "Primary",
                    width: 275,
                    tag: item.PrimaryImageTag
                }) : null
            },
            systemUpdateTaskKey: "SystemUpdateTask",
            renderUrls: function(page, systemInfo) {
                var helpButton = '<a is="emby-linkbutton" class="raised raised-mini button-submit" href="https://web.archive.org/web/20181216120305/https://github.com/MediaBrowser/Wiki/wiki/Connectivity" target="_blank" style="margin-left:.7em;font-size:84%;padding:.2em .8em;">' + globalize.translate("ButtonHelp") + "</a>",
                    localUrlElem = page.querySelector(".localUrl"),
                    externalUrlElem = page.querySelector(".externalUrl");
                if (systemInfo.LocalAddress) {
                    var localAccessHtml = globalize.translate("LabelLocalAccessUrl", '<a is="emby-linkbutton" class="button-link" href="' + systemInfo.LocalAddress + '" target="_blank">' + systemInfo.LocalAddress + "</a>");
                    localUrlElem.innerHTML = localAccessHtml + helpButton, localUrlElem.classList.remove("hide")
                } else localUrlElem.classList.add("hide");
                if (systemInfo.WanAddress) {
                    var externalUrl = systemInfo.WanAddress,
                        remoteAccessHtml = globalize.translate("LabelRemoteAccessUrl", '<a is="emby-linkbutton" class="button-link" href="' + externalUrl + '" target="_blank">' + externalUrl + "</a>");
                    externalUrlElem.innerHTML = remoteAccessHtml + helpButton, externalUrlElem.classList.remove("hide")
                } else externalUrlElem.classList.add("hide")
            },
            renderSupporterIcon: function(page, pluginSecurityInfo) {
                var imgUrl, text, supporterIconContainer = page.querySelector(".supporterIconContainer");
                pluginSecurityInfo.IsMBSupporter ? (supporterIconContainer.classList.remove("hide"), imgUrl = "css/images/supporter/supporterbadge.png", text = globalize.translate("MessageThankYouForSupporting"), supporterIconContainer.innerHTML = '<a is="emby-linkbutton" class="button-link imageLink supporterIcon" href="https://github.com/jellyfin/jellyfin/premiere" target="_blank" title="' + text + '"><img src="' + imgUrl + '" style="height:2em;" /></a>') : supporterIconContainer.classList.add("hide")
            },
            renderPendingInstallations: function(page, systemInfo) {
                if (!systemInfo.CompletedInstallations.length) return void page.querySelector("#collapsiblePendingInstallations").classList.add("hide");
                page.querySelector("#collapsiblePendingInstallations").classList.remove("hide");
                for (var html = "", i = 0, length = systemInfo.CompletedInstallations.length; i < length; i++) {
                    var update = systemInfo.CompletedInstallations[i];
                    html += "<div><strong>" + update.Name + "</strong> (" + update.Version + ")</div>"
                }
                page.querySelector("#pendingInstallations").innerHTML = html
            },
            renderPluginUpdateInfo: function(page, forceUpdate) {
                !forceUpdate && DashboardPage.lastPluginUpdateCheck && (new Date).getTime() - DashboardPage.lastPluginUpdateCheck < 18e5 || (DashboardPage.lastPluginUpdateCheck = (new Date).getTime(), ApiClient.getAvailablePluginUpdates().then(function(updates) {
                    var elem = page.querySelector("#pPluginUpdates");
                    if (!updates.length) return void elem.classList.add("hide");
                    elem.classList.remove("hide");
                    for (var html = "", i = 0, length = updates.length; i < length; i++) {
                        var update = updates[i];
                        html += "<p><strong>" + globalize.translate("NewVersionOfSomethingAvailable").replace("{0}", update.name) + "</strong></p>", html += '<button type="button" is="emby-button" class="raised block" onclick="DashboardPage.installPluginUpdate(this);" data-name="' + update.name + '" data-guid="' + update.guid + '" data-version="' + update.versionStr + '" data-classification="' + update.classification + '">' + globalize.translate("ButtonUpdateNow") + "</button>"
                    }
                    elem.innerHTML = html
                }))
            },
            installPluginUpdate: function(button) {
                buttonEnabled(button, !1);
                var name = button.getAttribute("data-name"),
                    guid = button.getAttribute("data-guid"),
                    version = button.getAttribute("data-version"),
                    classification = button.getAttribute("data-classification");
                loading.show(), ApiClient.installPlugin(name, guid, classification, version).then(function() {
                    loading.hide()
                })
            },
            updateApplication: function(btn) {
                var page = dom.parentWithClass(btn, "page");
                buttonEnabled(page.querySelector("#btnUpdateApplication"), !1), loading.show(), ApiClient.getScheduledTasks().then(function(tasks) {
                    var task = tasks.filter(function(t) {
                        return t.Key == DashboardPage.systemUpdateTaskKey
                    })[0];
                    ApiClient.startScheduledTask(task.Id).then(function() {
                        pollForInfo(page, ApiClient), loading.hide()
                    })
                })
            },
            stopTask: function(btn, id) {
                var page = dom.parentWithClass(btn, "page");
                ApiClient.stopScheduledTask(id).then(function() {
                    pollForInfo(page, ApiClient)
                })
            },
            restart: function(btn) {
                require(["confirm"], function(confirm) {
                    confirm({
                        title: globalize.translate("HeaderRestart"),
                        text: globalize.translate("MessageConfirmRestart"),
                        confirmText: globalize.translate("ButtonRestart"),
                        primary: "cancel"
                    }).then(function() {
                        var page = dom.parentWithClass(btn, "page");
                        buttonEnabled(page.querySelector("#btnRestartServer"), !1), buttonEnabled(page.querySelector("#btnShutdown"), !1), Dashboard.restartServer()
                    })
                })
            },
            shutdown: function(btn) {
                require(["confirm"], function(confirm) {
                    confirm({
                        title: globalize.translate("HeaderShutdown"),
                        text: globalize.translate("MessageConfirmShutdown"),
                        confirmText: globalize.translate("ButtonShutdown"),
                        primary: "cancel"
                    }).then(function() {
                        var page = dom.parentWithClass(btn, "page");
                        buttonEnabled(page.querySelector("#btnRestartServer"), !1), buttonEnabled(page.querySelector("#btnShutdown"), !1), ApiClient.shutdownServer()
                    })
                })
            }
        }, pageClassOn("pageshow", "type-interior", function() {
            var page = this;
            page.querySelector(".customSupporterPromotion") || getPluginSecurityInfo().then(function(pluginSecurityInfo) {
                var supporterPromotionElem = page.querySelector(".supporterPromotion");
                if (supporterPromotionElem && supporterPromotionElem.parentNode.removeChild(supporterPromotionElem), !pluginSecurityInfo.IsMBSupporter) {
                    var html = '<div class="supporterPromotionContainer"><div class="supporterPromotion">';
                    html += '<a is="emby-linkbutton" href="https://github.com/jellyfin/jellyfin" target="_blank" class="raised block" style="background-color:#00a4dc;color:#fff;"><div>' + globalize.translate("HeaderSupportTheTeam") + '</div><div style="font-weight:normal;margin-top:5px;">' + globalize.translate("TextEnjoyBonusFeatures") + "</div></a></div></div>", page.querySelector(".content-primary").insertAdjacentHTML("afterbegin", html)
                }
            })
        }),
        function(view, params) {
            function onRestartRequired(e, apiClient) {
                apiClient.serverId() === serverId && renderHasPendingRestart(view, apiClient, !0)
            }

            function onServerShuttingDown(e, apiClient) {
                apiClient.serverId() === serverId && renderHasPendingRestart(view, apiClient, !0)
            }

            function onServerRestarting(e, apiClient) {
                apiClient.serverId() === serverId && renderHasPendingRestart(view, apiClient, !0)
            }

            function onPackageInstalling(e, apiClient) {
                apiClient.serverId() === serverId && (pollForInfo(view, apiClient, !0), reloadSystemInfo(view, apiClient))
            }

            function onPackageInstallationCompleted(e, apiClient) {
                apiClient.serverId() === serverId && (pollForInfo(view, apiClient, !0), reloadSystemInfo(view, apiClient))
            }

            function onSessionsUpdate(e, apiClient, info) {
                apiClient.serverId() === serverId && renderInfo(view, info)
            }

            function onScheduledTasksUpdate(e, apiClient, info) {
                apiClient.serverId() === serverId && renderRunningTasks(view, info)
            }
            var serverId = ApiClient.serverId();
            view.querySelector(".btnConnectionHelp").addEventListener("click", onConnectionHelpClick), view.querySelector(".btnEditServerName").addEventListener("click", onEditServerNameClick), view.querySelector(".activeDevices").addEventListener("click", onActiveDevicesClick), view.addEventListener("viewshow", function() {
                var page = this,
                    apiClient = ApiClient;
                if (apiClient) {
                    loading.show(), pollForInfo(page, apiClient), DashboardPage.startInterval(apiClient), events.on(serverNotifications, "RestartRequired", onRestartRequired), events.on(serverNotifications, "ServerShuttingDown", onServerShuttingDown), events.on(serverNotifications, "ServerRestarting", onServerRestarting), events.on(serverNotifications, "PackageInstalling", onPackageInstalling), events.on(serverNotifications, "PackageInstallationCompleted", onPackageInstallationCompleted), events.on(serverNotifications, "Sessions", onSessionsUpdate),
                        events.on(serverNotifications, "ScheduledTasksInfo", onScheduledTasksUpdate), DashboardPage.lastAppUpdateCheck = null, DashboardPage.lastPluginUpdateCheck = null, getPluginSecurityInfo().then(function(pluginSecurityInfo) {
                            DashboardPage.renderSupporterIcon(page, pluginSecurityInfo)
                        }), reloadSystemInfo(page, ApiClient), page.userActivityLog || (page.userActivityLog = new ActivityLog({
                            serverId: ApiClient.serverId(),
                            element: page.querySelector(".userActivityItems")
                        })), ApiClient.isMinServerVersion("3.4.1.25") && (page.serverActivityLog || (page.serverActivityLog = new ActivityLog({
                            serverId: ApiClient.serverId(),
                            element: page.querySelector(".serverActivityItems")
                        })));
                    refreshActiveRecordings(view, apiClient), loading.hide()
                }
            }), view.addEventListener("viewbeforehide", function() {
                var apiClient = ApiClient;
                events.off(serverNotifications, "RestartRequired", onRestartRequired), events.off(serverNotifications, "ServerShuttingDown", onServerShuttingDown), events.off(serverNotifications, "ServerRestarting", onServerRestarting), events.off(serverNotifications, "PackageInstalling", onPackageInstalling), events.off(serverNotifications, "PackageInstallationCompleted", onPackageInstallationCompleted), events.off(serverNotifications, "Sessions", onSessionsUpdate), events.off(serverNotifications, "ScheduledTasksInfo", onScheduledTasksUpdate), apiClient && DashboardPage.stopInterval(apiClient)
            }), view.addEventListener("viewdestroy", function() {
                var page = this,
                    userActivityLog = page.userActivityLog;
                userActivityLog && userActivityLog.destroy();
                var serverActivityLog = page.serverActivityLog;
                serverActivityLog && serverActivityLog.destroy()
            })
        }
});
