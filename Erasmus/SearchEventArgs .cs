using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Erasmus
{
    public class SearchEventArgs : EventArgs 
    {
        public string SearchText { get; set; }
        public string SearchCategory { get; set; } 
    }
}
