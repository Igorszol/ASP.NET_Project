using ASP.NET_Project.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.NET_Project.EntityFramework
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<JobOffer> JobOffers { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<User> Users { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>().HasData(
                   new Company {Id=1, Name = "Microsoft" },
                   new Company{ Id = 2, Name = "Ilabo" }
            );
            modelBuilder.Entity<User>().HasData(
                              new  User{ Id = 3, Email="igorszol@interia.pl",Role="Admin"}
                       );
        }
    }
}
