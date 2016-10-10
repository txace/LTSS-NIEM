using System;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace DigestPasswordNS
{
    /// <summary>
    /// This class is responsible for creating a servicehost that includes a basic
    /// authentication request interceptor.
    /// </summary>
    public class DigestPasswordServiceHostFactory : ServiceHostFactoryBase
    {
        public override ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses)
        {
            return new DigestPasswordServiceHost(Type.GetType(constructorString), baseAddresses);
        }
    }
}