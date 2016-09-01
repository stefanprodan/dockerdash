using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerDash
{
    public class MainHub: Hub
    {
        private readonly DockerService _dockerService;

        public MainHub(DockerService dockerService)
        {
            _dockerService = dockerService;
        }

        public dynamic GetContainerList()
        {
            var list = _dockerService.GetContainerList();
            return list;
        }

        public string GetContainerLogs(string id, int tail)
        {
            return _dockerService.GetContainerLogs(id, tail);
        }

        public ContainerDetailsModel GetContainerDetails(string id)
        {
            return _dockerService.GetContainerDetails(id);
        }

        public HostModel GetHostInfo()
        {
            return _dockerService.GetHostInfo();
        }

        public dynamic GetMemoryStats(string id)
        {
            return _dockerService.GetMemoryStats(id);
        }

        public List<ImageModel> GetImageList()
        {
            return _dockerService.GetImageList();
        }

        public List<NetworkModel> GetNetworkList()
        {
            return _dockerService.GetNetworkList();
        }
    }
}
