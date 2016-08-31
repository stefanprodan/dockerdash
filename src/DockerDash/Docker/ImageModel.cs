using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerDash
{
    public class ImageModel
    {
        public string Id { get; set; }
        public string ParentID { get; set; }
        public string Name { get; set; }
        public string RepoTags { get; set; }
        public IList<string> RepoDigests { get; set; }
        public string Created { get; set; }
        public string Size { get; set; }
        public string VirtualSize { get; set; }
        public string Labels { get; set; }
    }
}
