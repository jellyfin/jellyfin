if (!window.MediaBrowser) {
    window.MediaBrowser = {};
}

MediaBrowser.ApiClient = function ($, navigator) {

    /**
     * Creates a new api client instance
     * @param {String} serverProtocol
     * @param {String} serverHostName
     * @param {String} serverPortNumber
     * @param {String} clientName 
     */
    return function (serverProtocol, serverHostName, serverPortNumber, clientName) {

        var self = this;
        var deviceName = "Web Browser";
        var deviceId = SHA1(navigator.userAgent + (navigator.cpuClass || ""));
        var currentUserId;

        /**
         * Gets the server host name.
         */
        self.serverHostName = function () {

            return serverHostName;
        };

        /**
         * Gets the server port number.
         */
        self.serverPortNumber = function () {

            return serverPortNumber;
        };

        /**
         * Gets or sets the current user id.
         */
        self.currentUserId = function (val) {

            if (val !== undefined) {
                currentUserId = val;
            } else {
                return currentUserId;
            }
        };

        /**
         * Wraps around jQuery ajax methods to add additional info to the request.
         */
        self.ajax = function (request) {

            if (!request) {
                throw new Error("Request cannot be null");
            }

            var auth = 'MediaBrowser Client="' + clientName + '", Device="' + deviceName + '", DeviceId="' + deviceId + '"';

            if (currentUserId) {
                auth += ', UserId="' + currentUserId + '"';
            }

            request.headers = {
                Authorization: auth
            };

            return $.ajax(request);
        };

        /**
         * Creates an api url based on a handler name and query string parameters
         * @param {String} name
         * @param {Object} params
         */
        self.getUrl = function (name, params) {

            if (!name) {
                throw new Error("Url name cannot be empty");
            }

            var url = serverProtocol + "//" + serverHostName + ":" + serverPortNumber + "/mediabrowser/" + name;

            if (params) {
                url += "?" + $.param(params);
            }

            return url;
        };

        /**
         * Gets an item from the server
         * Omit itemId to get the root folder.
         */
        self.getItem = function (userId, itemId) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/" + itemId);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the root folder from the server
         */
        self.getRootFolder = function (userId) {

            return self.getItem(userId);
        };

        /**
         * Gets the current server status
         */
        self.getSystemInfo = function () {

            var url = self.getUrl("System/Info");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all cultures known to the server
         */
        self.getCultures = function () {

            var url = self.getUrl("Localization/cultures");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all countries known to the server
         */
        self.getCountries = function () {

            var url = self.getUrl("Localization/countries");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets plugin security info
         */
        self.getPluginSecurityInfo = function () {

            var url = self.getUrl("Plugins/SecurityInfo");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the directory contents of a path on the server
         */
        self.getDirectoryContents = function (path, options) {

            if (!path) {
                throw new Error("null path");
            }

            options = options || {};

            options.path = path;

            var url = self.getUrl("Environment/DirectoryContents", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a list of physical drives from the server
         */
        self.getDrives = function () {

            var url = self.getUrl("Environment/Drives");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a list of network devices from the server
         */
        self.getNetworkDevices = function () {

            var url = self.getUrl("Environment/NetworkDevices");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Cancels a package installation
         */
        self.cancelPackageInstallation = function (installationId) {

            if (!installationId) {
                throw new Error("null installationId");
            }

            var url = self.getUrl("Packages/Installing/" + id);

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Installs or updates a new plugin
         */
        self.installPlugin = function (name, updateClass, version) {

            if (!name) {
                throw new Error("null name");
            }

            if (!updateClass) {
                throw new Error("null updateClass");
            }

            var options = {
                updateClass: updateClass
            };

            if (version) {
                options.version = version;
            }

            var url = self.getUrl("Packages/Installed/" + name, options);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
         * Instructs the server to perform a pending kernel reload or app restart.
         * If a restart is not currently required, nothing will happen.
         */
        self.performPendingRestart = function () {

            var url = self.getUrl("System/Restart");

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
         * Gets information about an installable package
         */
        self.getPackageInfo = function (name) {

            if (!name) {
                throw new Error("null name");
            }

            var url = self.getUrl("Packages/" + name);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the latest available application update (if any)
         */
        self.getAvailableApplicationUpdate = function () {

            var url = self.getUrl("Packages/Updates", { PackageType: "System" });

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the latest available plugin updates (if any)
         */
        self.getAvailablePluginUpdates = function () {

            var url = self.getUrl("Packages/Updates", { PackageType: "UserInstalled" });

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the virtual folder for a view. Specify a userId to get a user view, or omit for the default view.
         */
        self.getVirtualFolders = function (userId) {

            var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

            url = self.getUrl(url);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all the paths of the locations in the physical root.
         */
        self.getPhysicalPaths = function () {

            var url = self.getUrl("Library/PhysicalPaths");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the current server configuration
         */
        self.getServerConfiguration = function () {

            var url = self.getUrl("System/Configuration");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the server's scheduled tasks
         */
        self.getScheduledTasks = function () {

            var url = self.getUrl("ScheduledTasks");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
        * Starts a scheduled task
        */
        self.startScheduledTask = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("ScheduledTasks/Running/" + id);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
        * Gets a scheduled task
        */
        self.getScheduledTask = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("ScheduledTasks/" + id);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
        * Stops a scheduled task
        */
        self.stopScheduledTask = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("ScheduledTasks/Running/" + id);

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the configuration of a plugin
         * @param {String} Id
         */
        self.getPluginConfiguration = function (id) {

            if (!id) {
                throw new Error("null Id");
            }

            var url = self.getUrl("Plugins/" + id + "/Configuration");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a list of plugins that are available to be installed
         */
        self.getAvailablePlugins = function () {

            var url = self.getUrl("Packages", { PackageType: "UserInstalled" });

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Uninstalls a plugin
         * @param {String} Id
         */
        self.uninstallPlugin = function (id) {

            if (!id) {
                throw new Error("null Id");
            }

            var url = self.getUrl("Plugins/" + id);

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };

        /**
        * Removes a virtual folder from either the default view or a user view
        * @param {String} name
        */
        self.removeVirtualFolder = function (name, userId) {

            if (!name) {
                throw new Error("null name");
            }

            var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

            url += "/" + name;
            url = self.getUrl(url);

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };

        /**
       * Adds a virtual folder to either the default view or a user view
       * @param {String} name
       */
        self.addVirtualFolder = function (name, userId) {

            if (!name) {
                throw new Error("null name");
            }

            var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

            url += "/" + name;
            url = self.getUrl(url);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
       * Renames a virtual folder, within either the default view or a user view
       * @param {String} name
       */
        self.renameVirtualFolder = function (name, newName, userId) {

            if (!name) {
                throw new Error("null name");
            }

            var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

            url += "/" + name + "/Name";

            url = self.getUrl(url, { newName: newName });

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
        * Adds an additional mediaPath to an existing virtual folder, within either the default view or a user view
        * @param {String} name
        */
        self.addMediaPath = function (virtualFolderName, mediaPath, userId) {

            if (!virtualFolderName) {
                throw new Error("null virtualFolderName");
            }

            if (!mediaPath) {
                throw new Error("null mediaPath");
            }

            var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

            url += "/" + virtualFolderName + "/Paths";

            url = self.getUrl(url, { path: mediaPath });

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
        * Removes a media path from a virtual folder, within either the default view or a user view
        * @param {String} name
        */
        self.removeMediaPath = function (virtualFolderName, mediaPath, userId) {

            if (!virtualFolderName) {
                throw new Error("null virtualFolderName");
            }

            if (!mediaPath) {
                throw new Error("null mediaPath");
            }

            var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

            url += "/" + virtualFolderName + "/Paths";

            url = self.getUrl(url, { path: mediaPath });

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Deletes a user
         * @param {String} id
         */
        self.deleteUser = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("Users/" + id);

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Deletes a user image
         * @param {String} userId
         * @param {String} imageType The type of image to delete, based on the server-side ImageType enum.
         */
        self.deleteUserImage = function (userId, imageType) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!imageType) {
                throw new Error("null imageType");
            }

            var url = self.getUrl("Users/" + userId + "/Images/" + imageType);

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Uploads a user image
         * @param {String} userId
         * @param {String} imageType The type of image to delete, based on the server-side ImageType enum.
         * @param {Object} file The file from the input element
         */
        self.uploadUserImage = function (userId, imageType, file) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!imageType) {
                throw new Error("null imageType");
            }

            if (!file || !file.type.match('image.*')) {
                throw new Error("File must be an image.");
            }

            var deferred = $.Deferred();

            var reader = new FileReader();

            reader.onerror = function () {
                deferred.reject();
            };

            reader.onabort = function () {
                deferred.reject();
            };

            // Closure to capture the file information.
            reader.onload = function (e) {

                var data = window.btoa(e.target.result);

                var url = self.getUrl("Users/" + userId + "/Images/" + imageType);

                self.ajax({
                    type: "POST",
                    url: url,
                    data: data,
                    contentType: "image/" + file.name.substring(file.name.lastIndexOf('.') + 1)
                }).done(function (result) {

                    deferred.resolveWith(null, [result]);

                }).fail(function () {
                    deferred.reject();
                });
            };

            // Read in the image file as a data URL.
            reader.readAsBinaryString(file);

            return deferred.promise();
        };

        /**
         * Gets the list of installed plugins on the server
         */
        self.getInstalledPlugins = function () {

            var url = self.getUrl("Plugins");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a user by id
         * @param {String} id
         */
        self.getUser = function (id) {

            if (!id) {
                throw new Error("Must supply a userId");
            }

            var url = self.getUrl("Users/" + id);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a studio
         */
        self.getStudio = function (name) {

            if (!name) {
                throw new Error("null name");
            }

            var url = self.getUrl("Studios/" + name);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a genre
         */
        self.getGenre = function (name) {

            if (!name) {
                throw new Error("null name");
            }

            var url = self.getUrl("Genres/" + name);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a year
         */
        self.getYear = function (year) {

            if (!year) {
                throw new Error("null year");
            }

            var url = self.getUrl("Years/" + year);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a Person
         */
        self.getPerson = function (name) {

            if (!name) {
                throw new Error("null name");
            }

            var url = self.getUrl("Persons/" + name);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets weather info
         * @param {String} location - us zip code / city, state, country / city, country
         * Omit location to get weather info using stored server configuration value
         */
        self.getWeatherInfo = function (location) {

            var url = self.getUrl("weather", {
                location: location
            });

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all users from the server
         */
        self.getUsers = function () {

            var url = self.getUrl("users");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all available parental ratings from the server
         */
        self.getParentalRatings = function () {

            var url = self.getUrl("Localization/ParentalRatings");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a list of all available conrete BaseItem types from the server
         */
        self.getItemTypes = function (options) {

            var url = self.getUrl("Library/ItemTypes", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Constructs a url for a user image
         * @param {String} userId
         * @param {Object} options
         * Options supports the following properties:
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getUserImageUrl = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {

            };

            var url = "Users/" + userId + "/Images/" + options.type;

            if (options.index != null) {
                url += "/" + options.index;
            }

            // Don't put these on the query string
            delete options.type;
            delete options.index;

            return self.getUrl(url, options);
        };

        /**
         * Constructs a url for a person image
         * @param {String} name
         * @param {Object} options
         * Options supports the following properties:
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getPersonImageUrl = function (name, options) {

            if (!name) {
                throw new Error("null name");
            }

            options = options || {

            };

            var url = "Persons/" + name + "/Images/" + options.type;

            if (options.index != null) {
                url += "/" + options.index;
            }

            // Don't put these on the query string
            delete options.type;
            delete options.index;

            return self.getUrl(url, options);
        };

        /**
         * Constructs a url for a year image
         * @param {String} year
         * @param {Object} options
         * Options supports the following properties:
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getYearImageUrl = function (year, options) {

            if (!year) {
                throw new Error("null year");
            }

            options = options || {

            };

            var url = "Years/" + year + "/Images/" + options.type;

            if (options.index != null) {
                url += "/" + options.index;
            }

            // Don't put these on the query string
            delete options.type;
            delete options.index;

            return self.getUrl(url, options);
        };

        /**
         * Constructs a url for a genre image
         * @param {String} name
         * @param {Object} options
         * Options supports the following properties:
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getGenreImageUrl = function (name, options) {

            if (!name) {
                throw new Error("null name");
            }

            options = options || {

            };

            var url = "Genres/" + name + "/Images/" + options.type;

            if (options.index != null) {
                url += "/" + options.index;
            }

            // Don't put these on the query string
            delete options.type;
            delete options.index;

            return self.getUrl(url, options);
        };

        /**
         * Constructs a url for a genre image
         * @param {String} name
         * @param {Object} options
         * Options supports the following properties:
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getStudioImageUrl = function (name, options) {

            if (!name) {
                throw new Error("null name");
            }

            options = options || {

            };

            var url = "Studios/" + name + "/Images/" + options.type;

            if (options.index != null) {
                url += "/" + options.index;
            }

            // Don't put these on the query string
            delete options.type;
            delete options.index;

            return self.getUrl(url, options);
        };

        /**
         * Constructs a url for an item image
         * @param {String} itemId
         * @param {Object} options
         * Options supports the following properties:
         * type - Primary, logo, backdrop, etc. See the server-side enum ImageType
         * index - When downloading a backdrop, use this to specify which one (omitting is equivalent to zero)
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getImageUrl = function (itemId, options) {

            if (!itemId) {
                throw new Error("itemId cannot be empty");
            }

            options = options || {

            };

            var url = "Items/" + itemId + "/Images/" + options.type;

            if (options.index != null) {
                url += "/" + options.index;
            }

            // Don't put these on the query string
            delete options.type;
            delete options.index;

            return self.getUrl(url, options);
        };

        /**
         * Constructs a url for an item logo image
         * If the item doesn't have a logo, it will inherit a logo from a parent
         * @param {Object} item A BaseItem
         * @param {Object} options
         * Options supports the following properties:
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getLogoImageUrl = function (item, options) {

            if (!item) {
                throw new Error("null item");
            }

            options = options || {

            };

            options.imageType = "logo";

            var logoItemId = item.HasLogo ? item.Id : item.ParentLogoItemId;

            return logoItemId ? self.getImageUrl(logoItemId, options) : null;
        };

        /**
         * Constructs an array of backdrop image url's for an item
         * If the item doesn't have any backdrops, it will inherit them from a parent
         * @param {Object} item A BaseItem
         * @param {Object} options
         * Options supports the following properties:
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getBackdropImageUrl = function (item, options) {

            if (!item) {
                throw new Error("null item");
            }

            options = options || {

            };

            options.imageType = "backdrop";

            var backdropItemId;
            var backdropCount;

            if (!item.BackdropCount) {
                backdropItemId = item.ParentBackdropItemId;
                backdropCount = item.ParentBackdropCount || 0;
            } else {
                backdropItemId = item.Id;
                backdropCount = item.BackdropCount;
            }

            if (!backdropItemId) {
                return [];
            }

            var files = [];

            for (var i = 0; i < backdropCount; i++) {

                options.imageIndex = i;

                files[i] = self.getImageUrl(backdropItemId, options);
            }

            return files;
        };

        /**
         * Authenticates a user
         * @param {String} userId
         * @param {String} password
         */
        self.authenticateUser = function (userId, password) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/authenticate");

            var postData = {
                password: SHA1(password || "")
            };

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(postData),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Updates a user's password
         * @param {String} userId
         * @param {String} currentPassword
         * @param {String} newPassword
         */
        self.updateUserPassword = function (userId, currentPassword, newPassword) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/Password");

            var postData = {

            };

            postData.currentPassword = SHA1(currentPassword);

            if (newPassword) {
                postData.newPassword = newPassword;
            }

            return self.ajax({
                type: "POST",
                url: url,
                data: postData
            });
        };

        /**
        * Resets a user's password
        * @param {String} userId
        */
        self.resetUserPassword = function (userId) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/Password");

            var postData = {

            };

            postData.resetPassword = true;

            return self.ajax({
                type: "POST",
                url: url,
                data: postData
            });
        };

        /**
         * Updates the server's configuration
         * @param {Object} configuration
         */
        self.updateServerConfiguration = function (configuration) {

            if (!configuration) {
                throw new Error("null configuration");
            }

            var url = self.getUrl("System/Configuration");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(configuration),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Updates plugin security info
         */
        self.updatePluginSecurityInfo = function (info) {

            var url = self.getUrl("Plugins/SecurityInfo");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(info),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Creates a user
         * @param {Object} user
         */
        self.createUser = function (user) {

            if (!user) {
                throw new Error("null user");
            }

            var url = self.getUrl("Users");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(user),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Updates a user
         * @param {Object} user
         */
        self.updateUser = function (user) {

            if (!user) {
                throw new Error("null user");
            }

            var url = self.getUrl("Users/" + user.Id);

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(user),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Updates the Triggers for a ScheduledTask
         * @param {String} id
         * @param {Object} triggers
         */
        self.updateScheduledTaskTriggers = function (id, triggers) {

            if (!id) {
                throw new Error("null id");
            }

            if (!triggers) {
                throw new Error("null triggers");
            }

            var url = self.getUrl("ScheduledTasks/" + id + "/Triggers");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(triggers),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Updates a plugin's configuration
         * @param {String} Id
         * @param {Object} configuration
         */
        self.updatePluginConfiguration = function (id, configuration) {

            if (!id) {
                throw new Error("null Id");
            }

            if (!configuration) {
                throw new Error("null configuration");
            }

            var url = self.getUrl("Plugins/" + id + "/Configuration");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(configuration),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Gets items based on a query, typicall for children of a folder
         * @param {String} userId
         * @param {Object} options
         * Options accepts the following properties:
         * itemId - Localize the search to a specific folder (root if omitted)
         * startIndex - Use for paging
         * limit - Use to limit results to a certain number of items
         * filter - Specify one or more ItemFilters, comma delimeted (see server-side enum)
         * sortBy - Specify an ItemSortBy (comma-delimeted list see server-side enum)
         * sortOrder - ascending/descending
         * fields - additional fields to include aside from basic info. This is a comma delimited list. See server-side enum ItemFields.
         * index - the name of the dynamic, localized index function
         * dynamicSortBy - the name of the dynamic localized sort function
         * recursive - Whether or not the query should be recursive
         * searchTerm - search term to use as a filter
         */
        self.getItems = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/Items", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Marks an item as played or unplayed
         * This should not be used to update playstate following playback.
         * There are separate playstate check-in methods for that. This should be used for a
         * separate option to reset playstate.
         * @param {String} userId
         * @param {String} itemId
         * @param {Boolean} wasPlayed
         */
        self.updatePlayedStatus = function (userId, itemId, wasPlayed) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = "Users/" + userId + "/PlayedItems/" + itemId;

            var method = wasPlayed ? "POST" : "DELETE";

            return self.ajax({
                type: method,
                url: url,
                dataType: "json"
            });
        };

        /**
         * Updates a user's favorite status for an item and returns the updated UserItemData object.
         * @param {String} userId
         * @param {String} itemId
         * @param {Boolean} isFavorite
         */
        self.updateFavoriteStatus = function (userId, itemId, isFavorite) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = "Users/" + userId + "/FavoriteItems/" + itemId;

            var method = isFavorite ? "POST" : "DELETE";

            return self.ajax({
                type: method,
                url: url,
                dataType: "json"
            });
        };

        /**
         * Updates a user's personal rating for an item
         * @param {String} userId
         * @param {String} itemId
         * @param {Boolean} likes
         */
        self.updateUserItemRating = function (userId, itemId, likes) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/" + itemId + "/Rating", {
                likes: likes
            });

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
         * Clears a user's personal rating for an item
         * @param {String} userId
         * @param {String} itemId
         */
        self.clearUserItemRating = function (userId, itemId) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/" + itemId + "/Rating");

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };
    };

}(jQuery, navigator);

/**
 * Provides a friendly way to create an api client instance using information from the browser's current url
 */
MediaBrowser.ApiClient.create = function (clientName) {

    var loc = window.location;

    return new MediaBrowser.ApiClient(loc.protocol, loc.hostname, loc.port, clientName);
};