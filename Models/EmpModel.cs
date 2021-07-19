using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Mobile_Repair_History_System_MRHS.Models
{
    public class EmpModel
    {
        [Display(Name = "Enter Date")]
        public DateTime EnterDate { get; set; }
    }
}