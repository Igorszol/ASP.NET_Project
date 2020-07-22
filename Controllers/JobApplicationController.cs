using ASP.NET_Project.EntityFramework;
using ASP.NET_Project.Models;
using ASP.NET_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASP.NET_Project.Controllers
{
    [Authorize]
    public class JobApplicationController : Controller
    {
        private readonly DataContext _context;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IConfiguration _configuration;

        public JobApplicationController(DataContext context, IAzureBlobService azureBlobService, IConfiguration configuration)
        {

            _configuration = configuration;
            _context = context;
            _azureBlobService = azureBlobService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery(Name = "search")] string searchString)
    
        {
            var user = FindUser(User);
            List<JobApplication> searchResult= new List<JobApplication>();
            if (User.IsInRole("Admin"))
            {
            if (string.IsNullOrEmpty(searchString))
                return View(await _context.JobApplications.Include(x => x.JobOffer).Include(x=>x.JobOffer.Company).ToListAsync());

            searchResult = await _context.JobApplications.Include(x => x.JobOffer).Include(x => x.JobOffer.Company)
                .Where(o => o.JobOffer.JobTitle.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();
            }

            if (User.IsInRole("HR"))
            {
                if (string.IsNullOrEmpty(searchString))
                    return View(await _context.JobApplications.Include(x => x.JobOffer).Include(x => x.JobOffer.Company).Where(o=>o.JobOffer.UserId==user.Id).ToListAsync());

                searchResult = await _context.JobApplications.Include(x => x.JobOffer).Include(x => x.JobOffer.Company)
                    .Where(o => o.JobOffer.UserId == user.Id).Where(o => o.JobOffer.JobTitle.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .ToListAsync();
            }

            if (User.IsInRole("User"))
            {
                List<JobApplication> applications = new List<JobApplication>();

                if (string.IsNullOrEmpty(searchString))
                    return View(await _context.JobApplications.Include(x => x.JobOffer).Include(x => x.JobOffer.Company).Where(o=>o.UserId==user.Id).ToListAsync());

                searchResult = await _context.JobApplications.Include(x => x.JobOffer).Include(x => x.JobOffer.Company).Where(o => o.UserId == user.Id)
                    .Where(o => o.JobOffer.JobTitle.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .ToListAsync();

            }
            

            return View(searchResult);
        }


        [Authorize(Roles ="HR,User")]
        public async Task<IActionResult> Details(int id)
        {

            try
            {
                var app = await _context.JobApplications.Include(x => x.JobOffer)
                .FirstOrDefaultAsync(x => x.Id == id);
               
                ViewBag.app = app;
              
                return View(app);
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }

        }

        [Authorize(Roles = "HR")]
        [HttpPost]
        public async Task<ActionResult> Reject(int? id)
        {
            var to = await _context.JobApplications.Include(x=> x.User).FirstOrDefaultAsync(x => x.Id == id.Value);
            var from= await _context.JobOffers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == to.JobOfferId);
            await SendMessage(from.User, to.User, "Your application for " + from.JobTitle + " position has been rejected");
            if (to.CvUrl != "")
                await _azureBlobService.DeleteAsync(to.CvUrl);
            _context.JobApplications.Remove(to);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "HR")]
        [HttpPost]
        public async Task<ActionResult> Approve(int? id)
        {
            var to = await _context.JobApplications.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id.Value);
            var from = await _context.JobOffers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == to.JobOfferId);
            await SendMessage(from.User, to.User, "Your application for " + from.JobTitle + " position has been accepted");
            if (to.CvUrl != "")
                await _azureBlobService.DeleteAsync(to.CvUrl);
            _context.JobApplications.Remove(to);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [Authorize(Roles ="User")]
        [HttpPost]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest($"id should not be null");
            }
            var offer = await _context.JobApplications.Include(x => x.JobOffer).FirstOrDefaultAsync(x => x.Id == id.Value);
            if (offer.CvUrl != "")
            await _azureBlobService.DeleteAsync(offer.CvUrl);
            _context.JobApplications.Remove(offer);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest($"id shouldn't not be null");
            }
            var offer = await _context.JobApplications.Include(x=>x.JobOffer).FirstOrDefaultAsync(x => x.Id == id.Value);
            if (offer == null)
            {
                return NotFound($"offer not found in DB");
            }

            return View(offer);
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(JobApplication model)
        {
            if (!ModelState.IsValid)
            {
                var cos = await _context.JobApplications.Include(x => x.JobOffer).FirstOrDefaultAsync(x => x.Id == model.Id);
                return View(cos);
            }
            var offer = await _context.JobApplications.Include(x=>x.JobOffer).FirstOrDefaultAsync(x => x.Id == model.Id);         

            var request = await HttpContext.Request.ReadFormAsync();
            var files = request.Files;

            if (files != null && files.Count != 0)
            {
                string name = string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), Path.GetExtension(files[0].FileName));

                var uri = _azureBlobService.UploadAsync(files, name);
                if(offer.CvUrl!="")
                await _azureBlobService.DeleteAsync(offer.CvUrl);
                offer.CvUrl = uri.Result.AbsoluteUri;
            }



            offer.FirstName = model.FirstName;
            offer.LastName = model.LastName;
            offer.PhoneNumber = model.PhoneNumber;
            


            _context.Update(offer);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = model.Id });
        }

        public User FindUser(ClaimsPrincipal user)
        {
            string email = "";
            var identity = user.Identities.First();
            foreach (Claim c in identity.Claims)
            {
                if (c.Type == "emails")
                {
                    email = c.Value;
                }
            }
            var owner = _context.Users.FirstOrDefault(a => a.Email == email);
            return owner;
        }
        public async Task SendMessage(User from, User to, string text)
        {
            var apiKey = _configuration.GetSection("SENDGRID_API_KEY").Value;
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage();

            msg.SetFrom(new EmailAddress(from.Email, ".Net Project"));
            msg.AddTo(new EmailAddress(to.Email, to.Role));

            msg.SetSubject(".Net Project Notification");

            msg.AddContent(MimeType.Text, text);
            msg.AddContent(MimeType.Html, "<p>" + text + "</p>");
            var response = await client.SendEmailAsync(msg);
        }
    }
}