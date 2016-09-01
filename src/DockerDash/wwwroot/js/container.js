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
                // add new memory data
                $this.memChart.data.labels.push(data.memory.label);
                $this.memChart.data.datasets[0].data.push(data.memory.value);
                $this.memChart.update();

                // update memory current
                $this.mem = data.memory.label;

                // remove oldest memory point 
                if ($this.memChart.data.datasets[0].data.length == 7) {
                    $this.memChart.data.labels.splice(0, 1);
                    $this.memChart.data.datasets[0].data.splice(0, 1);
                    $this.memChart.update();
                };

                $this.rxTotal = data.network.labelrx;
                $this.txTotal = data.network.labeltx;
                $this.iorxTotal = data.io.labelrx;
                $this.iotxTotal = data.io.labeltx;
                $this.pids = data.pids;
                $this.cpuTime = data.cpuTime;

                // enqueue new call after 10 seconds
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
