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
