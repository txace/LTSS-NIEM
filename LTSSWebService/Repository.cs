using Application.Data.XML;
using Application.ExceptionExtensions;
using Application.GenericExtensions;
using Application.IDictionaryExtensions;
using Application.IEnumerableExtensions;
using Application.ObjectExtensions;
using Application.PropertyFieldInfoExtensions;
using LTSSWebService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace LTSSWebService
{
    public static class Repository
    {
        public static T Add<T>(this ReferralDbContext context, object request, bool identityInsert = false) where T : class, new()
        {
            var result = new T();
            request.InjectInto(ref result);

            var TableName = typeof(T).Name;
            var localContext = context ?? new ReferralDbContext();
            using (context != null ? null : localContext)
            {
                if (identityInsert)
                    context.Database.ExecuteSqlCommand(@"SET IDENTITY_INSERT [dbo].[" + TableName + "] ON");
                ((DbSet<T>)typeof(ReferralDbContext).GetProperty(TableName).GetValue(localContext)).Add(result);
                localContext.SaveChanges();
                if (identityInsert)
                    context.Database.ExecuteSqlCommand(@"SET IDENTITY_INSERT [dbo].[" + TableName + "] OFF");

                return result;
            }
        }

        public static void AddEntityContactEntities(this ReferralDbContext context, IList<object> request)
        {
            var DistincteEntityContactEntities = ObjectExtensions.InjectIntoList<EntityContactEntity>(request)
                .Where(x => x.ContactEntityID != 0)
                .GroupBy(x => new { x.EntityID, x.ContactEntityType, x.ContactEntityID }).Select(x => x.First()).ToArray();

            var localContext = context ?? new ReferralDbContext();
            using (context != null ? null : localContext)
            {
                var Command = string.Join("", DistincteEntityContactEntities.Select(x => @"BEGIN
                   IF NOT EXISTS (SELECT * FROM EntityContactEntity
                                   WHERE ContactEntityID = '" + x.ContactEntityID + @"'
                                   AND ContactEntityType = '" + x.ContactEntityType + @"'
                                   AND EntityID = '" + x.EntityID + @"')
                   BEGIN
                       INSERT INTO EntityContactEntity (ContactEntityID, ContactEntityType, EntityID)
                       VALUES ('" + x.ContactEntityID + @"', '" + x.ContactEntityType + @"', '" + x.EntityID + @"')
                   END
                END;").ToArray());

                context.Execute(Command);
                context.SaveChanges();
            }
        }

        public static List<T> AddRange<T>(this ReferralDbContext context, IList<object> request, bool identityInsert = false) where T : class, new()
        {
            var Entities = ObjectExtensions.InjectIntoList<T>(request);
            var TableName = typeof(T).Name;
            var localContext = context ?? new ReferralDbContext();
            using (context != null ? null : localContext)
            {
                if (identityInsert)
                    context.Database.ExecuteSqlCommand(@"SET IDENTITY_INSERT [dbo].[" + TableName + "] ON");
                ((System.Data.Entity.DbSet<T>)typeof(ReferralDbContext).GetProperty(TableName).GetValue(localContext)).AddRange(Entities);
                localContext.SaveChanges();
                if (identityInsert)
                    context.Database.ExecuteSqlCommand(@"SET IDENTITY_INSERT [dbo].[" + TableName + "] OFF");

                return Entities;
            }
        }

        public static List<Screening> GetScreenings(this ReferralDbContext context, IEnumerable<string> screeningIDs)
        {
            var localContext = context ?? new ReferralDbContext();
            using (context != null ? null : localContext)
            {
                return context.Screening.Where(x => screeningIDs.Contains(x.ScreeningID)).ToList();
            }
        }

        public static void MarkRequestAsProccessed(int requestID)
        {
            using (var context = ReferralDbContext.Create(0))
            {
                context.Request.Where(x => x.RequestID == requestID).First().IsProcessed = true;
                context.SaveChanges();
            }
        }

        public static List<Models.Request> NewRequests()
        {
            using (var context = ReferralDbContext.Create(0))
            {
                return context.Request.Where(x => x.ResponseData == null && x.Exception == null)
                    .OrderBy(x => x.RequestDate).ToList();
            }
        }

        public static List<Models.Request> NewResponses()
        {
            using (var context = ReferralDbContext.Create(0))
            {
                return context.Request.Where(x => !x.IsProcessed && x.Exception == null)
                                .OrderBy(x => x.RequestDate).ToList().TakeWhile(x => x.ResponseData != null).ToList();
            }
        }

        public static HashSet<ContactEntity> SaveContactEntity(this ReferralDbContext context, IList<object> request)
        {
            var DistinctContacts = ObjectExtensions.InjectIntoList<ContactEntity>(request)
            .Where(x => !ContactEntityComparer.IsEmpty(x))
            .GroupBy(x => x, ContactEntityComparer.Comparer)
            .Select(x =>
            {
                var Contact = new ContactEntity();
                var ContactDict = typeof(ContactEntity).DBPrimativePropsAndFields().ToDictionary(y => y.Name(),
                    y => x.Select(z => y.GetValue(z)).Where(z => z != null).FirstOrDefault());
                ContactDict.InjectInto(ref Contact);
                return Contact;
            })
            .ToList();

            var result = new HashSet<ContactEntity>(ContactEntityComparer.Comparer);
            var localContext = context ?? new ReferralDbContext();
            using (context != null ? null : localContext)
            {
                for (var i = 0; i < DistinctContacts.Count; i++)
                {
                    var contactEntity = DistinctContacts[i];

                    ContactEntity Contact = null;
                    var query = localContext.ContactEntity.AsQueryable();
                    if (!string.IsNullOrWhiteSpace(contactEntity.PersonSSNIdentification))
                    {
                        Contact = query.Where(x => x.PersonSSNIdentification == contactEntity.PersonSSNIdentification).FirstOrDefault();
                        query = query.Where(x => string.IsNullOrEmpty(x.PersonSSNIdentification));
                    }

                    if (Contact == null && (!string.IsNullOrWhiteSpace(contactEntity.ContactEmailID)))
                    {
                        Contact = query.Where(x => x.ContactEmailID == contactEntity.ContactEmailID).FirstOrDefault();
                        query = query.Where(x => string.IsNullOrEmpty(x.ContactEmailID));
                    }

                    if (Contact == null && (!string.IsNullOrWhiteSpace(contactEntity.ContactWebsiteURI)))
                    {
                        Contact = query.Where(x => x.ContactWebsiteURI == contactEntity.ContactWebsiteURI).FirstOrDefault();
                        query = query.Where(x => string.IsNullOrEmpty(x.ContactWebsiteURI));
                    }

                    if (Contact == null && !string.IsNullOrWhiteSpace(contactEntity.PersonBirthDate) && !string.IsNullOrWhiteSpace(contactEntity.PersonGivenName) && !string.IsNullOrWhiteSpace(contactEntity.PersonSurName))
                    {
                        Contact = query.Where(x => x.PersonBirthDate == contactEntity.PersonBirthDate
                        && x.PersonGivenName == contactEntity.PersonGivenName && x.PersonSurName == contactEntity.PersonSurName).FirstOrDefault();
                        query = query.Where(x => string.IsNullOrEmpty(x.PersonBirthDate));
                    }

                    if (string.IsNullOrWhiteSpace(contactEntity.PersonGivenName) && string.IsNullOrWhiteSpace(contactEntity.PersonSurName))
                    {
                        if (Contact == null && (!string.IsNullOrWhiteSpace(contactEntity.ContactTelephoneNumber)))
                        {
                            Contact = query.Where(x => x.ContactTelephoneNumber == contactEntity.ContactTelephoneNumber).FirstOrDefault();
                            query = query.Where(x => string.IsNullOrEmpty(x.ContactTelephoneNumber));
                        }

                        if (Contact == null && (!string.IsNullOrWhiteSpace(contactEntity.ContactMailingAddress)))
                        {
                            Contact = query.Where(x => x.ContactMailingAddress == contactEntity.ContactMailingAddress).FirstOrDefault();
                            query = query.Where(x => string.IsNullOrEmpty(x.ContactMailingAddress));
                        }
                    }

                    localContext.SaveEntity(ref Contact, ref contactEntity);
                    result.Add(contactEntity);
                }

                localContext.SaveChanges();

                return result;
            }
        }

        public static void SaveException(int requestID, Exception e)
        {
            using (var context = ReferralDbContext.Create(requestID))
            {
                context.Request.Where(x => x.RequestID == requestID).First().Exception = e.FullException();
                context.SaveChanges();
            }
        }

        public static HashSet<Location> SaveLocation(this ReferralDbContext context, IList<object> request)
        {
            var DistinctLocations = ObjectExtensions.InjectIntoList<Location>(request)
            .GroupBy(x => x, new LocationComparer())
            .Select(x =>
            {
                var Location = new Location();
                var LocationDict = typeof(Location).DBPrimativePropsAndFields().ToDictionary(y => y.Name(),
                    y => x.Select(z => y.GetValue(z)).Where(z => z != null).FirstOrDefault());
                LocationDict.InjectInto(ref Location);
                return Location;
            }).ToList();

            var result = new HashSet<Location>(new LocationComparer());
            var localContext = context ?? new ReferralDbContext();
            using (context != null ? null : localContext)
            {
                var LocationIdentifications = DistinctLocations.Select(x => x.LocationIdentification)
                    .Where(x => !string.IsNullOrEmpty(x)).AsQueryable();

                var LocationIdentificationDictionary = localContext.Location.Where(x => LocationIdentifications.Contains(x.LocationIdentification))
                    .ToDictionary(x => x.LocationIdentification, x => x);

                for (var i = 0; i < DistinctLocations.Count; i++)
                {
                    var location = DistinctLocations[i];

                    var Location = LocationIdentificationDictionary.GetValueOrDefault(location.LocationIdentification);
                    localContext.SaveEntity(ref Location, ref location);

                    result.Add(location);
                }

                localContext.SaveChanges();

                return result;
            }
        }

        public static HashSet<Organization> SaveOrganization(this ReferralDbContext context, IList<object> request)
        {
            var DistinctOrganizations = ObjectExtensions.InjectIntoList<Organization>(request)
            .GroupBy(x => x, new OrganizationComparer())
            .Select(x =>
            {
                var Organization = new Organization();
                var OrganizationDict = typeof(Organization).DBPrimativePropsAndFields().ToDictionary(y => y.Name(),
                    y => x.Select(z => y.GetValue(z)).Where(z => z != null).FirstOrDefault());
                OrganizationDict.InjectInto(ref Organization);
                return Organization;
            }).ToList();

            var result = new HashSet<Organization>(new OrganizationComparer());
            var localContext = context ?? new ReferralDbContext();
            using (context != null ? null : localContext)
            {
                var OrganizationIdentifications = DistinctOrganizations.Select(x => x.OrganizationIdentification)
                    .Where(x => !string.IsNullOrEmpty(x)).AsQueryable();

                var OrganizationIdentificationDictionary = localContext.Organization.Where(x => OrganizationIdentifications.Contains(x.OrganizationIdentification))
                    .ToDictionary(x => x.OrganizationIdentification, x => x);

                for (var i = 0; i < DistinctOrganizations.Count; i++)
                {
                    var organization = DistinctOrganizations[i];

                    var Organization = OrganizationIdentificationDictionary.GetValueOrDefault(organization.OrganizationIdentification);
                    localContext.SaveEntity(ref Organization, ref organization);

                    result.Add(organization);
                }

                localContext.SaveChanges();

                return result;
            }
        }

        public static void SaveResponse(int requestID, string response)
        {
            using (var context = ReferralDbContext.Create(requestID))
            {
                context.Request.Where(x => x.RequestID == requestID).First().ResponseData = response;
                context.SaveChanges();
            }
        }

        public static void SaveSendReferralRequest(Schema.SendReferralRequestPayloadType request)
        {
            using (var context = ReferralDbContext.Create(0))
            {
                var Request = new Request
                {
                    RequestData = JsonConvert.SerializeObject(new SendReferralRequest
                    {
                        ReferralReason = request?.ReferralReason?.Value,
                        ReferralCreatedDate = request?.ReferralCreatedDate?.Item.SelectOrDefault(SendReferralService.ExtractDate, null),
                        ReferralGenerationCode = request?.ReferralGenerationCode?.Value,
                        PersonLtssID = request?.PersonLtssID?.IdentificationID?.Value,
                        ReferralID = request?.ReferralID?.IdentificationID?.Value,
                        ScreeningID = request?.ScreeningID?.IdentificationID?.Value,
                        ReferralPriorityCode = request?.ReferralPriorityCode?.Value,
                    }, JsonSerializer.Settings),
                    ServiceType = Services.ServicesEnum.SendReferralService,
                    RequestDate = DateTime.Now
                };
                context.Request.Add(Request);

                context.SaveChanges();
            }
        }

        public static void SaveSendReferralService(int requestID, string response)
        {
            using (var context = ReferralDbContext.Create(requestID))
            {
                context.CreateOrContinueTransaction();

                var Screenings = XML.ToObjectFromXml<LTSSWebService.LTSSReferralManagementService.Envelope>(response)
                                       .Body.retrieveReferralInfoResponse.RetrieveReferralInfoResponse.ScreeningApplication;
                if (Screenings == null)
                    throw new Exception("The sent XML is invalid.");

                var ExistingScreeningIDs = context.GetScreenings(Screenings.Select(x => x.ScreeningID?.IdentificationID?.Value)).Select(x => x.ScreeningID);
                if (ExistingScreeningIDs.Any())
                    throw new Exception("The ScreeningID: " + ExistingScreeningIDs.Aggregate(x => x, (result, x) => result + ", " + x) + " has already been sent.");

                SendReferralService.SaveScreenings(Screenings, context);
                context.Commit();
            }
        }

        private static void SaveEntity<T>(this ReferralDbContext context, ref T DBEntity, ref T requestEntity) where T : class
        {
            if (DBEntity != null)
            {
                var key = PrimaryKeyHelper.PrimaryKeyValue(DBEntity);
                requestEntity.InjectInto(ref DBEntity);
                PrimaryKeyHelper.SetPrimaryKeyValue(DBEntity, key);
                DBEntity.InjectInto(ref requestEntity);
            }
            else
                ((DbSet<T>)typeof(ReferralDbContext).GetProperty(typeof(T).Name).GetValue(context)).Add(requestEntity);
        }
    }
}