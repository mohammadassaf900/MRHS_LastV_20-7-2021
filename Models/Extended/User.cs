using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
//namespace RegistrationAndLogin.Models.Extended
namespace Mobile_Repair_History_System_MRHS.Models
{
    [MetadataType(typeof(UserMetadata))]
    public class UserMetadata
    {
        [Display(Name = "User Name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "User name required")]
        public string UserName { get; set; }

        [Display(Name = "Email")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email ID required")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Minimum 6 characters required")]
        public string Password { get; set; }

        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Confirm password and password do not match")]
        public string ConfirmPassword { get; internal set; }
        [Display(Name = "Phone")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Phone is required")]
        [MinLength(10, ErrorMessage = "Minimum 10 characters required")]
        [MaxLength(10, ErrorMessage = "Maximum 10 characters required")]
        public string Phone { get; set; }
    }
}
