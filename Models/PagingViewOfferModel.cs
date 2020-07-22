using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.NET_Project.Models
{
    public class PagingViewOfferModel
{
    public IEnumerable<JobOffer> JobOffers { get; set; }
    public int TotalPage { get; set; }
}
}
