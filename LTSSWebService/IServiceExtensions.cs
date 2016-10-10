using System.Linq;
using System.Xml.Serialization;

namespace LTSSWebService.IServiceExtensions
{
    public static class IServiceExtensions
    {
        public static T GetRequest<T>(this IService service, string rootLocalName)
        {
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(service.Request.Descendants().Where(x => x.Name.LocalName == rootLocalName).First().CreateReader());
        }

        public static string GetResponse(this IService service)
        {
            return service.SuccessResponse;
        }
    }
}