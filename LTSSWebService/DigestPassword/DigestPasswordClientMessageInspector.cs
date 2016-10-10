using System;
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
    public abstract class DigestPasswordClientMessageInspector : IClientMessageInspector, IEndpointBehavior
    {
        private string _passWord;
        private string _userName;

        public DigestPasswordClientMessageInspector(string userName, string passWord) : base()
        {
            _userName = userName;
            _passWord = passWord;
        }

        public abstract Func<string, string> EvaluateResponseTemplate { get; }

        public string Reply { get; set; }

        public abstract string ResponseTemplate { get; }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public virtual void AfterReceiveReply(ref Message reply, object correlationState)
        {
            var buffer = reply.CreateBufferedCopy(Int32.MaxValue);
            var msg = buffer.CreateMessage();
            var sb = new StringBuilder();
            using (var xw = System.Xml.XmlWriter.Create(sb))
            {
                msg.WriteMessage(xw);
                xw.Close();
            }

            Reply = sb.ToString();
            reply = buffer.CreateMessage();
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var correlationState = request.Headers;

            var created = DateTime.UtcNow.AddMinutes(0);
            var expires = created.AddMinutes(10);
            var createdStr = created.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var expiresStr = expires.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            var nonce = Convert.ToBase64String(GetRandomBytes());
            string password = GetSHA1String(nonce, createdStr, _passWord);

            var Security = @"<wsse:Security
			xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd""
			xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
			<wsu:Timestamp wsu:Id=""Timestamp-" + GetRandomAlphaNumeric(33) + @""">
				<wsu:Created>" + createdStr + @"</wsu:Created>
				<wsu:Expires>" + expiresStr + @"</wsu:Expires>
			</wsu:Timestamp>
			<wsse:UsernameToken wsu:Id=""UsernameToken-" + GetRandomAlphaNumeric(33) + @""">
				<wsse:Username>" + _userName + @"</wsse:Username>
				<wsse:Password Type=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest"">" + password + @"</wsse:Password>
				<wsse:Nonce EncodingType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"">" + nonce + @"</wsse:Nonce>
				<wsu:Created>" + createdStr + @"</wsu:Created>
            </wsse:UsernameToken>
		</wsse:Security>";

            var Document = XDocument.Parse(EvaluateResponseTemplate(ResponseTemplate).Replace("[SECURITY]", Security));
            request = Message.CreateMessage(XmlDictionaryReader.CreateDictionaryReader(Document.CreateReader()), int.MaxValue, request.Version);

            return correlationState;
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        private static string GetRandomAlphaNumeric(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
        }

        private static byte[] GetRandomBytes()
        {
            var rng = new RNGCryptoServiceProvider();
            var randomBytes = new byte[16];
            rng.GetBytes(randomBytes);
            return randomBytes;
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