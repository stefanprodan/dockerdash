var baseMixin = {
    data: function () {
        return {
            mainHub: $.connection.mainHub,
            loaded: false
        };
    },
    filters: {
        truncate: function (val, len) {
            return val.substring(0, len);
        },
        statusGlyph: function (val) {
            if (val == "running") {
                return "glyphicon-play";
            }
            if (val == "paused") {
                return "glyphicon-pause";
            }
            if (val == "restarting") {
                return "glyphicon-refresh";
            }

            return "glyphicon-stop";
        }
    }
};

var host = Vue.extend({
    mixins: [baseMixin],
    template: '#host',
    data: function () {
        return {
            debouncer: null,
            timer: null,
            filterCon: '',
            filterImg: '',
            host : null,
            containers: null,
            images: null
        }
    },
    ready: function () {
        var $this = this;
        $.connection.hub.logging = true;
        this.mainHub.client.onContainerEvent = this.onContainerEvent;
        $.connection.hub.start().done(function () {
            $this.loadHost();
            $this.loadLists();
            $this.loaded = true;
        }).fail(function () {
            //log error
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
            });
        },
        loadLists: function () {
            this.loadContainers();
            this.loadImages();
            if (this.timer) clearTimeout(this.timer);
            this.timer = setTimeout(this.loadLists, 30000);
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
            timer: null,
            con: null
        }
    },
    ready: function () {
        this.id = this.$route.params.id;
        var $this = this;
        $.connection.hub.logging = true;
        this.mainHub.client.onContainerEvent = this.onContainerEvent;
        $.connection.hub.start().done(function () {
            $this.loadDetails();
            $this.loadLogs();
            $this.loaded = true;
        }).fail(function () {
            //log error
        });
    },
    methods: {
        loadDetails: function () {
            var $this = this;
            this.mainHub.server.getContainerDetails(this.id).then(function (details) {
                $this.con = details;
                if ($this.con.State == "running") {
                    $this.lineGraph();
                    $this.getMemoryStats();
                }
            });
        },
        loadLogs: function () {
            var $this = this;
            this.mainHub.server.getContainerLogs(this.id, 1000).then(function (logs) {
                $this.logs = logs;
            });
        },
        getMemoryStats: function(){
            var $this = this;
            this.mainHub.server.getMemoryStats(this.id).then(function (data) {
                // Add new data
                $this.memChart.data.labels.push(data.label); // add new label at end
                $this.memChart.data.datasets[0].data.push(data.value);

                $this.memChart.update();
                $this.mem = data.label;
                if ($this.memChart.data.datasets[0].data.length == 6) {
                    $this.memChart.data.labels.splice(0, 1);
                    $this.memChart.data.datasets[0].data.splice(0, 1);
                    $this.memChart.update();
                };
                if ($this.timer) clearTimeout($this.timer);
                $this.timer = setTimeout($this.getMemoryStats, 10000);
            });
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