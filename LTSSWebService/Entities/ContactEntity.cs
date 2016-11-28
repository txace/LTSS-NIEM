using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LTSSWebService.Models
{
    public class ContactEntity
    {
        public string CategoryCode { get; set; }

        public string ContactEmailID { get; set; }

        [Key]
        public int ContactEntityID { get; set; }

        public string ContactMailingAddress { get; set; }

        public string ContactTelephoneNumber { get; set; }

        public string ContactWebsiteURI { get; set; }

        public string EthnicityCode { get; set; }

        public virtual ICollection<Location> Location { get; set; }

        public virtual ICollection<Organization> Organization { get; set; }

        public string PersonAgeMeasure { get; set; }

        public string PersonBirthDate { get; set; }

        public string PersonFullName { get; set; }

        public string PersonGivenName { get; set; }

        public bool? PersonLivingIndicator { get; set; }

        public string PersonMiddleName { get; set; }

        public string PersonNamePrefixText { get; set; }

        public string PersonNameSuffixText { get; set; }

        public string PersonSSNIdentification { get; set; }

        public string PersonSurName { get; set; }

        public string PersonSurNamePrefixText { get; set; }

        public bool? PersonUSCitizenIndicator { get; set; }

        public string RACCode { get; set; }

        public string SEXCode { get; set; }
    }

    public class ContactEntityComparer : IEqualityComparer<ContactEntity>
    {
        public static readonly ContactEntityComparer Comparer = new ContactEntityComparer();

        public static bool IsEmpty(ContactEntity x)
        {
            if (x == null)
                return true;

            if (!string.IsNullOrWhiteSpace(x.PersonSSNIdentification)
                || !string.IsNullOrWhiteSpace(x.ContactEmailID)
                || !string.IsNullOrWhiteSpace(x.ContactWebsiteURI)
                || !string.IsNullOrWhiteSpace(x.ContactTelephoneNumber)
                || !string.IsNullOrWhiteSpace(x.PersonGivenName)
                || !string.IsNullOrWhiteSpace(x.PersonSurName)
                || !string.IsNullOrWhiteSpace(x.ContactMailingAddress))
            {
                return false;
            }

            return true;
        }

        public bool Equals(ContactEntity x, ContactEntity y)
        {
            if (x == null || y == null)
                return false;
            if (object.ReferenceEquals(x, y))
                return true;

            if (x.ContactEntityID != 0 && x.ContactEntityID == y.ContactEntityID)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(x.PersonSSNIdentification) && !string.IsNullOrWhiteSpace(y.PersonSSNIdentification))
            {
                return x.PersonSSNIdentification == y.PersonSSNIdentification;
            }

            if (!string.IsNullOrWhiteSpace(x.ContactEmailID) && !string.IsNullOrWhiteSpace(y.ContactEmailID))
            {
                return x.ContactEmailID == y.ContactEmailID;
            }

            if (!string.IsNullOrWhiteSpace(x.ContactWebsiteURI) && !string.IsNullOrWhiteSpace(y.ContactWebsiteURI))
            {
                return x.ContactWebsiteURI == y.ContactWebsiteURI;
            }

            if (!string.IsNullOrWhiteSpace(x.PersonBirthDate) && !string.IsNullOrWhiteSpace(x.PersonGivenName) && !string.IsNullOrWhiteSpace(x.PersonSurName) &&
                !string.IsNullOrWhiteSpace(y.PersonBirthDate) && !string.IsNullOrWhiteSpace(y.PersonGivenName) && !string.IsNullOrWhiteSpace(y.PersonSurName))
            {
                return x.PersonBirthDate == y.PersonBirthDate && x.PersonGivenName == y.PersonGivenName && x.PersonSurName == y.PersonSurName;
            }

            if (!string.IsNullOrWhiteSpace(x.ContactTelephoneNumber) && !string.IsNullOrWhiteSpace(y.ContactTelephoneNumber))
            {
                return x.ContactTelephoneNumber == y.ContactTelephoneNumber;
            }

            if (!string.IsNullOrWhiteSpace(x.ContactMailingAddress) && !string.IsNullOrWhiteSpace(y.ContactMailingAddress))
            {
                return x.ContactMailingAddress == y.ContactMailingAddress;
            }

            if (!string.IsNullOrWhiteSpace(x.PersonAgeMeasure) && !string.IsNullOrWhiteSpace(x.PersonGivenName) && !string.IsNullOrWhiteSpace(x.PersonSurName) &&
                !string.IsNullOrWhiteSpace(y.PersonAgeMeasure) && !string.IsNullOrWhiteSpace(y.PersonGivenName) && !string.IsNullOrWhiteSpace(y.PersonSurName))
            {
                return x.PersonAgeMeasure == y.PersonAgeMeasure && x.PersonGivenName == y.PersonGivenName && x.PersonSurName == y.PersonSurName;
            }

            if ((!string.IsNullOrWhiteSpace(x.PersonGivenName) && !string.IsNullOrWhiteSpace(y.PersonGivenName))
                || (!string.IsNullOrWhiteSpace(x.PersonSurName) && !string.IsNullOrWhiteSpace(y.PersonSurName)))
            {
                return x.PersonGivenName == y.PersonGivenName && x.PersonSurName == y.PersonSurName;
            }

            return false;
        }

        public int GetHashCode(ContactEntity obj)
        {
            return 0;  // the intersection of identifying fields is empty
        }
    }
}