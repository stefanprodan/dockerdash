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
