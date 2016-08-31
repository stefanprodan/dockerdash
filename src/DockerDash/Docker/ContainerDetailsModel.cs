using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerDash
{
    public class ContainerDetailsModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }

        /// <summary>
        /// created, restarting, running, paused, exited
        /// </summary>
        public string State { get; set; }
        public string Created { get; set; }
        public string IpAddress { get; set; }
        public string Ports { get; set; }
        public string MemoryUsage { get; set; }
        public string Command { get; set; }
        public string SizeRw { get; set; }
        public string SizeRootFs { get; set; }
        public long RestartCount { get; set; }
        public string Driver { get; set; }
    }
}
