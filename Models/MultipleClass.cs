using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Mobile_Repair_History_System_MRHS.Models
{
    public class MultipleClass
    {
        public BlockAcconut blockAcconut { set; get; }
        public User user { set; get; }
        public Store store { set; get; }
        public Rate rate { set; get; }
        public Comment comment { set; get; }
    }
}