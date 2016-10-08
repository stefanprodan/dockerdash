var login = Vue.extend({
    template: '#login',
    data: function () {
        return {
            credentials: {
                username: '',
                password: ''
            },
            error: ''
        }
    },
    ready: function () {
        var $this = this;

        // remove token if exists
        if (localStorage.getItem('access_token')) {
            localStorage.removeItem('access_token');
        }
    },
    methods: {
        login: function () {
            var $this = this;

            // use application/x-www-form-urlencoded
            Vue.http.options.emulateJSON = true;

            this.$http.post(window.location.origin + '/token', this.credentials).then(function (response) {
                // store token in local storage
                var access_token = response.json().access_token;
                localStorage.setItem('access_token', access_token);

                // notify app that user is authenticated
                $this.$dispatch('on-login');

                // set global authorization header
                Vue.http.headers.common['Authorization'] = 'Bearer ' + access_token;

                // redirect to root
                router.go('/')

            }, function (response) {
                $this.error = response.body;
            });
        }
    },
    route: {
        deactivate: function () {

        }
    }
});
