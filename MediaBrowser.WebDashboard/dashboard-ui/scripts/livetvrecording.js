function deleteRecording(page, id) {

    Dashboard.confirm("Are you sure you wish to delete this recording?", "Confirm Recording Deletion", function (result) {

        if (result) {

            Dashboard.showLoadingMsg();

            ApiClient.deleteLiveTvRecording(id).done(function () {

                Dashboard.alert('Recording deleted');

                reload(page);
            });
        }

    });
}

