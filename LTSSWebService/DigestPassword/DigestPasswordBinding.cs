using System.ServiceModel.Channels;

namespace DigestPasswordNS
{
    public static class DigestPasswordBinding
    {
        public static Binding CreateBinding()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var Encoding = new TextMessageEncodingBindingElement();
            Encoding.MessageVersion = MessageVersion.Soap12;
            Encoding.WriteEncoding = System.Text.Encoding.UTF8;

            var Transport = new HttpsTransportBindingElement();
            Transport.MaxReceivedMessageSize = 20000000;
            Transport.MaxBufferPoolSize = 20000000;
            Transport.MaxBufferSize = 20000000;
            Transport.MaxPendingAccepts = 100;

            var Binding = new CustomBinding();
            Binding.Elements.Add(Encoding);
            Binding.Elements.Add(Transport);

            return Binding;
        }
    }
}