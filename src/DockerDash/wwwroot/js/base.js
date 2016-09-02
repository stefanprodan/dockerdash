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

        $.connection.hub.qs = { 'authorization': auth.getAccessToken() };

        // enable SignalR console logging
        $.connection.hub.logging = true;

        // alert on slow connection
        $.connection.hub.connectionSlow(function () {
            $this.showAlert('We are currently experiencing difficulties with the SignalR connection');
        });

        // alert on connection error
        $.connection.hub.error(function (error) {
            if (error.context && error.context.status == 401)
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
    },
    methods: {
        showAlert: function (message) {
            this.alert.find("p").text(message);
            this.alert.show();
        },
        isAuthenticated: function () {
            auth.checkAuth();
        }
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
