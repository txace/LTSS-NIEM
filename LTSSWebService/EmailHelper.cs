using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;

namespace LTSSWebService
{
    public static class EmailHelper
    {
        public static void SendEmail(string to, string from, string subject, string body, string smtpClient, bool useSSL = false,
            IEnumerable<Attachment> attachments = null, string userName = null, string password = null)
        {
            var client = new SmtpClient(smtpClient);
            client.UseDefaultCredentials = userName == null;
            client.EnableSsl = useSSL;
            if (userName != null)
                client.Credentials = new System.Net.NetworkCredential(userName, password);
            var message = new MailMessage(from, to);
            message.Subject = subject;
            message.Body = body;
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    message.Attachments.Add(attachment);
                }
            }

            client.Send(message);
        }

        public static MemoryStream ToStream(this string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}