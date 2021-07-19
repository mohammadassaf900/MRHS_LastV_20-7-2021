using Mobile_Repair_History_System_MRHS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;

namespace Mobile_Repair_History_System_MRHS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public MRHSEntities db = new MRHSEntities();

        #region Stores List in Dashboard Page
        public PartialViewResult Store_list()
        {
            List<Store> stores = db.Stores.ToList();
            List<User> users = db.Users.ToList();
            var multipletable = from e in stores
                                join d in users on e.Email equals d.Email into table1
                                from d in table1.ToList()
                                select new MultipleClass
                                {
                                    store = e,
                                    user = d
                                };
            return PartialView("_Store_list", multipletable);
        }
        #endregion

        #region Invoices List in Dashboard Page
        public PartialViewResult Invoice_list()
        {
            var ListInvoice = db.Invoices.ToList();
            return PartialView("_Invoice_list", ListInvoice);
        }
        #endregion

        #region Users List in Dashboard Page
        public PartialViewResult User_list()
        {
            var ListUser = db.Users.ToList();
            return PartialView("_User_list", ListUser);
        }
        #endregion

        #region Blocked Accounts Ist in Dashboard Page
        public PartialViewResult Block_list()
        {
            List<BlockAcconut> blockAcconuts = db.BlockAcconuts.Where(x => x.Status == "Blocked").ToList();
            List<BlockAcconut> block = new List<BlockAcconut>().ToList();
            BlockAcconut maxid = new BlockAcconut();
            int count = 0;
            foreach (var item in blockAcconuts)
            {
                maxid = item;
                foreach (var temp in blockAcconuts)
                {
                    if (item.UserEmail == temp.UserEmail)
                    {
                        if (item.ID < temp.ID)
                        {
                            maxid = temp;
                        }
                    }
                }
                var find = block.Find(x => x.UserEmail == item.UserEmail);
                if (find == null)
                {
                    block.Add(maxid);
                    count++;
                }
            }
            List<User> users = db.Users.Where(x => x.Status == ("Blocked")).ToList();
            var multipletable = from b in block
                                join u in users on b.UserEmail equals u.Email into table1
                                from u in table1.ToList()
                                select new MultipleClass
                                {
                                    user = u,
                                    blockAcconut = b
                                };
            ViewBag.BlockedCount = count.ToString();
            return PartialView("_Block_list", multipletable);
        }
        #endregion

        #region Dashboard Page
        public ActionResult Dashboard()
        {
            List<User>listuser = db.Users.Where(x => x.RoleID == 3 && x.Status == "Active").ToList();
            List<User> liststore = db.Users.Where(x => x.RoleID == 2 && x.Status == "Active").ToList();
            List<User> listblock = db.Users.Where(x=>x.Status=="Blocked").ToList();
            var Customercount = db.Users.Where(x => x.RoleID == 3 && x.Status == "Active").Count();
            var Guestcount = db.Users.Where(x => x.RoleID == 4 && x.Status == "Active").Count();
            ViewBag.userReg = Customercount + Guestcount;
            ViewBag.StoreCount = db.Users.Where(x => x.RoleID == 2&&x.Status=="Active").Count();
            ViewBag.invoice = db.Invoices.Count();
            ViewBag.BlockedCount = db.Users.Where(x => x.Status.Equals("Blocked")).Count();
            return View();
        }
        #endregion Dashboard Page

        #region Recommendation List Page
        public ActionResult Recommendation_list()
        {
            List<Store> stores = db.Stores.ToList();
            List<User> users = db.Users.ToList();
            List<Rate> rates = db.Rates.ToList();
            List<Rate> Rt = new List<Rate>();
            int cursor = 1;
            foreach (var item in stores)
            {
                Rate x = new Rate();
                float sum = 0; int count = 0;
                foreach (var r in rates)
                {
                    if (r.RecieverEmail == item.Email)
                    {
                        sum = (float)(sum + r.Value);
                        count++;
                    }
                }
                if (count >1)
                {
                    count -= 1; 
                }
                x.RateID = cursor;
                x.SenderEmail = "Admin@gmail.com";
                x.RecieverEmail = item.Email;
                x.Value = (sum / count);
                x.DateTime = DateTime.Now;
                Rt.Add(x);
                cursor++;
            }
            var multipletable = from e in stores
                                    join d in users on e.Email equals d.Email into table1
                                    from d in table1.ToList()
                                    join i in Rt on e.Email equals i.RecieverEmail into table2
                                    from i in table2.ToList()
                                    select new MultipleClass
                                    {
                                        store = e,
                                        user = d,
                                        rate = i
                                    };
            multipletable = multipletable.OrderByDescending(x => x.rate.Value);
            return View(multipletable);
        }
        #endregion

        #region Add Store Page
        public ActionResult Add_Store()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Add_Store(User u)
        {
            if (ModelState.IsValid)
            {
                u.ActivationCode = new Guid("7668e1ba-b231-47a9-b90a-674a60e41682"); //Default
                u.RoleID = 2;
                u.Status = "Active";
                u.IsEmailVerified = true;
                var pass = Crypto.Hash(u.Password);
                u.Password = pass;
                var c = db.Users.Find(u.Email);
                if (c == null)
                {
                    db.Users.Add(u);
                    Store s = new Store
                    {
                        Email = u.Email,
                        Points = 0,
                        Address = "Amman",
                        GovernorateID = 1
                    };
                    db.Stores.Add(s);
                    Rate r = new Rate
                    {
                        SenderEmail = "Admin@gmail.com", // Admin Email
                        RecieverEmail = s.Email,
                        Value = 0,
                        DateTime = DateTime.Now
                    };
                    try
                    {
                        db.Rates.Add(r);
                        db.SaveChanges();
                        return RedirectToAction("Dashboard");
                    }
                    catch
                    {
                        ModelState.AddModelError("UserName", "User Name is already exist");
                        return View();
                    }
                }
                else
                {
                    ModelState.AddModelError("Email", "Email is already exist");
                    return View();
                }
            }
            else
            {                
                return View();
            }   
        }
        #endregion

        #region Delete Invoices
        public ActionResult Delete_Invoices(int?id)
        {
            try
            {
                if (id != null)
                {
                    Invoice invoice = db.Invoices.Single(i => i.InvoiceID == id);
                    db.Invoices.Remove(invoice);
                    db.SaveChanges();
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    return RedirectToAction("Dashboard");
                }
            }
            catch
            {
                return RedirectToAction("Dashboard");
            }
        }
        #endregion

        #region Search Device ID Page
        public ActionResult Search()
        {
            if (ModelState.IsValid)
            {
                return View();
            }
            else
            {
                ModelState.AddModelError("DeviceID", "Device ID is Required");
                return View();
            }    
        }
        [HttpPost]
        public PartialViewResult Search_Result(string DeviceId)
        {
            if (ModelState.IsValid)
            {
                var device_info = db.Invoices.Where(x => x.DeviceID.Equals(DeviceId));
                return PartialView("_Search_Result", device_info.ToList());
            }
            else
            {
                ModelState.AddModelError("DeviceID", "Device ID is Required");
                return PartialView("_Search_Result");
            }
        }
        public PartialViewResult Search_Result()
        {
            return PartialView("_Search_Result");
        }
        #endregion

        #region Store Information Page
        public ActionResult Store_info(string email)
        {
            User user_info = db.Users.Find(email);
            Store store_info = db.Stores.Find(email);
            var rate_info = db.Rates.Where(x => x.RecieverEmail == email);
            var count = 0;
            float sum = 0;
            foreach(var item in rate_info)
            {
                count++;
                sum=(float)(sum+item.Value);
            }
            if (count > 1)
            {
                count -= 1;
            }
            ViewBag.InvoiceCount = db.Invoices.Where(x => x.StoreEmail == email).Count();
            ViewBag.Points = store_info.Points;
            ViewBag.Rates = Math.Round(((sum*1.0)/count),1);
            return View(user_info);
        }
        #endregion

        #region Block Account
        public PartialViewResult Blocked()
        {
            return PartialView("_Blocked");
        }
        [HttpPost]
        public PartialViewResult Blocked(string email, string Reason)
        {
            if (ModelState.IsValid)
            {
                User User_block = db.Users.Find(email);
                if (User_block.Status.Equals("Active"))
                {
                    User_block.Status = "Blocked";
                    var count = db.BlockAcconuts.Count();
                    BlockAcconut blockAcconut = new BlockAcconut
                    {
                        UserEmail = email,
                        Reason = Reason,
                        Status = "Blocked",
                        DateTime = DateTime.Now
                    };
                    if (count == 0)
                    {
                        blockAcconut.ID = 1;
                    }
                    else
                    {
                        blockAcconut.ID = db.BlockAcconuts.OrderByDescending(x => x.ID).FirstOrDefault().ID + 1;
                    }
                    db.BlockAcconuts.Add(blockAcconut);
                    db.SaveChanges();
                }
                return PartialView("_Blocked");
            }
            else
            {
                return PartialView("_Blocked");
            }  
        }
        #endregion

        #region Active Account
        public ActionResult Active(string email)
        {
            User User_block = db.Users.Find(email);
            User_block.Status = "Active";
            BlockAcconut blockAcconut = new BlockAcconut
            {
                UserEmail = email,
                Reason = "none",
                Status = "Active",
                DateTime = DateTime.Now
            };
            db.BlockAcconuts.Add(blockAcconut);
            db.SaveChanges();
            return RedirectToAction("Dashboard");
        }
        #endregion

        #region User(Customer&Guest) Information Page
        public ActionResult User_info(string email)
        {
            User user_info = db.Users.Find(email);
            return View(user_info);
        }
        #endregion

        #region Profile Information Page
        public ActionResult Profile_info()
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            var user = Session["username"].ToString();
            User user_info = db.Users.Find(user);
            return View(user_info);  
        }
        #endregion

        #region Edit Profile Page
        public ActionResult Edit_Profile()
        {
            if (Session["username"] == null)
            {
                return RedirectToAction("Login", "User");
            }
            ViewBag.error = "";
            var user = Session["username"].ToString();
            User user_info = db.Users.Find(user);
            return View(user_info);
        }
        [HttpPost]
        public ActionResult Edit_Profile(string Username,string Phone)
        {
            var user = Session["username"].ToString();
            User user_info = db.Users.Find(user);
            try
            {
                user_info.UserName = Username;
                user_info.Phone = Phone;
                db.SaveChanges();
                return RedirectToAction("Profile_info");
            }
            catch
            {   
                if (Username.Length==0)
                {
                    ViewBag.error = "User Name is Required";
                }
                User test = db.Users.Where(x => x.UserName == Username).FirstOrDefault();
                if (test != null)
                {
                     ViewBag.error = "User name is already exist";
                }
                if (Phone.Length==0)
                {
                    ViewBag.error = "Phone Number is Required";
                }
                if(Phone.Length!= 10 && Phone.Length!=0)
                {
                    ViewBag.error = "Phone Number Must Be 10 Digits";
                }
                return View(user_info);
            }
        }
        #endregion

        #region Preview Comments Page
        public ActionResult Preview_comments()
        {
            var comment = db.Comments.OrderByDescending(x => x.CommentID).ToList();
            return View(comment);
        }
        #endregion

        #region Delete Comment
        public ActionResult Delete_Comment(int? id)
        {
            var c = db.Comments.Find(id);
            var reply = db.Comments.Where(x => x.ParentID == id);
            foreach (var item in reply)
            {
                db.Comments.Remove(item);
            }
            db.Comments.Remove(c);
            db.SaveChanges();
            return RedirectToAction("Preview_comments");
        }
        #endregion

        #region Details Invoices Page
        public ActionResult Details_Invoices(int?id)
        {
            if (id != null)
            {    
                Invoice invoice = db.Invoices.Single(i => i.InvoiceID == id);
                User u = db.Users.Where(x => x.Phone == invoice.CustomerPhoneNumber).FirstOrDefault();
                if(u!= null)
                {
                    ViewBag.i_nuser = u.UserName;
                    ViewBag.i_euser = "Email: "+ u.Email;
                }
                User s = db.Users.Where(x => x.Email == invoice.StoreEmail).FirstOrDefault();
                Store store = db.Stores.Single(x => x.Email == s.Email);
                ViewBag.i_nstore = s.UserName;
                ViewBag.i_astore = store.Address;
                ViewBag.i_pstore = s.Phone;
                return View(invoice);
            }
            else
            {
                return RedirectToAction("Dashboard");
            }
        }
        #endregion

        #region Add Governorate
        //public ActionResult Add_Governorate()
        //{
        //    return View();
        //}
        //[HttpPost]
        //public ActionResult Add_Governorate(Governorate G)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Governorates.Add(G);
        //        db.SaveChanges();
        //        return RedirectToAction("Dashboard");
        //    }
        //    else
        //    {
        //        return View();
        //    }

        //}
        #endregion
    }
}