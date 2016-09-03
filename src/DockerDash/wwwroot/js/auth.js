var auth = function () {
    return {
        login: function (context, creds, redirect) {
            $this = this;
            // use application/x-www-form-urlencoded
            Vue.http.options.emulateJSON = true;

            context.$http.post(window.location.origin + '/token', creds).then(function (response) {

                var access_token = response.json().access_token;
                localStorage.setItem('access_token', access_token);
                context.$dispatch('on-login');

                Vue.http.headers.common['Authorization'] = 'Bearer ' + $this.getAccessToken();

                if (redirect) {
                    router.go(redirect)
                }

            }, function (response) {
                context.error = response;
            });
        },
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
        },
        logout: function () {
            localStorage.removeItem('access_token');
        }
    };
}();
