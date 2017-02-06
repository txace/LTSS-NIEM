using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace LTSSWebService.Models
{
    public class Email
    {
        [Required]
        public string Body { get; set; }

        [Key]
        public int EmailID { get; set; }

        [Required]
        public string From { get; set; }

        [Required]
        public string LocationName { get; set; }

        public string Password { get; set; }

        [Required]
        public string SmtpClient { get; set; }

        [Required]
        public string To { get; set; }

        public string UserName { get; set; }

        [Required]
        public bool UseSSL { get; set; }
    }
}