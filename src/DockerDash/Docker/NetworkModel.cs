using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerDash
{
    public class NetworkModel
    {
        public string Id { get; set; }
        public string Scope { get; set; }
        public string Name { get; set; }
        public string Driver { get; set; }
        public int Containers { get; set; }
    }
}
