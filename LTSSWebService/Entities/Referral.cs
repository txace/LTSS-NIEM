using System;
using System.ComponentModel.DataAnnotations;

namespace LTSSWebService.Models
{
    public enum ReferralType
    {
        S, // Sent
        I // Identified
    }

    public class Referral
    {
        public virtual Organization DestinationOrganization { get; set; }

        public int? DestinationOrganizationID { get; set; }

        public string ReferralBasisForOutcome { get; set; }

        public string ReferralCreatedBy { get; set; }

        public DateTime? ReferralCreatedDate { get; set; }

        public string ReferralGenerationCode { get; set; }

        [Key]
        public string ReferralID { get; set; }

        public string ReferralPriorityCode { get; set; }

        public string ReferralReason { get; set; }

        public string ReferralReasonCode { get; set; }

        public string ReferralStatusCode { get; set; }

        public string ReferralType { get; set; }

        public string ReferralUpdatedBy { get; set; }

        public DateTime? ReferralUpdatedDate { get; set; }

        public virtual Screening Screening { get; set; }

        public string ScreeningID { get; set; }

        public virtual Organization SourceOrganization { get; set; }

        public int? SourceOrganizationID { get; set; }
    }
}