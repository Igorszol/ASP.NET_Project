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
    public class JobOfferController : Controller
    {
        private readonly DataContext _context;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IConfiguration _configuration;

        public JobOfferController(DataContext context, IAzureBlobService azureBlobService, IConfiguration configuration)
        {
           
            _configuration = configuration;
            _context = context;
            _azureBlobService = azureBlobService;
        }


        [Authorize(Roles ="HR")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public PagingViewOfferModel List(string searchString = "", int pageNo = 1, int pageSize = 8)
        {
            int totalPage, totalRecord;
            IEnumerable<JobOffer> record;
            if (string.IsNullOrEmpty(searchString))
            {
                totalRecord = _context.JobOffers.Count();
                totalPage = (totalRecord / pageSize) + ((totalRecord % pageSize) > 0 ? 1 : 0);
                record = (from u in _context.JobOffers.Include(x => x.Company)
                          orderby u.JobTitle
                          select u).Skip((pageNo - 1) * pageSize).Take(pageSize).ToList();
            }
            else
            {
                record = (from u in _context.JobOffers.Include(x => x.Company)
                          where (u.JobTitle.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                          orderby u.JobTitle
                          select u).Skip((pageNo - 1) * pageSize).Take(pageSize).ToList();
                totalRecord = record.Count();
                totalPage = (totalRecord / pageSize) + ((totalRecord % pageSize) > 0 ? 1 : 0);
            }
            PagingViewOfferModel empData = new PagingViewOfferModel
            {
                JobOffers = record,
                TotalPage = totalPage
            };

            return empData;
        }

        [Authorize(Roles = "HR")]
        public PagingViewOfferModel MyList(string searchString = "", int pageNo = 1, int pageSize = 8)
        {
            int totalPage, totalRecord;
            IEnumerable<JobOffer> record;
            var user = FindUser(User);
            if (string.IsNullOrEmpty(searchString))
            {
                totalRecord = _context.JobOffers.Count();
                totalPage = (totalRecord / pageSize) + ((totalRecord % pageSize) > 0 ? 1 : 0);
                record = (from u in _context.JobOffers.Include(x => x.Company)  
                          orderby u.JobTitle
                          where (u.UserId == user.Id)
                          select u).Skip((pageNo - 1) * pageSize).Take(pageSize).ToList();
            }
            else
            {
                record = (from u in _context.JobOffers.Include(x => x.Company) where(u.UserId == user.Id)
                          where (u.JobTitle.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                          orderby u.JobTitle
                          select u).Skip((pageNo - 1) * pageSize).Take(pageSize).ToList();
                totalRecord = record.Count();
                totalPage = (totalRecord / pageSize) + ((totalRecord % pageSize) > 0 ? 1 : 0);
            }
            PagingViewOfferModel empData = new PagingViewOfferModel
            {
                JobOffers = record,
                TotalPage = totalPage
            };

            return empData;
        }


        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest($"id shouldn't not be null");
            }
            var offer = await _context.JobOffers.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (offer == null)
            {
                return NotFound($"offer not found in DB");
            }

            return View(offer);
        }

        [Authorize(Roles = "HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(JobOffer model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var offer = await _context.JobOffers.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (offer.RowVersion != model.RowVersion) return NotFound($"offer has just been changed");
            offer.JobTitle = model.JobTitle;
            offer.SalaryFrom = model.SalaryFrom;
            offer.SalaryTo = model.SalaryTo;
            offer.Description = model.Description;
            offer.RowVersion++;
            _context.Update(offer);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = model.Id });
        }

        [Authorize(Roles = "HR,Admin")]
        [HttpPost]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest($"id should not be null");
            }
            var offer = await _context.JobOffers.Include(x=>x.JobApplications).FirstOrDefaultAsync(x => x.Id ==id);
            foreach(var app in offer.JobApplications)
            {
                if (app.CvUrl != "")
                    await _azureBlobService.DeleteAsync(app.CvUrl);
            //    _context.JobApplications.Remove(app);
            }
            _context.JobOffers.Remove(offer);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "HR")]
        public async Task<ActionResult> Create()
        {
            var model = new JobOfferCreateView
            {
                Companies = await _context.Companies.ToListAsync()
            };

            return View(model);
        }


        [Authorize(Roles = "HR")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JobOffer offer)
        {
            if (!ModelState.IsValid)
            {
                var model = new JobOfferCreateView
                {
                    Companies = await _context.Companies.ToListAsync()
                };
                        return View(model);
               
            }

            offer.Created = DateTime.Now;
            offer.RowVersion = 1;
            var user = FindUser(User);
            offer.UserId = user.Id;
            _context.JobOffers.Add(offer);

            await _context.SaveChangesAsync();

            return CreatedAtAction("Details", new { id = offer.Id }, offer);

        }

        [Authorize(Roles = "User")]
        public ActionResult Apply(int id)
        {


            var model = new JobApplication
            {
                JobOfferId = id
            };
            return View(model);
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Apply(JobApplication model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = await HttpContext.Request.ReadFormAsync();
            var files = request.Files;
            string url = "";
            if (files != null && files.Count != 0)
            {
                string name = string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), Path.GetExtension(files[0].FileName));

                var uri = _azureBlobService.UploadAsync(files, name);
                url = uri.Result.AbsoluteUri;

            }
            var user = FindUser(User);

            JobApplication ja = new JobApplication
            {
                JobOfferId = model.JobOfferId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,              
                CvUrl = url,
                UserId = user.Id
            };
            var offer = await _context.JobOffers.Include(x=>x.User).FirstOrDefaultAsync(x => x.Id == ja.JobOfferId);


            await SendMessage(user, offer.User, "Somebody appiled to your job offer!");
            await _context.JobApplications.AddAsync(ja);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = model.JobOfferId });

        }


        public async Task<IActionResult> Details(int id)
        {
            var offer = await _context.JobOffers
                .Include(x => x.Company).Include(x => x.JobApplications)
                .FirstOrDefaultAsync(x => x.Id == id);
            return View(offer);
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
         public async Task SendMessage(User from, User to,string text)
        {
            var apiKey = _configuration.GetSection("SENDGRID_API_KEY").Value;
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage();

            msg.SetFrom(new EmailAddress(from.Email, ".Net Project"));
            msg.AddTo(new EmailAddress(to.Email, to.Role));
           
            msg.SetSubject(".Net Project Notification");

            msg.AddContent(MimeType.Text, text);
            msg.AddContent(MimeType.Html, "<p>"+text+"</p>");
            var response = await client.SendEmailAsync(msg);
        }
    }
}