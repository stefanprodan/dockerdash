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

var baseMixin = {
    data: function () {
        return {
            mainHub: $.connection.mainHub,
            loaded: false,
            alert: $('#alert')
        };
    },
    ready: function () {
        var $this = this;

        // append token to signalr query string
        $.connection.hub.qs = { 'authorization': auth.getAccessToken() };

        // enable SignalR console logging
        $.connection.hub.logging = true;

        // alert on slow connection
        $.connection.hub.connectionSlow(function () {
            $this.showAlert('We are currently experiencing difficulties with the SignalR connection');
        });

        // alert on connection error
        $.connection.hub.error(function (error) {

            // check if token has expired and logout
            if (error.context && error.context.status === 401)
            {
                $this.showAlert('Session expired, please login.');
                $this.$dispatch('do-logout');
            } else {
                $this.showAlert(error);
            }

        });

        // alert on reconnected
        $.connection.hub.reconnected(function () {
            $this.showAlert('Reconnected to SignalR hub, transport ' + $.connection.hub.transport.name);
        });

        // reconnect on wakeup for mobile devices
        ifvisible.on("wakeup", function () {
            if ($.connection.hub && $.connection.hub.state === $.signalR.connectionState.disconnected) {
                $.connection.hub.start();
            }
        });
    },
    methods: {
        showAlert: function (message) {
            this.alert.find("p").text(message);
            this.alert.show();
        }
    },
    filters: {
        truncate: function (val, len) {
            return val.substring(0, len);
        },
        statusGlyph: function (val) {
            if (val === "running") {
                return "glyphicon-play";
            }
            if (val === "paused") {
                return "glyphicon-pause";
            }
            if (val === "restarting") {
                return "glyphicon-refresh";
            }

            return "glyphicon-stop";
        }
    }
};

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

var host = Vue.extend({
    mixins: [baseMixin],
    template: '#host',
    data: function () {
        return {
            debouncer: null,
            timer: null,
            filterCon: '',
            filterImg: '',
            filterNet: '',
            countCon: 0,
            countImg: 0,
            countNet: 0,
            host : null,
            containers: null,
            images: null,
            networks: null,
        }
    },
    ready: function () {
        var $this = this;

        // subscribe to push events
        this.mainHub.client.onContainerEvent = this.onContainerEvent;

        // connect to SignalR hub
        $.connection.hub.start().done(function () {
            $this.loadData();
            $this.loaded = true;
        });
    },
    methods: {
        loadHost: function () {
            var $this = this;
            this.mainHub.server.getHostInfo().then(function (host) {
                $this.host = host;
            });
        },
        loadContainers: function () {
            var $this = this;
            this.mainHub.server.getContainerList().then(function (containers) {
                $this.containers = containers;
                $this.countCon = containers.length;
            });
        },
        onContainerEvent: function (event) {
            console.log(event);
            if (this.debouncer) clearTimeout(this.debouncer);
            this.debouncer = setTimeout(this.loadContainers, 1000);
        },
        loadImages: function () {
            var $this = this;
            this.mainHub.server.getImageList().then(function (images) {
                $this.images = images;
                $this.countImg = images.length;
            });
        },
        loadNetworks: function () {
            var $this = this;
            this.mainHub.server.getNetworkList().then(function (networks) {
                $this.networks = networks;
                $this.countNet = networks.length;
            });
        },
        loadData: function () {
            var $this = this;
            this.mainHub.server.getHost().then(function (data) {
                $this.host = data.host;
                $this.containers = data.containers;
                $this.countCon = data.containers.length;
                $this.images = data.images;
                $this.countImg = data.images.length;
                $this.networks = data.networks;
                $this.countNet = data.networks.length;
            });

            if (this.timer) clearTimeout(this.timer);
            this.timer = setTimeout(this.loadData, 30000);
        }
    },
    route: {
        deactivate: function () {
            if (this.timer) clearTimeout(this.timer);
            if (this.debouncer) clearTimeout(this.debouncer);
        }
    }
});

