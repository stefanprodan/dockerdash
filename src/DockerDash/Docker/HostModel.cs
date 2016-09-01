using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerDash
{
    public class HostModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long Containers { get; set; }
        public long ContainersRunning { get; set; }
        public long ContainersPaused { get; set; }
        public long ContainersStopped { get; set; }
        public long Images { get; set; }
        public string Driver { get; set; }
        public string LoggingDriver { get; set; }
        public string KernelVersion { get; set; }
        public string OperatingSystem { get; set; }
        public string OSType { get; set; }
        public string Architecture { get; set; }
        public long NCPU { get; set; }
        public string MemTotal { get; set; }
        public string ServerVersion { get; set; }
        public string DefaultRuntime { get; set; }
        public string ExecutionDriver { get; set; }
        public string CgroupDriver { get; set; }
        public long NGoroutines { get; set; }
        public string SwarmMode { get; set; }
        public long SwarmManagers { get; set; }
        public long SwarmNodes { get; set; }
        public long Networks { get; set; }
    }
}
