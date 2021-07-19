using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mobile_Repair_History_System_MRHS.Models;
using System.Net.Mail;
using System.Net;
using System.Web.Security;
using System.Web.Helpers;

namespace Mobile_Repair_History_System_MRHS.Controllers
{
    public class UserController : Controller
    {
        public MRHSEntities db = new MRHSEntities();
        //Registration Action
        public ActionResult Registration()
        {
            return View();
        }
        //Registration POST action 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified,ActivationCode")] User user, string ConfirmPassword)
        {
            bool Status = false;
            string message = "";
            // Model Validation 
            if (ModelState.IsValid)
            {
                try
                {
                    #region Email is already Exist 
                    var isExist = IsEmailExist(user.Email);
                    if (isExist)
                    {
                        ModelState.AddModelError("EmailExist", "Email already exist");
                        return View(user);
                    }
                    #endregion

                    #region Generate Activation Code 
                    user.ActivationCode = Guid.NewGuid();
                    #endregion

                    #region  Password Hashing 
                    user.Password = Crypto.Hash(user.Password);
                    user.ConfirmPassword = Crypto.Hash(ConfirmPassword); //
                    #endregion

                    user.IsEmailVerified = false;
                    user.RoleID = 4;
                    user.Status = "Active";
                    #region Save to Database

                    db.Users.Add(user);
                    db.SaveChanges();

                    //Send Email to User
                    SendVerificationLinkEmail(user.Email, user.ActivationCode.ToString());
                    message = "Registration successfully done. Account activation link " +
                        " has been sent to your email:" + user.Email;
                    Status = true;

                    #endregion
                }
                catch
                {
                    User test = db.Users.Where(x => x.UserName == user.UserName).FirstOrDefault();
                    if (test != null)
                    {
                        ModelState.AddModelError("UserName", "User name is already exist");
                    }
                }
            }
            else
            {
                message = "Invalid Request";
            }
            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(user);
        }
        //Verify Account  
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
            db.Configuration.ValidateOnSaveEnabled = false; // This line I have added here to avoid 
                                                            // Confirm password does not match issue on save changes
            var v = db.Users.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
            if (v != null)
            {
                v.IsEmailVerified = true;
                db.SaveChanges();
                Status = true;
            }
            else
            {
                ViewBag.Message = "Invalid Request";
            }
            ViewBag.Status = Status;
            return View();
        }
        //Login 
        [HttpGet]
        public ActionResult Login()
        {  
            if (Session["username"] != null)
            {
                return RedirectToAction("Index", "Home", new { username = Session["username"].ToString() });
            }
            else
            {
                return View();
            }
        }
        //Login POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login, string ReturnUrl )
        {
            string message = "";
            var v = db.Users.Where(a => a.Email == login.Email).FirstOrDefault();
            if (v != null)
            {
                if (!v.IsEmailVerified)
                {
                    ViewBag.Message = "Please verify your email first";
                    return View();
                }
                if(v.Status=="Blocked")
                {
                    ViewBag.Message = "Your Account is Blocked, You Can Contact Us Here...";
                    return View();
                }
                if (string.Compare(Crypto.Hash(login.Password), v.Password) == 0)
                {
                    int timeout = login.RememberMe ? 525600 : 525600; // 525600 min = 1 year
                    var ticket = new FormsAuthenticationTicket(login.Email, login.RememberMe, timeout);
                    string encrypted = FormsAuthentication.Encrypt(ticket);
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
                    {
                        Expires = DateTime.Now.AddMinutes(timeout),
                        HttpOnly = true
                    };
                    Response.Cookies.Add(cookie);
                    //FormsAuthentication.SetAuthCookie(login.Email, false); //if close bro.(false)

                    //if (User.Identity.IsAuthenticated)
                    //{
                    //    Session["username"] = login.Email;
                    //}
                    Session["username"] = login.Email;
                    if (Url.IsLocalUrl(ReturnUrl))
                    {
                        return Redirect(ReturnUrl);
                    }
                    else
                    {
                        switch (v.RoleID)
                        {
                            case 1: return RedirectToAction("Index", "Home", new { username = login.Email });
                            case 2: return RedirectToAction("Index", "Store", new { username = login.Email });
                            case 3: return RedirectToAction("Index", "Home", new { username = login.Email });
                            case 4: return RedirectToAction("Index", "Home", new { username = login.Email });
                        }
                    }
                }
                else
                {
                    message = "Invalid credential provided";
                }
            }
            else
            {
                message = "Invalid credential provided";
            }
            ViewBag.Message = message;
            return View();
        }
        //Logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Abandon(); // it will clear the session at the end of request
            return RedirectToAction("Index", "Home");
        }
        [NonAction]
        public bool IsEmailExist(string email)
        {
            var v = db.Users.Where(a => a.Email == email).FirstOrDefault();
            return v != null;
        }
        [NonAction]
        public void SendVerificationLinkEmail(string Email, string activationCode, string emailFor = "VerifyAccount")
        {
            var verifyUrl = "/User/" + emailFor + "/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);
            var fromEmail = new MailAddress("assafm281@gmail.com", "MRHS");
            var toEmail = new MailAddress(Email);
            var fromEmailPassword = "123456Mo123456"; // Replace with actual password
            string subject = "";
            string body = "";
            if (emailFor == "VerifyAccount")
            {
                subject = "Your account is successfully created!";
                body = "<br/><br/>We are excited to tell you that your Dotnet Awesome account is" +
                    " successfully created. Please click on the below link to verify your account" +
                    " <br/><br/><a href='" + link + "'>" + link + "</a> ";
            }
            else if (emailFor == "ResetPassword")
            {
                subject = "Reset Password";
                body = "Hi,<br/>br/>We got request for reset your account password. Please click on the below link to reset your password" +
                    "<br/><br/><a href=" + link + ">Reset Password link</a>";
            }
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };
            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                smtp.Send(message);
        }
        //Part 3 - Forgot Password
        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForgotPassword(string Email)
        {
            //Verify Email
            //Generate Reset password link 
            //Send Email 
            string message = "";
            var account = db.Users.Where(a => a.Email == Email).FirstOrDefault();
            if (account != null)
            {
                if (!account.IsEmailVerified)
                {
                    ViewBag.Message = "Please verify your email first";
                    return View();
                }
                //Send email for reset password
                string resetCode = Guid.NewGuid().ToString();
                SendVerificationLinkEmail(account.Email, resetCode, "ResetPassword");
                account.ResetPasswordCode = resetCode;
                //This line I have added here to avoid confirm password not match issue , as we had added a confirm password property 
                //in our model class in part 1
                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();
                message = "Reset password link has been sent to your email.";
                ViewBag.resetCode = resetCode;
                ViewBag.Message = message;
                return View();
            }
            else
            {
                message = "Account not found";
                ViewBag.Message = message;
                return View();
            }
        }
        public ActionResult ResetPassword(string id)
        {
            //Verify the reset password link
            //Find account associated with this link
            //redirect to reset password page
            if (string.IsNullOrWhiteSpace(id))
            {
                return HttpNotFound();
            }
            var user = db.Users.Where(a => a.ResetPasswordCode == id).FirstOrDefault();
            if (user != null)
            {
                ResetPasswordModel model = new ResetPasswordModel
                {
                    ResetCode = id
                };
                return View(model);
            }
            else
            {
                return HttpNotFound();
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            var message = "";
            if (ModelState.IsValid)
            {
                var user = db.Users.Where(a => a.ResetPasswordCode == model.ResetCode).FirstOrDefault();
                if (user != null)
                {
                    user.Password = Crypto.Hash(model.NewPassword);
                    user.ResetPasswordCode = model.ResetCode;
                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                    message = "New password updated successfully";
                    ViewBag.Message = message;
                    return RedirectToAction("Login","User");
                }
                else
                {
                    message = "Something invalid";
                    ViewBag.Message = message;
                    return View(model);
                }
            }
            else
            {
                message = "Something invalid";
                ViewBag.Message = message;
                return View(model);
            }
        }
    }
}