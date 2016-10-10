using LTSSWebService.Schema;

namespace LTSSWebService.Models
{
    public class SendReferralRequest
    {
        public string PersonLtssID { get; set; }

        public string ReferralCreatedDate { get; set; }

        public ReferralGenerationCodeSimpleType? ReferralGenerationCode { get; set; }

        public string ReferralID { get; set; }

        public ReferralPriorityCodeSimpleType? ReferralPriorityCode { get; set; }

        public string ReferralReason { get; set; }

        public string ScreeningID { get; set; }
    }
}