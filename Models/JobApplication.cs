using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.NET_Project.Models
{
    public class JobApplication
    {
        public int Id { get; set; }
        [Required]
        public virtual int JobOfferId { get; set; }
        public virtual JobOffer JobOffer { get; set; }
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        
        [Display(Name = "CV File")]
        public string CvUrl { get; set; }

        public virtual User User { get; set; }
        public virtual int UserId { get; set; }
    }
}
