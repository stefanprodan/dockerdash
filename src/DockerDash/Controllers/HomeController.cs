using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DockerDash.Controllers
{
    public class HomeController : Controller
    {
        private readonly DockerService _dockerService;

        public HomeController(DockerService dockerService)
        {
            _dockerService = dockerService;
            dockerService.MonitorEvents();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Ingest([FromBody]Payload payload)
        {
            return new EmptyResult();
        }

        public class Payload
        {
            public string Log { get; set; }
        }

        [HttpGet]
        public IActionResult Healthcheck()
        {
            return _dockerService.IsMonitoringEvents() ? StatusCode(200) : StatusCode(500);
        }
    }
}
