using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public class UtterancesContext : DbContext
    {
        public UtterancesContext(DbContextOptions<UtterancesContext> options) : base(options)
        {

        }

        public DbSet<UtterancesContext> Utterances{ get; set; }
    }
}
