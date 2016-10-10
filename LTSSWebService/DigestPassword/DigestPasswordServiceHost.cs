using LTSSWebService;
using System;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace DigestPasswordNS
{
    public class DigestPasswordServiceHost : ServiceHost
    {
        public DigestPasswordServiceHost(Type serviceType, Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
        }

        protected override void InitializeRuntime()
        {
            Description.Behaviors.Add(new ServiceMetadataBehavior
            {
                HttpGetEnabled = false,
                HttpsGetEnabled = true,
                HttpsGetUrl = new Uri(ConfigurationManager.AppSettings["CenterEndpoint"] + @"/LTSSWebService.SendReferralService.svc/mex")
            });

            var ServiceBehavior = (ServiceBehaviorAttribute)Description.Behaviors.Where(x => x is ServiceBehaviorAttribute).First();
            ServiceBehavior.AddressFilterMode = AddressFilterMode.Any;
            ServiceBehavior.ValidateMustUnderstand = false;

            AddServiceDebugBehavior();

            AddEndpoints();

            AddServiceCredentialBehavior();

            Description.Behaviors.RemoveAll<ServiceAuthorizationBehavior>();

            Description.Behaviors.RemoveAll<ServiceAuthenticationBehavior>();

            base.InitializeRuntime();
        }

        private void AddEndpoints()
        {
            foreach (Uri address in BaseAddresses)
            {
                var ServiceEndpoint = this.AddServiceEndpoint(typeof(ISendReferralService), DigestPasswordBinding.CreateBinding(), address);
                ServiceEndpoint.EndpointBehaviors.Add(new DigestPasswordDispatchMessageInspector());
            }
        }

        private void AddServiceCredentialBehavior()
        {
            Description.Behaviors.RemoveAll<ServiceCredentials>();
            var Credential = new ServiceCredentials();
            Credential.ServiceCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindByThumbprint,
                new string(ConfigurationManager.AppSettings["CenterSSLCertThumbprint"].AsEnumerable().Where(x => Char.IsLetter(x) || Char.IsNumber(x)).ToArray()));
            Description.Behaviors.Add(Credential);
        }

        private void AddServiceDebugBehavior()
        {
            var debugBehavior = Description.Behaviors.Find<ServiceDebugBehavior>();
            if (debugBehavior == null)
            {
                debugBehavior = new ServiceDebugBehavior();
                Description.Behaviors.Add(new ServiceDebugBehavior());
            }

            debugBehavior.IncludeExceptionDetailInFaults = true;
        }

        private void AddServiceMetadataBehavior()
        {
            var metadataBehavior = Description.Behaviors.Find<ServiceMetadataBehavior>();

            if (metadataBehavior == null)
            {
                ServiceMetadataBehavior serviceMetadataBehavior = new ServiceMetadataBehavior();
                serviceMetadataBehavior.HttpsGetEnabled = true;
                Description.Behaviors.Add(serviceMetadataBehavior);
            }
        }
    }
}