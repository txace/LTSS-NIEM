using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace LTSSWebService.Models
{
    public class Organization
    {
        public virtual ICollection<Referral> DestinationOrganizationReferral { get; set; }

        public virtual ICollection<Location> Location { get; set; }

        public string OrganizationBranchName { get; set; }

        public string OrganizationCode { get; set; }

        [Key]
        public int OrganizationID { get; set; }

        public string OrganizationIdentification { get; set; }

        public string OrganizationName { get; set; }

        public string OrganizationUnitName { get; set; }

        public virtual ICollection<Referral> SourceOrganizationReferral { get; set; }
    }

    public class OrganizationComparer : IEqualityComparer<Organization>
    {
        public static readonly OrganizationComparer Comparer = new OrganizationComparer();

        public bool Equals(Organization x, Organization y)
        {
            if (x == null || y == null)
                return false;
            if (object.ReferenceEquals(x, y))
                return true;
            if (x.OrganizationIdentification == y.OrganizationIdentification && x.OrganizationName == y.OrganizationName)
                return true;
            if (x.OrganizationID != 0 && x.OrganizationID == y.OrganizationID)
                return true;
            return false;
        }

        public int GetHashCode(Organization obj)
        {
            return obj.OrganizationIdentification.GetHashCode() + 3 * obj.OrganizationName.GetHashCode();
        }
    }

    public class OrganizationConfiguration : EntityTypeConfiguration<Organization>
    {
        public OrganizationConfiguration()
        {
            HasMany(e => e.SourceOrganizationReferral)
            .WithRequired(e => e.SourceOrganization)
            .HasForeignKey(e => e.SourceOrganizationID)
            .WillCascadeOnDelete(false);

            HasMany(e => e.DestinationOrganizationReferral)
            .WithRequired(e => e.DestinationOrganization)
            .HasForeignKey(e => e.DestinationOrganizationID)
            .WillCascadeOnDelete(false);

            HasMany(e => e.Location)
            .WithRequired(e => e.Organization);
        }
    }
}