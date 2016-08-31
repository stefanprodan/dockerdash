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
    '/host': {
        component: host,
        name: 'host',
        title: 'Host'
    },
    '/container/:id': {
        component: container,
        name: 'container',
        title: 'Container details'
    },
    '/about': {
        component: {
            template: '#about'
        },
        name: 'about',
        title: 'About'
    }
})

var app = Vue.extend({});

router.start(app, 'html');
