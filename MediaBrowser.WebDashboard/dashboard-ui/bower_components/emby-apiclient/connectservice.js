define([], function () {

    function replaceAll(str, find, replace) {

        return str.split(find).join(replace);
    }

    return {

        cleanPassword: function (password) {

            password = password || '';

            password = replaceAll(password, "&", "&amp;");
            password = replaceAll(password, "/", "&#092;");
            password = replaceAll(password, "!", "&#33;");
            password = replaceAll(password, "$", "&#036;");
            password = replaceAll(password, "\"", "&quot;");
            password = replaceAll(password, "<", "&lt;");
            password = replaceAll(password, ">", "&gt;");
            password = replaceAll(password, "'", "&#39;");

            return password;
        }
    };
});