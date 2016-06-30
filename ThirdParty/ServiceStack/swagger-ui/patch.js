$("#input_apiKey").parent().replaceWith(
    "<div class='input'><input type='text' id='txtUsername' placeholder='Username' style='width:100px'/></div>" +
    "<div class='input'><input type='password' id='txtPassword' placeholder='Password' style='width:100px'/></div>");

$('#txtUsername').change(addBasicAuth);
$('#txtPassword').change(addBasicAuth);

function addBasicAuth() {
    var username = $('#txtUsername').val().replace(/^\s+|\s+$/gm, ''); //trim
    var password = $('#txtPassword').val().replace(/^\s+|\s+$/gm, '');
    if (username && password) {
        window.authorizations.add("basic", new PasswordAuthorization("basic", username, password));
    }
}
