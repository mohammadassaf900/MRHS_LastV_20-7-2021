using Mobile_Repair_History_System_MRHS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace Mobile_Repair_History_System_MRHS.Controllers
{
    public class HomeController : Controller
    {
        public MRHSEntities db = new MRHSEntities();

        #region Home Page
        public ActionResult Index()
        {   List<Store> stores = db.Stores.ToList();
            List<User> users = db.Users.ToList();
            List<Rate> rates = db.Rates.ToList();
            List<Invoice> invoices = db.Invoices.ToList();
            ViewBag.countUser = users.Where(x => x.RoleID == 3 || x.RoleID == 4).Count();
            ViewBag.countStore = users.Where(x => x.RoleID == 2).Count();
            ViewBag.countInvoice = invoices.Count();
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
                x.RateID = cursor;
                x.SenderEmail = "admin@gmail.com";
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

        #region Search in Home Page
        public ActionResult Search()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Search(string DeviceId)
        {
            var device_info = db.Invoices.Where(x => x.DeviceID.Equals(DeviceId));
            return View(device_info.ToList());
        }
        #endregion

        #region Details Invoice Page
        [Authorize]
        public ActionResult Details(int?id)
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            var user = Session["username"].ToString();
            User user_info = db.Users.Find(user);
            if (id != null)
            {
                Invoice invoice = db.Invoices.Single(i => i.InvoiceID == id);
                if (User.IsInRole("Admin"))
                {
                    User u = db.Users.Single(x => x.Phone == invoice.CustomerPhoneNumber);
                    User s = db.Users.Single(x => x.Email == invoice.StoreEmail);
                    Store store = db.Stores.Single(x => x.Email == s.Email);
                    ViewBag.i_nstore = s.UserName;
                    ViewBag.i_astore = store.Address;
                    ViewBag.i_pstore = s.Phone;
                    ViewBag.i_nuser = u.UserName;
                    ViewBag.i_euser = u.Email;
                    ViewBag.Role = "Admin";
                }
                else if (invoice.StoreEmail == user)
                {
                    User u = db.Users.Single(x => x.Phone == invoice.CustomerPhoneNumber);
                    User s = db.Users.Single(x => x.Email == invoice.StoreEmail);
                    Store store = db.Stores.Single(x => x.Email == s.Email);
                    ViewBag.i_nstore = s.UserName;
                    ViewBag.i_astore = store.Address;
                    ViewBag.i_pstore = s.Phone;
                    ViewBag.i_nuser = u.UserName;
                    ViewBag.i_euser = u.Email;
                    ViewBag.Role = "Store";
                }
                else if (user_info.Phone == invoice.CustomerPhoneNumber)
                {
                    User u = db.Users.Single(x => x.Phone == invoice.CustomerPhoneNumber);
                    User s = db.Users.Single(x => x.Email == invoice.StoreEmail);
                    Store store = db.Stores.Single(x => x.Email == s.Email);
                    ViewBag.i_nstore = s.UserName;
                    ViewBag.i_astore = store.Address;
                    ViewBag.i_pstore = s.Phone;
                    ViewBag.i_nuser = u.UserName;
                    ViewBag.i_euser = u.Email;
                    ViewBag.Role = "Customer";
                }
                else
                {
                    User s = db.Users.Single(x => x.Email == invoice.StoreEmail);
                    Store store = db.Stores.Single(x => x.Email == s.Email);
                    ViewBag.i_nstore = s.UserName;
                    ViewBag.i_astore = store.Address;
                    ViewBag.i_pstore = s.Phone;
                    ViewBag.Role = "Guest";
                }
                return View(invoice);
            }
            else
            {
                return RedirectToAction("Preview_Invoices");
            }       
        }
        #endregion

        #region Store Information Page
        public ActionResult Store_info(string email)
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            if (email == null)
            {
                email = Session["Store"].ToString();
            }
            User user_info = db.Users.Find(email);
            Store store_info = db.Stores.Find(email);
            var rate_info = db.Rates.Where(x => x.RecieverEmail == email);
            var count = 0;
            float sum = 0;
            foreach (var item in rate_info)
            {
                count++;
                sum = (float)(sum+item.Value);
            }
            if (count > 1)
            {
                count -= 1;
            }
            ViewBag.InvoiceCount = db.Invoices.Where(x => x.StoreEmail == email).Count();
            ViewBag.Points = store_info.Points;
            ViewBag.Rates = Math.Round(((sum * 1.0) / count),1);
            ViewBag.Address = store_info.Address;
            var u = Session["username"].ToString();
            User users = db.Users.Find(u);
            List<Invoice> invoices = db.Invoices.Where(x => x.CustomerPhoneNumber == users.Phone&&x.StoreEmail==email).ToList();
            if (invoices.Count != 0)
            {
                ViewBag.Check = "Customer";
            }
            else
            {
                ViewBag.Check = "Guest";
            }
            var rated = db.Rates.Where(x => x.SenderEmail == u && x.RecieverEmail == email).FirstOrDefault();
            if (rated == null)
            {
                ViewBag.ratedOrNot = "No";
            }
            else
            {
                ViewBag.RateFrom5 = rated.Value;
                ViewBag.ratedOrNot = "Yes";
            }
            return View(user_info);
        }
        #endregion

        #region Edit Profile Page (Customer&Guest)
        public ActionResult Edit_Profile()
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            var user = Session["username"].ToString();
            User user_info = db.Users.Find(user);
            return View(user_info);
        }
        [HttpPost]
        public ActionResult Edit_Profile(User u)
        {
            try
            {
                if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
                var user = Session["username"].ToString();
                User user_info = db.Users.Find(user);
                user_info.UserName = u.UserName;
                user_info.Phone = u.Phone;
                db.SaveChanges();
                return RedirectToAction("Profile_info");
            }
             catch
            {
                if(u.Phone == "" || u.Phone==null)
                {
                    ModelState.AddModelError("Phone", "Phone Must Be 10 Digits");  
                }
                User test = db.Users.Where(x => x.UserName == u.UserName).FirstOrDefault();
                if (test != null)
                {
                    ModelState.AddModelError("UserName", "User name is already exist");
                }
                if (u.UserName == null || u.UserName == "")
                {
                    ModelState.AddModelError("UserName", "User Name is Required");
                }
                return View(u);
            }
        }
        #endregion

        #region Profile Information (Customer&Guest)
        public ActionResult Profile_info()
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            var user = Session["username"].ToString();
            User user_info = db.Users.Find(user);
            return View(user_info);
        }
        #endregion

        #region Preview Comments in Store Information Page
        public PartialViewResult Preview_Comments(string reciever)
        {
            //  ViewBag.IsStore = 
            string email = Session["username"].ToString();
            if (reciever == email)
            {
                List<Store> s = db.Stores.Where(x => x.Email == email).ToList();
                if (s.Count != 0)
                {
                    ViewBag.Check1 = "Store";
                }
            }
            List<Comment> comments = db.Comments.Where(x => x.RecieverEmail == reciever)
                .OrderByDescending(x => x.CommentID).ToList();

            return PartialView("_Preview_Comments", comments);
        }
        #endregion

        #region Make Rate To Store From Customer Only in Store Information Page
        public ActionResult MakeRate(double? ValueOfRate, string email)
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            Session["Store"] = email;
            try
            {
                if (ValueOfRate != null)
                {
                    Rate r = new Rate();
                    var last = db.Rates.OrderByDescending(x => x.RateID).FirstOrDefault().RateID + 1;
                    var rT = db.Rates.Where(x => x.RateID == last).FirstOrDefault();
                    r.RateID = last;
                    r.Value = (double)ValueOfRate;
                    r.SenderEmail = Session["username"].ToString();
                    r.RecieverEmail = email;
                    r.DateTime = DateTime.Now;
                    db.Rates.Add(r);
                    db.SaveChanges();
                }
            }
            catch
            {
                ModelState.AddModelError("","");
            }
            User user_info = db.Users.Find(email);
            Store store_info = db.Stores.Find(email);
            var rate_info = db.Rates.Where(x => x.RecieverEmail == email);
            var count = 0;
            float sum = 0;
            foreach (var item in rate_info)
            {
                count++;
                sum = (float)(sum+item.Value);
            }
            if (count > 1)
            {
                count -= 1;
            }
            ViewBag.InvoiceCount = db.Invoices.Where(x => x.StoreEmail == email).Count();
            ViewBag.Points = store_info.Points;
            ViewBag.Rates = Math.Round(((sum * 1.0) / count),1); // تقريب لاول خانة(القيمة,عدد الخانات
            var u = Session["username"].ToString();
            User users = db.Users.Find(u);
            List<Invoice> invoices = db.Invoices.Where(x => x.CustomerPhoneNumber == users.Phone).ToList();
            if (invoices.Count != 0)
            {
                ViewBag.Check = "Customer";
            }
            else
            {
                ViewBag.Check = "Guest";
            }
            var rated = db.Rates.Where(x => x.SenderEmail == u && x.RecieverEmail == email).FirstOrDefault();
            if (rated == null)
            {
                ViewBag.ratedOrNot = "No";
            }
            else
            {
                ViewBag.RateFrom5 = rated.Value;
                ViewBag.ratedOrNot = "Yes";
            }
            return RedirectToAction("Store_info","Home",email);
        }
        #endregion

        #region Edit Rate From Customer Only And Make Rate Before in Store Information Page
        public ActionResult EditRate(double? ValueOfRate, string email)
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            Session["Store"] = email;
            string sender = Session["username"].ToString();
            try
            {
                if (ValueOfRate != null)
                {
                    var r = db.Rates.Where(x => x.RecieverEmail == email && x.SenderEmail == sender).FirstOrDefault();
                    var last = db.Rates.OrderByDescending(x => x.RateID).FirstOrDefault().RateID + 1;
                    var convert = (double)ValueOfRate; 
                    r.Value = convert;
                    db.SaveChanges();
                }
            }
            catch
            {
                ModelState.AddModelError("","");
            }
            User user_info = db.Users.Find(email);
            Store store_info = db.Stores.Find(email);
            var rate_info = db.Rates.Where(x => x.RecieverEmail == email);
            var count = 0;
            float sum = 0;
            foreach (var item in rate_info)
            {
                count++;
                sum =(float)(sum+ item.Value);
            }
            if (count > 1)
            {
                count -= 1;
            }
            ViewBag.InvoiceCount = db.Invoices.Where(x => x.StoreEmail == email).Count();
            ViewBag.Points = store_info.Points;
            ViewBag.Rates = (sum * 1.0) / count;
            var u = Session["username"].ToString();
            User users = db.Users.Find(u);
            List<Invoice> invoices = db.Invoices.Where(x => x.CustomerPhoneNumber == users.Phone).ToList();//----------------------------------REVIEW------------------------------------
            if (invoices.Count !=0)
            {
                ViewBag.Check = "Customer";
            }
            else
            {
                ViewBag.Check = "Guest";
            }
            var rated = db.Rates.Where(x => x.SenderEmail == u && x.RecieverEmail == email).FirstOrDefault();
            if (rated == null)
            {
                ViewBag.ratedOrNot = "No";
            }
            else
            {
                ViewBag.RateFrom5 = rated.Value;
                ViewBag.ratedOrNot = "Yes";
            }
            return RedirectToAction("Store_info", "Home", email);
        }
        #endregion

        #region Add Comment From Customer Only in Store Information Page
        [HttpPost]
        public ActionResult Add_Comment(string message, string email)
        {
            try
            {
                if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
                Session["Store"] = email;
                Comment comment = new Comment();
                var last = db.Comments.OrderByDescending(x => x.CommentID).FirstOrDefault().CommentID + 1;
                comment.CommentID = last;
                comment.Content = message;
                comment.DateTime = DateTime.Now;
                comment.SenderEmail = Session["username"].ToString();
                comment.RecieverEmail = email;
                comment.ParentID = 0;
                db.Comments.Add(comment);
                db.SaveChanges();
            }
            catch
            {
                ModelState.AddModelError("Message", "Enter A Comment");
            }
            User user_info = db.Users.Find(email);
            Store store_info = db.Stores.Find(email);
            var rate_info = db.Rates.Where(x => x.RecieverEmail == email);
            var count = 0;
            float sum = 0;
            foreach (var item in rate_info)
            {
                count++;
                sum =(float)(sum+ item.Value);
            }
            if (count > 1)
            {
                count -= 1;
            }
            ViewBag.InvoiceCount = db.Invoices.Where(x => x.StoreEmail == email).Count();
            ViewBag.Points = store_info.Points;
            ViewBag.Rates = (sum * 1.0) / count;
            var u = Session["username"].ToString();
            User users = db.Users.Find(u);
            List<Invoice> invoices = db.Invoices.Where(x => x.CustomerPhoneNumber == users.Phone).ToList();
            if (invoices.Count!=0)
            {
                ViewBag.Check = "Customer";
            }
            else
            {
                ViewBag.Check = "Guest";
            }
            var rated = db.Rates.Where(x => x.SenderEmail == u && x.RecieverEmail == email).FirstOrDefault();
            if (rated == null)
            {
                ViewBag.ratedOrNot = "No";
            }
            else
            {
                ViewBag.RateFrom5 = rated.Value;
                ViewBag.ratedOrNot = "Yes";
            }
            return RedirectToAction("Store_info","Home",email);
        }
        #endregion

        #region Reply From Store To Comment Customer 
        public ActionResult Reply(string reply,int P_ID)
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            Session["Store"] = Session["username"];
            try
            {
                Comment comm = new Comment
                {
                    Content = reply,
                    ParentID = P_ID,
                    SenderEmail = Session["username"].ToString(),
                    RecieverEmail = Session["username"].ToString(),
                    DateTime = DateTime.Now
                };
                int id = db.Comments.OrderByDescending(x => x.CommentID).FirstOrDefault().CommentID+1;
             comm.CommentID = id;
             db.Comments.Add(comm);
             db.SaveChanges();
            }
            catch
            {
                ViewBag.error = "Reply is Empty";
            }
            var email = Session["username"].ToString();
            User user_info = db.Users.Find(email);
            Store store_info = db.Stores.Find(email);
            var rate_info = db.Rates.Where(x => x.RecieverEmail == email);
            var count = 0;
            float sum = 0;
            foreach (var item in rate_info)
            {
                count++;
                sum =(float)(sum+ item.Value);
            }
            if (count > 1)
            {
                count -= 1;
            }
            ViewBag.InvoiceCount = db.Invoices.Where(x => x.StoreEmail == email).Count();
            ViewBag.Points = store_info.Points;
            ViewBag.Rates = (sum * 1.0) / count;
            var u = Session["username"].ToString();
            User users = db.Users.Find(u);
            List<Invoice> invoices = db.Invoices.Where(x => x.CustomerPhoneNumber == users.Phone).ToList();//----------------------------------REVIEW------------------------------------
            if (invoices.Count != 0)
            {
                ViewBag.Check = "Customer";
            }
            else
            {
                ViewBag.Check = "Guest";
            }
            var rated = db.Rates.Where(x => x.SenderEmail == u && x.RecieverEmail == email).FirstOrDefault();
            if (rated == null)
            {
                ViewBag.ratedOrNot = "No";
            }
            else
            {
                ViewBag.RateFrom5 = rated.Value;
                ViewBag.ratedOrNot = "Yes";
            }
            return RedirectToAction("Store_info", "Home", email);
        }
        #endregion

        #region Contact Page
        public ActionResult Contact()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Contact(string name, string email, string subject, string text)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add("assafm281@gmail.com");
                mail.From = new MailAddress(email);
                mail.Subject = subject;
                string userMessage = "";
                userMessage = "<br/>Name :" + name;
                userMessage = userMessage + "<br/>Email Id: " + email;
                userMessage = userMessage + "<br/>Message: " + text;
                string Body = "Hi, <br/><br/> A new enquiry by user. Detail is as follows:<br/><br/> " + userMessage + "<br/><br/>Thanks";
                mail.Body = Body;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("assafm281@gmail.com", "123456Mo123456"),
                    EnableSsl = true
                };
                smtp.Send(mail);
                ViewBag.Message = "Thank you for contacting us.";
            }
            catch
            {
                ViewBag.Message = "Error............";
            }
            return View();
        }
        #endregion
    }
}