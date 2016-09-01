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
                $this.host.Images = images.length;
            });
        },
        loadData: function () {
            this.loadHost();
            this.loadContainers();
            this.loadImages();
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
