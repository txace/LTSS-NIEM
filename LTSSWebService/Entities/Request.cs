using System;
using System.ComponentModel.DataAnnotations;
using static LTSSWebService.Services;

namespace LTSSWebService.Models
{
    public class Request
    {
        public string Exception { get; set; }

        public bool IsProcessed { get; set; }

        public string RequestData { get; set; }

        public DateTime RequestDate { get; set; }

        [Key]
        public int RequestID { get; set; }

        public string ResponseData { get; set; }

        public ServicesEnum ServiceType { get; set; }
    }
}