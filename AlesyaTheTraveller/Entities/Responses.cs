using System;
using System.Collections.Generic;

namespace AlesyaTheTraveller.Entities
{
    public partial class Responses
    {
        public int Id { get; set; }
        public string Word { get; set; }
        public int Respid { get; set; }
        public bool Active { get; set; }

        public virtual Utterances Resp { get; set; }
    }
}
