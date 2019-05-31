using System;
using System.Collections.Generic;

namespace AlesyaTheTraveller.Entities
{
    public partial class Sorting
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int Typeid { get; set; }
        public string Typename { get; set; }
        public bool Active { get; set; }
    }
}
