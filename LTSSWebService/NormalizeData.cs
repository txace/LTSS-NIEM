using System;
using static LTSSWebService.Services;

namespace LTSSWebService
{
    public static class NormalizeData
    {
        private static readonly object NormalizeLock = new object();

        public static void Normalize()
        {
            lock (NormalizeLock)
            {
                var Requests = Repository.NewResponses();

                foreach (var request in Requests)
                {
                    try
                    {
                        var type = Type.GetType("LTSSWebService." + ((ServicesEnum)request.ServiceType).ToString());

                        var SaveService = (Action<int, string>)type.GetField("SaveService").GetValue(null);
                        SaveService.Invoke(request.RequestID, request.ResponseData);
                        Repository.MarkRequestAsProccessed(request.RequestID);
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