define(["localassetmanager", "cameraRoll"], function(localAssetManager, cameraRoll) {
    "use strict";

    function getFilesToUpload(files, uploadHistory) {
        return files.filter(function(file) {
            if (!file) return !1;
            var uploadId = getUploadId(file);
            return 0 === uploadHistory.FilesUploaded.filter(function(u) {
                return uploadId === u.Id
            }).length
        })
    }

    function getUploadId(file) {
        return btoa(file.Id + "1")
    }

    function uploadNext(files, index, server, apiClient, resolve, reject) {
        var length = files.length;
        if (index >= length) return void resolve();
        uploadFile(files[index], apiClient).then(function() {
            uploadNext(files, index + 1, server, apiClient, resolve, reject)
        }, function() {
            uploadNext(files, index + 1, server, apiClient, resolve, reject)
        })
    }

    function uploadFile(file, apiClient) {
        return new Promise(function(resolve, reject) {
            require(["fileupload"], function(FileUpload) {
                var url = apiClient.getUrl("Devices/CameraUploads", {
                    DeviceId: apiClient.deviceId(),
                    Name: file.Name,
                    Album: "Camera Roll",
                    Id: getUploadId(file),
                    api_key: apiClient.accessToken()
                });
                console.log("Uploading file to " + url), (new FileUpload).upload(file, url).then(resolve, reject)
            })
        })
    }

    function ContentUploader() {}
    return ContentUploader.prototype.uploadImages = function(connectionManager, server) {
        return cameraRoll.getFiles().then(function(photos) {
            if (!photos.length) return Promise.resolve();
            var apiClient = connectionManager.getApiClient(server.Id);
            return apiClient.getContentUploadHistory().then(function(uploadHistory) {
                return photos = getFilesToUpload(photos, uploadHistory), console.log("Found " + photos.length + " files to upload"), new Promise(function(resolve, reject) {
                    uploadNext(photos, 0, server, apiClient, resolve, reject)
                })
            }, function() {
                return Promise.resolve()
            })
        })
    }, ContentUploader
});