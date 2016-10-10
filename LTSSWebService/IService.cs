using System.Xml.Linq;

namespace LTSSWebService
{
    public interface IService
    {
        XDocument Request { get; set; }

        string SuccessResponse { get; }
    }

    public static class Services
    {
        public enum ServicesEnum
        {
            SendReferralService = 1
        }
    }
}