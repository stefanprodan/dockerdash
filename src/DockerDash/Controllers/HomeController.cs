using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DockerDash.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(DockerService dockerService)
        {
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
    }
}
