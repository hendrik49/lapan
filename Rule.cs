using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LAPAN
{
    public class Rule
    {
        [Key]
        public int id { get; set; }
        public string kelas { get; set; }
        public string rule { get; set; }

    }
}