var container = Vue.extend({
    mixins: [baseMixin],
    template: '#container',
    data: function () {
        return {
            id: '',
            logs: '',
            memChart: null,
            mem: '',
            rxTotal: '',
            txTotal: '',
            iorxTotal: '',
            iotxTotal: '',
            cpuTime: '',
            pids: 0,
            timer: null,
            con: null
        }
    },
    ready: function () {
        this.id = this.$route.params.id;
        var $this = this;

        this.mainHub.client.onContainerEvent = this.onContainerEvent;
        $.connection.hub.start().done(function () {
            $this.loadData();
            $this.loaded = true;
        });
    },
    methods: {
        loadDetails: function () {
            var $this = this;
            this.mainHub.server.getContainerDetails(this.id).then(function (details) {
                if (details === undefined) {
                    $this.showAlert("Container not found");
                } else {
                    $this.con = details;
                    if ($this.con.State == "running") {
                        $this.loadStats();
                    } else {
                        $this.mem = null;
                    }
                }
            });
        },
        loadLogs: function () {
            var $this = this;
            this.mainHub.server.getContainerLogs(this.id, 1000).then(function (logs) {
                $this.logs = logs;
            });
        },
        loadStats: function () {
            var $this = this;
            this.mainHub.server.getContainerStats(this.id).then(function (data) {
                if (data) {
                    // update stats
                    $this.mem = data.memory.label;
                    $this.rxTotal = data.network.labelrx;
                    $this.txTotal = data.network.labeltx;
                    $this.iorxTotal = data.io.labelrx;
                    $this.iotxTotal = data.io.labeltx;
                    $this.pids = data.pids;
                    $this.cpuTime = data.cpuTime;
                }
                //// add new memory data
                //$this.memChart.data.labels.push(data.memory.label);
                //$this.memChart.data.datasets[0].data.push(data.memory.value);
                //$this.memChart.update();

                //// remove oldest memory point 
                //if ($this.memChart.data.datasets[0].data.length == 7) {
                //    $this.memChart.data.labels.splice(0, 1);
                //    $this.memChart.data.datasets[0].data.splice(0, 1);
                //    $this.memChart.update();
                //};
            });
        },
        loadData: function () {
            this.loadDetails();
            this.loadLogs();

            // enqueue new call after 30 seconds
            if (this.timer) clearTimeout(this.timer);
            this.timer = setTimeout(this.loadData, 30000);
        },
        lineGraph: function () {
            Chart.defaults.global.responsive = true;
            Chart.defaults.global.maintainAspectRatio = true;
            Chart.defaults.global.legend.display = false;
            var ctx = $("#lineChart");
            var data = {
                labels: [],
                datasets: [
                    {
                        label: "Memory",
                        fill: true,
                        backgroundColor: "rgba(15,80,136,0.4)",
                        pointBorderColor: "#fff",
                        pointBackgroundColor: "rgba(15,80,136,1)",
                        pointHoverBackgroundColor: "rgba(57,174,225,1)",
                        pointHoverBorderColor: "rgba(220,220,220,1)",
                        pointBorderWidth: 2,
                        pointRadius: 5,
                        data: [],
                        spanGaps: true,
                    }
                ]
            };
            var options = {
                tooltips: {
                    enabled: true,
                    mode: 'single',
                    callbacks: {
                        label: function (tooltipItems, data) {
                            return 'Memory';
                        }
                    }
                },
                scales:
                {
                    xAxes: [{
                        gridLines: {
                            display:false
                        },
                        ticks: {
                            display: false
                        }
                    }],
                    yAxes: [{
                        ticks: {
                            min: 0
                        }
                    }]
                }
            };
            this.memChart = new Chart(ctx, {
                type: 'line',
                data: data,
                options: options
            });
        }
    },
    route: {
        deactivate: function () {
            if (this.timer) clearTimeout(this.timer);
        }
    }
});

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