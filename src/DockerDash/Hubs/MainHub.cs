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

        public async Task<dynamic> GetHost()
        {
            return new
            {
                host = await _dockerService.GetHostInfo(),
                containers = await _dockerService.GetContainerList(),
                images = await _dockerService.GetImageList(),
                networks = await _dockerService.GetNetworkList()
            };
        }

        public async Task<dynamic> GetHostInfo()
        {
            return await _dockerService.GetHostInfo();
        }

        public async Task<dynamic> GetContainerList()
        {
            return await _dockerService.GetContainerList();
        }

        public async Task<dynamic> GetImageList()
        {
            return await _dockerService.GetImageList();
        }

        public async Task<dynamic> GetNetworkList()
        {
            return await _dockerService.GetNetworkList();
        }

        public async Task<dynamic> GetContainerDetails(string id)
        {
            return await _dockerService.GetContainerDetails(id);
        }

        public async Task<dynamic> GetContainerLogs(string id, int tail)
        {
            return await _dockerService.GetContainerLogs(id, tail);
        }

        public async Task<dynamic> GetContainerStats(string id)
        {
            return await _dockerService.GetContainerStats(id);
        }
    }
}
