using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ASP.NET_Project.Models;
using System.Security.Claims;
using ASP.NET_Project.Services;
using Microsoft.AspNetCore.Authorization;

namespace ASP.NET_Project.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [Authorize]
        public IActionResult About()
        {

            ViewData["Message"] = string.Format("Claims available for the user {0}", (User.FindFirst("name")?.Value));
            return View();
          
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }




    }
}
