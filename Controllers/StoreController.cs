using Mobile_Repair_History_System_MRHS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Mobile_Repair_History_System_MRHS.Controllers
{
    [Authorize(Roles ="Store")]
    public class StoreController : Controller
    {
        public MRHSEntities db = new MRHSEntities();

        #region Home Page
        public ActionResult Index()
        {
            List<Store> stores = db.Stores.ToList();
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
                        sum =(float)(sum + r.Value);
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

        #region Search Page
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
        public ActionResult Details(int?id)
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            var user = Session["username"].ToString();
            if (id != null)
            {
                Invoice invoice = db.Invoices.Single(i => i.InvoiceID == id);
                User u = db.Users.Where(x => x.Phone == invoice.CustomerPhoneNumber).FirstOrDefault();
                if (u != null)
                {
                    User s = db.Users.Where(x => x.Email == invoice.StoreEmail).FirstOrDefault();
                    Store store = db.Stores.Where(x => x.Email == s.Email).FirstOrDefault();
                    if (invoice.StoreEmail==user) /// Store1@gmail.com
                    {
                        ViewBag.i_nuser = u.UserName;
                        ViewBag.i_euser = "Email: " + u.Email;
                        ViewBag.Role = "Store";
                    }
                    ViewBag.i_nstore = s.UserName;
                    ViewBag.i_astore = store.Address;
                    ViewBag.i_pstore = s.Phone;
                    return View(invoice);
                }
                else
                {
                    User s = db.Users.Where(x => x.Email == invoice.StoreEmail).FirstOrDefault();
                    Store store = db.Stores.Where(x => x.Email == s.Email).FirstOrDefault();
                    if (invoice.StoreEmail == user)
                    {
                        ViewBag.Role = "Store";
                    }
                    ViewBag.i_nstore = s.UserName;
                    ViewBag.i_astore = store.Address;
                    ViewBag.i_pstore = s.Phone;
                    return View(invoice);
                }
            }
            else
            {
                return RedirectToAction("Preview_Invoices");
            }
        }
        #endregion

        #region Create Invoices Page
        public ActionResult Create_invoices()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create_invoices(Invoice i)
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            try
            {
                if (ModelState.IsValid)
                {
                    if (i.ImageFile != null)
                    {
                        i.ImageFile.SaveAs(Server.MapPath("~/Contant/Invoice_img/" + i.ImageFile.FileName));
                        i.Image = "~/Contant/Invoice_img/" + i.ImageFile.FileName;
                    }
                    var Ur = db.Users.Where(x => x.Phone == i.CustomerPhoneNumber).FirstOrDefault();
                    if (Ur != null)
                    {
                        if (Ur.RoleID == 4)
                        {
                            Ur.RoleID = 3;
                        }
                    }
                    var user = Session["username"].ToString();
                    i.StoreEmail = user;
                    var s = db.Stores.Where(x => x.Email == user).FirstOrDefault();
                    s.Points += 10;
                    i.Date = DateTime.Now;
                    db.Invoices.Add(i);
                    db.SaveChanges();
                    return RedirectToAction("Preview_Invoices");
                }
                else
                {
                    return View(i);
                }
            }
            catch
            {
                ModelState.AddModelError("Image", "Image Can Not Be Empty");
                return View(i);
            }
        }
        #endregion

        #region Preview Invoices Page
        public ActionResult Preview_Invoices()
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            var user = Session["username"].ToString();
            var invoice_list = db.Invoices.Where(x => x.StoreEmail == user).ToList();
            return View(invoice_list);
        }
        #endregion

        #region Delete Invoices Page
        public ActionResult Delete_Invoies(int? id)
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            var user = Session["username"].ToString();
            if (id != null)
            {
                Invoice invoice = db.Invoices.Single(i => i.InvoiceID == id);
                var s = db.Stores.Where(x => x.Email == user).FirstOrDefault();
                s.Points -= 10;
                db.Invoices.Remove(invoice);
                db.SaveChanges();
                return RedirectToAction("Preview_Invoices");
            }
            else
            {
                return RedirectToAction("Preview_Invoices");
            }
        }
        #endregion

        #region Profile Information Page
        public ActionResult Profile_info()
        {
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            var user = Session["username"].ToString();
            User user_info = db.Users.Find(user);
            Store store_info = db.Stores.Find(user);
            var rate_info = db.Rates.Where(x => x.RecieverEmail == user);
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
            ViewBag.InvoiceCount = db.Invoices.Where(x => x.StoreEmail == user).Count();
            ViewBag.Points = store_info.Points;
            ViewBag.Rates = Math.Round(((sum * 1.0) / count), 1);
            ViewBag.Address = store_info.Address;
            return View(user_info);
        }
        #endregion

        #region Edit Profile Page
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
            if (Session["username"] == null) { return RedirectToAction("Login", "User"); }
            var user = Session["username"].ToString();
            User user_info = db.Users.Find(user);
            Store store_info = db.Stores.Find(user);
            try
            {         
                user_info.UserName = u.UserName;
                user_info.Phone = u.Phone;
                store_info.Address = u.Store.Address;
                db.SaveChanges();
                return RedirectToAction("Profile_info");
            }
            catch
            {
                if (u.UserName==null)
                    ModelState.AddModelError("Store_name", "User name is required");
                User test = db.Users.Where(x => x.UserName == u.UserName).FirstOrDefault();
                if(test!=null)
                {
                    ModelState.AddModelError("Store_name", "User name is already exist");
                }
                if (u.Phone == null)
                    ModelState.AddModelError("Store_Phone", "Phone number is required");
                else if(u.Phone.Length!=10)
                    ModelState.AddModelError("Store_Phone", "Phone number Must be 10 digits");
                if (store_info.Address== null)
                    ModelState.AddModelError("Store_add", "Address is required");
                return View(user_info);
            }
        }
        #endregion
    }
}