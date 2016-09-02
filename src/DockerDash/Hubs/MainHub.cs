using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerDash
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class MainHub: Hub
    {
        private readonly DockerService _dockerService;

        public MainHub(DockerService dockerService)
        {
            _dockerService = dockerService;
        }

        public dynamic GetHost()
        {
            return new
            {
                host = _dockerService.GetHostInfo(),
                containers = _dockerService.GetContainerList(),
                images = _dockerService.GetImageList(),
                networks = _dockerService.GetNetworkList()
            };
        }

        public dynamic GetHostInfo()
        {
            return _dockerService.GetHostInfo();
        }

        public dynamic GetContainerList()
        {
            var list = _dockerService.GetContainerList();
            return list;
        }

        public dynamic GetImageList()
        {
            return _dockerService.GetImageList();
        }

        public dynamic GetNetworkList()
        {
            return _dockerService.GetNetworkList();
        }

        public dynamic GetContainerDetails(string id)
        {
            return _dockerService.GetContainerDetails(id);
        }

        public dynamic GetContainerLogs(string id, int tail)
        {
            return _dockerService.GetContainerLogs(id, tail);
        }

        public dynamic GetContainerStats(string id)
        {
            return _dockerService.GetContainerStats(id);
        }
    }
}
