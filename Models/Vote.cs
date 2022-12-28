using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace laba3_vote.Models
{
    public class Vote
    {
        public string Who { get; set; }
        public string ForWho { get; set; }

        public Vote(string who, string forWho)
        {
            Who = who;
            ForWho = forWho;
        }
    }
}
