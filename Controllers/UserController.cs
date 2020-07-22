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
using ASP.NET_Project.EntityFramework;

namespace ASP.NET_Project.Controllers
{
    [Authorize(Roles="Admin")]
    public class UserController : Controller
    {
        private readonly DataContext _context;


        public UserController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View( _context.Users.Where(x=>x.Role!="Admin").ToList());
        }

        [HttpPost]
        public async Task<ActionResult> Change(int id)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user.Role == "User") user.Role = "HR";
            else if (user.Role == "HR") user.Role = "User";
            _context.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");

        }
    }
}
