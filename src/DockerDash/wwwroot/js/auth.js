var auth = {
    user: {
        authenticated: false
    }
    ,
    login(context, creds, redirect) {
        $this = this;
        // use application/x-www-form-urlencoded
        Vue.http.options.emulateJSON = true;
        context.$http.post(window.location.origin + '/token', creds).then((response) => {

            var access_token = response.json().access_token;
            localStorage.setItem('access_token', access_token);
            $this.user.authenticated = true;

            context.$dispatch('on-login');

            Vue.http.headers.common['Authorization'] = 'Bearer ' + $this.getAccessToken();

            if (redirect) {
                router.go(redirect)
            }

        }, (response) => {
            context.error = response.body;
        });
    },
    checkAuth() {
        if (localStorage.getItem('access_token')) {
            this.user.authenticated = true;
        }
        else {
            this.user.authenticated = false;
        }

        return this.user.authenticated;
    },
    getAccessToken() {
        return localStorage.getItem('access_token')
    },
    logout() {
        localStorage.removeItem('access_token')
        this.user.authenticated = false
    }
};
