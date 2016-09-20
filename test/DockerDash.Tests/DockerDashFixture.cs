using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerDash.Tests
{
    public class DockerDashFixture<TStartup> : DockerDashFixtureBase<TStartup>
        where TStartup : class
    {
        public DockerDashFixture() : base("http://localhost:5000")
        {
        }
    }
}
