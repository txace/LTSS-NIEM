using System.ComponentModel.DataAnnotations;

namespace LTSSWebService.Models
{
    public class Enrollment
    {
        public string EndDate { get; set; }

        [Key]
        public int EnrollmentID { get; set; }

        public string IdentificationID { get; set; }

        public string PlanName { get; set; }

        public string ProgramName { get; set; }

        public virtual Screening Screening { get; set; }

        public string ScreeningID { get; set; }

        public string StartDate { get; set; }
    }
}