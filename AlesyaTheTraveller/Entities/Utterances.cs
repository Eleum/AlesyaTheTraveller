using System;
using System.Collections.Generic;

namespace AlesyaTheTraveller.Entities
{
    public partial class Utterances
    {
        public Utterances()
        {
            Responses = new HashSet<Responses>();
        }

        public int Id { get; set; }
        public int Respid { get; set; }
        public string Content { get; set; }

        public virtual ICollection<Responses> Responses { get; set; }
    }
}
