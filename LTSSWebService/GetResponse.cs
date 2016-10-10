using System;
using static LTSSWebService.Services;

namespace LTSSWebService
{
    public static class GetResponse
    {
        private static readonly object RequestLock = new object();

        public static void Request()
        {
            lock (RequestLock)
            {
                var Requests = Repository.NewRequests();

                foreach (var request in Requests)
                {
                    try
                    {
                        var type = Type.GetType("LTSSWebService." + ((ServicesEnum)request.ServiceType).ToString());

                        var GetRequest = (Action<int, string>)type.GetField("GetRequest").GetValue(null);
                        GetRequest.Invoke(request.RequestID, request.RequestData);
                    }
                    catch (Exception e)
                    {
                        Repository.SaveException(request.RequestID, e);
                    }
                }
            }
        }
    }
}