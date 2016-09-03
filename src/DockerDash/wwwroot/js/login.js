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
    },
    methods: {
        submit: function() {
            var credentials = {
                username: this.credentials.username,
                password: this.credentials.password
            }
            auth.login(this, credentials, 'host')
        }
    },
    route: {
        deactivate: function () {

        }
    }
});
