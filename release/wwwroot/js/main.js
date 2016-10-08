var router = new VueRouter({
    history: true,
    mode: 'html5',
    linkActiveClass: 'active',
    transitionOnLoad: true,
    root: '/'
});

router.map({
    '/': {
        component: {
            template: '',
            ready: function () {
                this.$route.router.go('/host');
            }
        },
        name: 'home',
        title: 'Home'
    },
    '/login': {
        component: login,
        name: 'login',
        title: 'Login'
    },
    '/host': {
        component: host,
        name: 'host',
        title: 'Host',
        auth: true
    },
    '/container/:id': {
        component: container,
        name: 'container',
        title: 'Container details',
        auth: true
    },
    '/about': {
        component: {
            template: '#about'
        },
        name: 'about',
        title: 'About'
    }
});

router.beforeEach(function (transition) {
    if (transition.to.auth && !auth.checkAuth()) {
        transition.redirect('/login')
    } else {
        transition.next()
    }
});

var app = Vue.extend({
    data: function () {
        return {
            alert: $('#alert'),
            authenticated: false
        };
    },
    ready: function () {

        this.authenticated = auth.checkAuth();

        if (this.authenticated) {
            Vue.http.headers.common['Authorization'] = 'Bearer ' + auth.getAccessToken();
        }
    },
    methods: {
        logout: function () {
            localStorage.removeItem('access_token');
            this.authenticated = false;
            this.$route.router.go('/login');
        },
        showAlert: function (message) {
            this.alert.find("p").text(message);
            this.alert.show();
        },
        closeAlert: function () {
            this.alert.hide();
        }
    },
    events: {
        'on-login': function () {
            this.authenticated = true;
        },
        'do-logout': function () {
            this.logout();
        }
    }
});

router.start(app, 'html');