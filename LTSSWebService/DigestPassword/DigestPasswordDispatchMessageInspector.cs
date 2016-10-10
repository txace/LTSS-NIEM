using LTSSWebService;
using LTSSWebService.IServiceExtensions;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DigestPasswordNS
{
    public class DigestPasswordDispatchMessageInspector : IDispatchMessageInspector, IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            var correlationState = instanceContext;

            var buffer = request.CreateBufferedCopy(Int32.MaxValue);
            var msg = buffer.CreateMessage();
            var sb = new StringBuilder();
            using (var xw = System.Xml.XmlWriter.Create(sb))
            {
                msg.WriteMessage(xw);
                xw.Close();
            }

            var Document = XDocument.Parse(sb.ToString());
            var Service = (IService)instanceContext.GetServiceInstance();
            Service.Request = Document;

            var Username = Document.Root.Descendants().Where(x => x.Name.LocalName == "Username").First().Value;
            var Password = Document.Root.Descendants().Where(x => x.Name.LocalName == "Password").First().Value;
            var Nonce = Document.Root.Descendants().Where(x => x.Name.LocalName == "Nonce").First().Value;
            var Created = Document.Root.Descendants().Where(x => x.Name.LocalName == "UsernameToken").First()
                .Descendants().Where(x => x.Name.LocalName == "Created").First().Value;
            var Expires = Document.Root.Descendants().Where(x => x.Name.LocalName == "Expires").First().Value;

            var Now = DateTime.UtcNow;
            var CreatedDate = DateTime.Parse(Created);
            var ExpiredDate = DateTime.Parse(Expires);

            if ((Now < CreatedDate.AddMinutes(-10) || Now > ExpiredDate.AddMinutes(10)) &&
                (DateTime.Now < CreatedDate.AddMinutes(-10) || DateTime.Now > ExpiredDate.AddMinutes(10)))
                throw new FaultException("Message expired.");

            var CenterUserName = ConfigurationManager.AppSettings["CenterUserName"];
            var CenterPassword = ConfigurationManager.AppSettings["CenterPassword"];

            if (Username != CenterUserName)
                throw new FaultException("Invalid user name.");

            var PasswordDigest = GetSHA1String(Nonce, Created, CenterPassword);

            if (PasswordDigest != Password)
                throw new FaultException("Invalid password.");

            request = buffer.CreateMessage();

            return correlationState;
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            // This gets and removes the response from the cache
            var Service = (IService)((((InstanceContext)correlationState)).GetServiceInstance());
            var Response = Service.GetResponse();

            if (reply.IsFault)
                return;

            var replacedMessage = Message.CreateMessage(XmlReader.Create(new StringReader(Response)), int.MaxValue, reply.Version);
            replacedMessage.Headers.CopyHeadersFrom(reply.Headers);
            replacedMessage.Properties.CopyProperties(reply.Properties);
            reply = replacedMessage;
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        private static string GetSHA1String(string nonce, string created, string password)
        {
            var concatBytes = Convert.FromBase64String(nonce).Concat(Encoding.UTF8.GetBytes(created + password));
            SHA1CryptoServiceProvider sha1Hasher = new SHA1CryptoServiceProvider();
            byte[] hashedDataBytes = sha1Hasher.ComputeHash(concatBytes.ToArray());
            return Convert.ToBase64String(hashedDataBytes);
        }
    }
}