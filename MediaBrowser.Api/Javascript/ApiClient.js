/**
 * Represents a javascript version of ApiClient.
 * This should be kept up to date with all possible api methods and parameters
 */
var ApiClient = {

    serverProtocol: "http",

    /**
     * Gets or sets the host name of the server
     */
    serverHostName: "localhost",

    serverPortNumber: 8096,

    /**
     * Detects the hostname and port of MB server based on the current url
     */
    inferServerFromUrl: function () {

        var loc = window.location;

        ApiClient.serverProtocol = loc.protocol;
        ApiClient.serverHostName = loc.hostname;
        ApiClient.serverPortNumber = loc.port;
    },

    /**
     * Creates an api url based on a handler name and query string parameters
     * @param {String} name
     * @param {Object} params
     */
    getUrl: function (name, params) {

        if (!name) {
            throw new Error("Url name cannot be empty");
        }

        params = params || {};

        var url = ApiClient.serverProtocol + "//" + ApiClient.serverHostName + ":" + ApiClient.serverPortNumber + "/mediabrowser/" + name;

        if (params) {
            url += "?" + $.param(params);

        }
        return url;
    },

    /**
     * Returns the name of the current browser
     */
    getDeviceName: function () {

        /*if ($.browser.chrome) {
            return "Chrome";
        }
        if ($.browser.safari) {
            return "Safari";
        }
        if ($.browser.webkit) {
            return "WebKit";
        }
        if ($.browser.msie) {
            return "Internet Explorer";
        }
        if ($.browser.firefox) {
            return "Firefox";
        }
        if ($.browser.mozilla) {
            return "Firefox";
        }
        if ($.browser.opera) {
            return "Opera";
        }*/

        return "Web Browser";
    },

    /**
     * Creates a custom api url based on a handler name and query string parameters
     * @param {String} name
     * @param {Object} params
     */
    getCustomUrl: function (name, params) {

        if (!name) {
            throw new Error("Url name cannot be empty");
        }

        params = params || {};
        params.client = "Dashboard";
        params.device = ApiClient.getDeviceName();
        params.format = "json";

        var url = ApiClient.serverProtocol + "//" + ApiClient.serverHostName + ":" + ApiClient.serverPortNumber + "/mediabrowser/" + name;

        if (params) {
            url += "?" + $.param(params);

        }
        return url;
    },

    /**
     * Gets an item from the server
     * Omit itemId to get the root folder.
     */
    getItem: function (userId, itemId) {

        if (!userId) {
            throw new Error("null userId");
        }

        var url = ApiClient.getUrl("Users/" + userId + "/Items/" + itemId);

        return $.getJSON(url);
    },

    /**
     * Gets the root folder from the server
     */
    getRootFolder: function (userId) {

        return ApiClient.getItem(userId);
    },

    /**
     * Gets the current server status
     */
    getSystemInfo: function () {

        var url = ApiClient.getUrl("System/Info");

        return $.getJSON(url);
    },

    /**
     * Gets all cultures known to the server
     */
    getCultures: function () {

        var url = ApiClient.getUrl("Localization/cultures");

        return $.getJSON(url);
    },

    /**
     * Gets all countries known to the server
     */
    getCountries: function () {

        var url = ApiClient.getUrl("Localization/countries");

        return $.getJSON(url);
    },

    /**
     * Gets plugin security info
     */
    getPluginSecurityInfo: function () {

        var url = ApiClient.getUrl("Plugins/SecurityInfo");

        return $.getJSON(url);
    },

    /**
     * Gets the directory contents of a path on the server
     */
    getDirectoryContents: function (path, options) {

        if (!path) {
            throw new Error("null path");
        }

        options = options || {};

        options.path = path;

        var url = ApiClient.getUrl("Environment/DirectoryContents", options);

        return $.getJSON(url);
    },

    /**
     * Gets a list of physical drives from the server
     */
    getDrives: function () {

        var url = ApiClient.getUrl("Environment/Drives");

        return $.getJSON(url);
    },

    /**
     * Gets a list of network devices from the server
     */
    getNetworkDevices: function () {

        var url = ApiClient.getUrl("Environment/NetworkDevices");

        return $.getJSON(url);
    },

    /**
     * Cancels a package installation
     */
    cancelPackageInstallation: function (installationId) {

        if (!installationId) {
            throw new Error("null installationId");
        }

        var url = ApiClient.getUrl("Packages/Installing/" + id);

        return $.ajax({
            type: "DELETE",
            url: url,
            dataType: "json"
        });
    },

    /**
     * Installs or updates a new plugin
     */
    installPlugin: function (name, updateClass, version) {

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

        var url = ApiClient.getUrl("Packages/Installed/" + name, options);

        return $.post(url);
    },

    /**
     * Instructs the server to perform a pending kernel reload or app restart.
     * If a restart is not currently required, nothing will happen.
     */
    performPendingRestart: function () {

        var url = ApiClient.getUrl("System/Restart");

        return $.post(url);
    },

    /**
     * Gets information about an installable package
     */
    getPackageInfo: function (name) {

        if (!name) {
            throw new Error("null name");
        }

        var url = ApiClient.getUrl("Packages/" + name);

        return $.getJSON(url);
    },

    /**
     * Gets the latest available application update (if any)
     */
    getAvailableApplicationUpdate: function () {

        var url = ApiClient.getUrl("Packages/Updates", { PackageType: "System" });

        return $.getJSON(url);
    },

    /**
     * Gets the latest available plugin updates (if any)
     */
    getAvailablePluginUpdates: function () {

        var url = ApiClient.getUrl("Packages/Updates", { PackageType: "UserInstalled" });

        return $.getJSON(url);
    },

    /**
     * Gets the virtual folder for a view. Specify a userId to get a user view, or omit for the default view.
     */
    getVirtualFolders: function (userId) {

        var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

        url = ApiClient.getUrl(url);

        return $.getJSON(url);
    },

    /**
     * Gets all the paths of the locations in the physical root.
     */
    getPhysicalPaths: function () {

        var url = ApiClient.getUrl("Library/PhysicalPaths");

        return $.getJSON(url);
    },

    /**
     * Gets the current server configuration
     */
    getServerConfiguration: function () {

        var url = ApiClient.getUrl("System/Configuration");

        return $.getJSON(url);
    },

    /**
     * Gets the server's scheduled tasks
     */
    getScheduledTasks: function () {

        var url = ApiClient.getUrl("ScheduledTasks");

        return $.getJSON(url);
    },

    /**
    * Starts a scheduled task
    */
    startScheduledTask: function (id) {

        if (!id) {
            throw new Error("null id");
        }

        var url = ApiClient.getUrl("ScheduledTasks/Running/" + id);

        return $.post(url);
    },

    /**
    * Gets a scheduled task
    */
    getScheduledTask: function (id) {

        if (!id) {
            throw new Error("null id");
        }

        var url = ApiClient.getUrl("ScheduledTasks/" + id);

        return $.getJSON(url);
    },

    /**
   * Stops a scheduled task
   */
    stopScheduledTask: function (id) {

        if (!id) {
            throw new Error("null id");
        }

        var url = ApiClient.getUrl("ScheduledTasks/Running/" + id);

        return $.ajax({
            type: "DELETE",
            url: url,
            dataType: "json"
        });
    },

    /**
     * Gets the configuration of a plugin
     * @param {String} Id
     */
    getPluginConfiguration: function (id) {

        if (!id) {
            throw new Error("null Id");
        }

        var url = ApiClient.getUrl("Plugins/" + id + "/Configuration");

        return $.getJSON(url);
    },

    /**
     * Gets a list of plugins that are available to be installed
     */
    getAvailablePlugins: function () {

        var url = ApiClient.getUrl("Packages", { PackageType: "UserInstalled" });

        return $.getJSON(url);
    },

    /**
     * Uninstalls a plugin
     * @param {String} Id
     */
    uninstallPlugin: function (id) {

        if (!id) {
            throw new Error("null Id");
        }

        var url = ApiClient.getUrl("Plugins/" + id);

        return $.ajax({
            type: "DELETE",
            url: url,
            dataType: "json"
        });
    },

    /**
    * Removes a virtual folder from either the default view or a user view
    * @param {String} name
    */
    removeVirtualFolder: function (name, userId) {

        if (!name) {
            throw new Error("null name");
        }

        var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

        url += "/" + name;
        url = ApiClient.getUrl(url);

        return $.ajax({
            type: "DELETE",
            url: url,
            dataType: "json"
        });
    },

    /**
   * Adds a virtual folder to either the default view or a user view
   * @param {String} name
   */
    addVirtualFolder: function (name, userId) {

        if (!name) {
            throw new Error("null name");
        }

        var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

        url += "/" + name;
        url = ApiClient.getUrl(url);

        return $.post(url);
    },

    /**
   * Renames a virtual folder, within either the default view or a user view
   * @param {String} name
   */
    renameVirtualFolder: function (name, newName, userId) {

        if (!name) {
            throw new Error("null name");
        }

        var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

        url += "/" + name + "/Name";

        url = ApiClient.getUrl(url, { newName: newName });

        return $.post(url);
    },

    /**
    * Adds an additional mediaPath to an existing virtual folder, within either the default view or a user view
    * @param {String} name
    */
    addMediaPath: function (virtualFolderName, mediaPath, userId) {

        if (!virtualFolderName) {
            throw new Error("null virtualFolderName");
        }

        if (!mediaPath) {
            throw new Error("null mediaPath");
        }

        var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

        url += "/" + virtualFolderName + "/Paths";

        url = ApiClient.getUrl(url, { path: mediaPath });

        return $.post(url);
    },

    /**
    * Removes a media path from a virtual folder, within either the default view or a user view
    * @param {String} name
    */
    removeMediaPath: function (virtualFolderName, mediaPath, userId) {

        if (!virtualFolderName) {
            throw new Error("null virtualFolderName");
        }

        if (!mediaPath) {
            throw new Error("null mediaPath");
        }

        var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

        url += "/" + virtualFolderName + "/Paths";

        url = ApiClient.getUrl(url, { path: mediaPath });

        return $.ajax({
            type: "DELETE",
            url: url,
            dataType: "json"
        });
    },

    /**
     * Deletes a user
     * @param {String} id
     */
    deleteUser: function (id) {

        if (!id) {
            throw new Error("null id");
        }

        var url = ApiClient.getUrl("Users/" + id);

        return $.ajax({
            type: "DELETE",
            url: url,
            dataType: "json"
        });
    },

    /**
     * Deletes a user image
     * @param {String} userId
     * @param {String} imageType The type of image to delete, based on the server-side ImageType enum.
     */
    deleteUserImage: function (userId, imageType) {

        if (!userId) {
            throw new Error("null userId");
        }

        if (!imageType) {
            throw new Error("null imageType");
        }

        var url = ApiClient.getUrl("Users/" + userId + "/Images/" + imageType);

        return $.ajax({
            type: "DELETE",
            url: url,
            dataType: "json"
        });
    },

    /**
     * Uploads a user image
     * @param {String} userId
     * @param {String} imageType The type of image to delete, based on the server-side ImageType enum.
     * @param {Object} file The file from the input element
     */
    uploadUserImage: function (userId, imageType, file) {

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

            var url = ApiClient.getUrl("Users/" + userId + "/Images/" + imageType);

            $.ajax({
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
    },

    /**
     * Gets the list of installed plugins on the server
     */
    getInstalledPlugins: function () {

        var url = ApiClient.getUrl("Plugins");

        return $.getJSON(url);
    },

    /**
     * Gets a user by id
     * @param {String} id
     */
    getUser: function (id) {

        if (!id) {
            throw new Error("Must supply a userId");
        }

        var url = ApiClient.getUrl("Users/" + id);

        return $.getJSON(url);
    },

    /**
     * Gets a studio
     */
    getStudio: function (name) {

        if (!name) {
            throw new Error("null name");
        }

        var url = ApiClient.getUrl("Studios/" + name);

        return $.getJSON(url);
    },

    /**
     * Gets a genre
     */
    getGenre: function (name) {

        if (!name) {
            throw new Error("null name");
        }

        var url = ApiClient.getUrl("Genres/" + name);

        return $.getJSON(url);
    },

    /**
     * Gets a year
     */
    getYear: function (year) {

        if (!year) {
            throw new Error("null year");
        }

        var url = ApiClient.getUrl("Years/" + year);

        return $.getJSON(url);
    },

    /**
     * Gets a Person
     */
    getPerson: function (name) {

        if (!name) {
            throw new Error("null name");
        }

        var url = ApiClient.getUrl("Persons/" + name);

        return $.getJSON(url);
    },

    /**
     * Gets weather info
     * @param {String} location - us zip code / city, state, country / city, country
     * Omit location to get weather info using stored server configuration value
     */
    getWeatherInfo: function (location) {

        var url = ApiClient.getUrl("weather", {
            location: location
        });

        return $.getJSON(url);
    },

    /**
     * Gets all users from the server
     */
    getAllUsers: function () {

        var url = ApiClient.getUrl("users");

        return $.getJSON(url);
    },

    /**
     * Gets all available parental ratings from the server
     */
    getParentalRatings: function () {

        var url = ApiClient.getUrl("Localization/ParentalRatings");

        return $.getJSON(url);
    },

    /**
     * Gets a list of all available conrete BaseItem types from the server
     */
    getItemTypes: function (options) {

        var url = ApiClient.getUrl("Library/ItemTypes", options);

        return $.getJSON(url);
    },

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
    getUserImageUrl: function (userId, options) {

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

        return ApiClient.getUrl(url, options);
    },

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
    getPersonImageUrl: function (name, options) {

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

        return ApiClient.getUrl(url, options);
    },

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
    getYearImageUrl: function (year, options) {

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

        return ApiClient.getUrl(url, options);
    },

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
    getGenreImageUrl: function (name, options) {

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

        return ApiClient.getUrl(url, options);
    },

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
    getStudioImageUrl: function (name, options) {

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

        return ApiClient.getUrl(url, options);
    },

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
    getImageUrl: function (itemId, options) {

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

        return ApiClient.getUrl(url, options);
    },

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
    getLogoImageUrl: function (item, options) {

        if (!item) {
            throw new Error("null item");
        }

        options = options || {
        };

        options.imageType = "logo";

        var logoItemId = item.HasLogo ? item.Id : item.ParentLogoItemId;

        return logoItemId ? ApiClient.getImageUrl(logoItemId, options) : null;
    },

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
    getBackdropImageUrl: function (item, options) {

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

            files[i] = ApiClient.getImageUrl(backdropItemId, options);
        }

        return files;
    },

    /**
     * Authenticates a user
     * @param {String} userId
     * @param {String} password
     */
    authenticateUser: function (userId, password) {

        if (!userId) {
            throw new Error("null userId");
        }

        var url = ApiClient.getUrl("Users/" + userId + "/authenticate");

        var postData = {
        };

        if (password) {
            postData.password = password;
        }
        
        return $.ajax({
            type: "POST",
            url: url,
            data: JSON.stringify(postData),
            dataType: "json",
            contentType: "application/json"
        });
    },

    /**
     * Updates a user's password
     * @param {String} userId
     * @param {String} currentPassword
     * @param {String} newPassword
     */
    updateUserPassword: function (userId, currentPassword, newPassword) {

        if (!userId) {
            throw new Error("null userId");
        }

        var url = ApiClient.getUrl("Users/" + userId + "/Password");

        var postData = {
        };

        if (currentPassword) {
            postData.currentPassword = currentPassword;
        }
        if (newPassword) {
            postData.newPassword = newPassword;
        }
        return $.post(url, postData);
    },

    /**
    * Resets a user's password
    * @param {String} userId
    */
    resetUserPassword: function (userId) {

        if (!userId) {
            throw new Error("null userId");
        }

        var url = ApiClient.getUrl("Users/" + userId + "/Password");

        var postData = {
        };

        postData.resetPassword = 1;
        return $.post(url, postData);
    },

    /**
     * Updates the server's configuration
     * @param {Object} configuration
     */
    updateServerConfiguration: function (configuration) {

        if (!configuration) {
            throw new Error("null configuration");
        }

        var url = ApiClient.getUrl("System/Configuration");

        return $.ajax({
            type: "POST",
            url: url,
            data: JSON.stringify(configuration),
            dataType: "json",
            contentType: "application/json"
        });
    },

    /**
     * Updates plugin security info
     */
    updatePluginSecurityInfo: function (info) {

        var url = ApiClient.getUrl("Plugins/SecurityInfo");

        return $.ajax({
            type: "POST",
            url: url,
            data: JSON.stringify(info),
            dataType: "json",
            contentType: "application/json"
        });
    },

    /**
     * Creates a user
     * @param {Object} user
     */
    createUser: function (user) {

        if (!user) {
            throw new Error("null user");
        }

        var url = ApiClient.getUrl("Users");

        return $.ajax({
            type: "POST",
            url: url,
            data: JSON.stringify(user),
            dataType: "json",
            contentType: "application/json"
        });
    },

    /**
     * Updates a user
     * @param {Object} user
     */
    updateUser: function (user) {

        if (!user) {
            throw new Error("null user");
        }

        var url = ApiClient.getUrl("Users/" + user.Id);

        return $.ajax({
            type: "POST",
            url: url,
            data: JSON.stringify(user),
            dataType: "json",
            contentType: "application/json"
        });
    },

    /**
     * Updates the Triggers for a ScheduledTask
     * @param {String} id
     * @param {Object} triggers
     */
    updateScheduledTaskTriggers: function (id, triggers) {

        if (!id) {
            throw new Error("null id");
        }

        if (!triggers) {
            throw new Error("null triggers");
        }

        var url = ApiClient.getUrl("ScheduledTasks/" + id + "/Triggers");

        return $.ajax({
            type: "POST",
            url: url,
            data: JSON.stringify(triggers),
            dataType: "json",
            contentType: "application/json"
        });
    },

    /**
     * Updates a plugin's configuration
     * @param {String} Id
     * @param {Object} configuration
     */
    updatePluginConfiguration: function (id, configuration) {

        if (!id) {
            throw new Error("null Id");
        }

        if (!configuration) {
            throw new Error("null configuration");
        }

        var url = ApiClient.getUrl("Plugins/" + id + "/Configuration");

        return $.ajax({
            type: "POST",
            url: url,
            data: JSON.stringify(configuration),
            dataType: "json",
            contentType: "application/json"
        });
    },

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
    getItems: function (userId, options) {

        if (!userId) {
            throw new Error("null userId");
        }

        return $.getJSON(ApiClient.getUrl("Users/" + userId + "/Items", options));
    },

    /**
     * Marks an item as played or unplayed
     * This should not be used to update playstate following playback.
     * There are separate playstate check-in methods for that. This should be used for a
     * separate option to reset playstate.
     * @param {String} userId
     * @param {String} itemId
     * @param {Boolean} wasPlayed
     */
    updatePlayedStatus: function (userId, itemId, wasPlayed) {

        if (!userId) {
            throw new Error("null userId");
        }

        if (!itemId) {
            throw new Error("null itemId");
        }

        var url = "Users/" + userId + "/PlayedItems/" + itemId;

        var method = wasPlayed ? "POST" : "DELETE";

        return $.ajax({
            type: method,
            url: url,
            dataType: "json"
        });
    },

    /**
     * Updates a user's favorite status for an item and returns the updated UserItemData object.
     * @param {String} userId
     * @param {String} itemId
     * @param {Boolean} isFavorite
     */
    updateFavoriteStatus: function (userId, itemId, isFavorite) {

        if (!userId) {
            throw new Error("null userId");
        }

        if (!itemId) {
            throw new Error("null itemId");
        }

        var url = "Users/" + userId + "/FavoriteItems/" + itemId;

        var method = isFavorite ? "POST" : "DELETE";

        return $.ajax({
            type: method,
            url: url,
            dataType: "json"
        });
    },

    /**
     * Updates a user's personal rating for an item
     * @param {String} userId
     * @param {String} itemId
     * @param {Boolean} likes
     */
    updateUserItemRating: function (userId, itemId, likes) {

        if (!userId) {
            throw new Error("null userId");
        }

        if (!itemId) {
            throw new Error("null itemId");
        }

        var url = ApiClient.getUrl("Users/" + userId + "/Items/" + itemId + "/Rating", {
            likes: likes
        });

        return $.post(url);
    },

    /**
     * Clears a user's personal rating for an item
     * @param {String} userId
     * @param {String} itemId
     */
    clearUserItemRating: function (userId, itemId) {

        if (!userId) {
            throw new Error("null userId");
        }

        if (!itemId) {
            throw new Error("null itemId");
        }

        var url = ApiClient.getUrl("Users/" + userId + "/Items/" + itemId + "/Rating");

        return $.ajax({
            type: "DELETE",
            url: url,
            dataType: "json"
        });
    }
};

// Do this initially. The consumer can always override later
ApiClient.inferServerFromUrl();
