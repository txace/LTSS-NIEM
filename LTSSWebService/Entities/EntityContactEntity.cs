using System.Data.Entity.ModelConfiguration;

namespace LTSSWebService.Models
{
    public enum ContactEntityType
    {
        AR, //AuthorizedRepresentative,
        CR, //CareRecipient,
        PA, //PersonAssociation,
        SA, //ScreeningApplicant,
        CI, //ContactInformation
        LO, //Location
        OR //Organization
    }

    public class EntityContactEntity
    {
        public virtual ContactEntity ContactEntity { get; set; }

        public int ContactEntityID { get; set; }

        public string ContactEntityType { get; set; }

        public string EntityID { get; set; }
    }

    public class EntityContactEntityConfiguration : EntityTypeConfiguration<EntityContactEntity>
    {
        public EntityContactEntityConfiguration()
        {
            this.HasKey(x => new { x.EntityID, x.ContactEntityID, x.ContactEntityType });
        }
    }
}