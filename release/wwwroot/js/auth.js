var auth = function () {
    return {
        checkAuth: function () {
            if (localStorage.getItem('access_token')) {
                return true;
            }
            else {
                return false;
            }
        },
        getAccessToken: function () {
            return localStorage.getItem('access_token');
        }
    };
}();
