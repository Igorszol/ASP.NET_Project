﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.NET_Project.Models
{
    public class JobOfferCreateView : JobOffer
    {
        public IEnumerable<Company> Companies { get; set; }
    }
}

