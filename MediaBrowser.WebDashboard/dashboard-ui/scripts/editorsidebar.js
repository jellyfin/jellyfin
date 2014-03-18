(function ($, document, window) {

    function getNode(item, folderState) {

        var state = item.IsFolder ? folderState : '';

        var htmlName = getNodeInnerHtml(item);

        var rel = item.IsFolder ? 'folder' : 'default';

        return { attr: { id: item.Id, rel: rel, itemtype: item.Type }, data: htmlName, state: state };
    }
    
    function getNodeInnerHtml(item) {
        
        var name = item.Name;

        // Channel number
        if (item.Number) {
            name = item.Number + " - " + name;
        }
        if (item.IndexNumber != null && item.Type != "Season") {
            name = item.IndexNumber + " - " + name;
        }

        var cssClass = "editorNode";

        if (item.LocationType == "Offline") {
            cssClass += " offlineEditorNode";
        }

        var htmlName = "<div class='" + cssClass + "'>";

        if (item.LockData) {
            htmlName += '<img src="css/images/editor/lock.png" />';
        }

        htmlName += name;

        if (!item.LocalTrailerCount && item.Type == "Movie") {
            htmlName += '<img src="css/images/editor/missingtrailer.png" title="Missing local trailer." />';
        }

        if (!item.ImageTags || !item.ImageTags.Primary) {
            htmlName += '<img src="css/images/editor/missingprimaryimage.png" title="Missing primary image." />';
        }

        if (!item.BackdropImageTags || !item.BackdropImageTags.length) {
            if (item.Type !== "Episode" && item.Type !== "Season" && item.MediaType !== "Audio" && item.Type !== "TvChannel" && item.Type !== "MusicAlbum") {
                htmlName += '<img src="css/images/editor/missingbackdrop.png" title="Missing backdrop image." />';
            }
        }

        if (!item.ImageTags || !item.ImageTags.Logo) {
            if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "Series" || item.Type == "MusicArtist" || item.Type == "BoxSet") {
                htmlName += '<img src="css/images/editor/missinglogo.png" title="Missing logo image." />';
            }
        }

        if (item.Type == "Episode" && item.LocationType == "Virtual") {

            try {
                if (item.PremiereDate && (new Date().getTime() >= parseISO8601Date(item.PremiereDate, { toLocal: true }).getTime())) {
                    htmlName += '<img src="css/images/editor/missing.png" title="Missing episode." />';
                }
            } catch (err) {

            }

        }

        htmlName += "</div>";

        return htmlName;
    }

    function loadChildrenOfRootNode(page, callback, openItems, selectedId) {

        var promise1 = $.getJSON(ApiClient.getUrl("Library/MediaFolders"));

        var promise2 = ApiClient.getLiveTvInfo();

        $.when(promise1, promise2).done(function (response1, response2) {

            var mediaFolders = response1[0].Items;
            var liveTvInfo = response2[0];

            var nodes = [];

            var i, length;

            for (i = 0, length = mediaFolders.length; i < length; i++) {

                var state = openItems.indexOf(mediaFolders[i].Id) == -1 ? 'closed' : 'open';

                nodes.push(getNode(mediaFolders[i], state));
            }

            for (i = 0, length = liveTvInfo.Services.length; i < length; i++) {

                var service = liveTvInfo.Services[i];

                var name = service.Name;

                var htmlName = "<div class='editorNode'>";

                htmlName += name;

                htmlName += "</div>";

                nodes.push({ attr: { id: name, rel: 'folder', itemtype: 'livetvservice' }, data: htmlName, state: 'closed' });
            }

            nodes.push({ attr: { id: 'libraryreport', rel: 'default', itemtype: 'libraryreport' }, data: 'Reports' });

            callback(nodes);
            
            if (!selectedId) {
                
                if (window.location.toString().toLowerCase().indexOf('report.html') != -1) {
                    selectedId = 'libraryreport';
                }
            }

            if (selectedId && nodes.filter(function (f) {

                return f.attr.id == selectedId;

            }).length) {

                selectNode(page, selectedId);
            }
        });
    }

    function loadLiveTvChannels(service, openItems, callback) {

        ApiClient.getLiveTvChannels({ ServiceName: service }).done(function (result) {

            var nodes = result.Items.map(function (i) {

                var state = openItems.indexOf(i.Id) == -1 ? 'closed' : 'open';

                return getNode(i, state);

            });

            callback(nodes);

        });

    }

    function loadNode(page, node, openItems, selectedId, currentUser, callback) {

        if (node == '-1') {

            loadChildrenOfRootNode(page, callback, openItems, selectedId);
            return;
        }

        var id = node.attr("id");

        var itemtype = node.attr("itemtype");

        if (itemtype == 'libraryreport') {

            return;
        }

        if (itemtype == 'livetvservice') {

            loadLiveTvChannels(id, openItems, callback);
            return;
        }

        var query = {
            ParentId: id,
            Fields: 'Settings'
        };

        if (itemtype != "Season" && itemtype != "Series") {
            query.SortBy = "SortName";
        }

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            var nodes = result.Items.map(function (i) {

                var state = openItems.indexOf(i.Id) == -1 ? 'closed' : 'open';

                return getNode(i, state);

            });

            callback(nodes);

            if (selectedId && result.Items.filter(function (f) {

                return f.Id == selectedId;

            }).length) {

                selectNode(page, selectedId);
            }

        });

    }

    function selectNode(page, id) {

        var elem = $('#' + id, page)[0];

        $.jstree._reference(".libraryTree", page).select_node(elem);

        if (elem) {
            elem.scrollIntoView();
        }

        $(document).scrollTop(0);
    }

    function initializeTree(page, currentUser, openItems, selectedId) {

        $('.libraryTree', page).jstree({

            "plugins": ["themes", "ui", "json_data"],

            data: function (node, callback) {
                loadNode(page, node, openItems, selectedId, currentUser, callback);
            },

            json_data: {

                data: function (node, callback) {
                    loadNode(page, node, openItems, selectedId, currentUser, callback);
                }

            },

            core: { initially_open: [], load_open: true, html_titles: true },
            ui: { initially_select: [] },

            themes: {
                theme: 'mb3',
                url: 'thirdparty/jstree1.0/themes/mb3/style.css?v=' + Dashboard.initialServerVersion
            }

        }).off('select_node.jstree').on('select_node.jstree', function (event, data) {

            var eventData = {
                id: data.rslt.obj.attr("id"),
                itemType: data.rslt.obj.attr("itemtype")
            };

            $(this).trigger('itemclicked', [eventData]);

        });
    }
    
    function updateEditorNode(page, item) {

        var elem = $('#' + item.Id + '>a', page)[0];

        if (elem == null) {
            return;
        }

        $('.editorNode', elem).remove();

        $(elem).append(getNodeInnerHtml(item));
        
        if (item.IsFolder) {

            var tree = jQuery.jstree._reference(".libraryTree");
            var currentNode = tree._get_node(null, false);
            tree.refresh(currentNode);
        }
    }

    $(document).on('itemsaved', ".metadataEditorPage", function (e, item) {

        updateEditorNode(this, item);

    }).on('pagebeforeshow', ".metadataEditorPage", function () {

        window.MetadataEditor = new metadataEditor();

        var page = this;

        Dashboard.getCurrentUser().done(function (user) {

            var id = MetadataEditor.currentItemId;

            if (id) {

                ApiClient.getAncestorItems(id, user.Id).done(function (ancestors) {

                    var ids = ancestors.map(function (i) {
                        return i.Id;
                    });

                    initializeTree(page, user, ids, id);
                });

            } else {
                initializeTree(page, user, []);
            }

        });

    }).on('pagebeforehide', ".metadataEditorPage", function () {

        var page = this;

        $('.libraryTree', page).off('select_node.jstree');

    });

    function metadataEditor() {

        var self = this;

        function ensureInitialValues() {

            if (self.currentItemType || self.currentItemName || self.currentItemId) {
                return;
            }

            var url = window.location.hash || window.location.toString();

            var name = getParameterByName('person', url);

            if (name) {
                self.currentItemType = "Person";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('studio', url);

            if (name) {
                self.currentItemType = "Studio";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('genre', url);

            if (name) {
                self.currentItemType = "Genre";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('musicgenre', url);

            if (name) {
                self.currentItemType = "MusicGenre";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('gamegenre', url);

            if (name) {
                self.currentItemType = "GameGenre";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('musicartist', url);

            if (name) {
                self.currentItemType = "MusicArtist";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('channelid', url);

            if (name) {
                self.currentItemType = "TvChannel";
                self.currentItemId = name;
                return;
            }

            var id = getParameterByName('id', url);

            if (id) {
                self.currentItemId = id;
                self.currentItemType = null;
            }
        };

        self.getItemPromise = function () {

            var currentItemType = self.currentItemType;
            var currentItemName = self.currentItemName;
            var currentItemId = self.currentItemId;

            if (currentItemType == "TvChannel") {
                return ApiClient.getLiveTvChannel(currentItemId);
            }

            if (currentItemType == "Person") {
                return ApiClient.getPerson(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "Studio") {
                return ApiClient.getStudio(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "Genre") {
                return ApiClient.getGenre(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "MusicGenre") {
                return ApiClient.getMusicGenre(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "GameGenre") {
                return ApiClient.getGameGenre(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "MusicArtist" && currentItemName) {
                return ApiClient.getArtist(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemId) {
                return ApiClient.getItem(Dashboard.getCurrentUserId(), currentItemId);
            }

            return ApiClient.getRootFolder(Dashboard.getCurrentUserId());
        };

        self.getEditQueryString = function (item) {

            var query;

            if (item.Type == "Person" ||
                item.Type == "Studio" ||
                item.Type == "Genre" ||
                item.Type == "MusicGenre" ||
                item.Type == "GameGenre" ||
                item.Type == "MusicArtist") {
                query = item.Type + "=" + ApiClient.encodeName(item.Name);

            } else {
                query = "id=" + item.Id;
            }

            var context = getParameterByName('context');

            if (context) {
                query += "&context=" + context;
            }

            return query;
        };

        ensureInitialValues();
    }


})(jQuery, document, window);