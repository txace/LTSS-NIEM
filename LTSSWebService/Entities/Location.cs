using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LTSSWebService.Models
{
    public class Location
    {
        public decimal? LatitudeDegreeValue { get; set; }

        public decimal? LatitudeMinuteValue { get; set; }

        public decimal? LatitudeSecondValue { get; set; }

        [Key]
        public int LocationID { get; set; }

        public string LocationIdentification { get; set; }

        public string LocationName { get; set; }

        public decimal? LongitudeDegreeValue { get; set; }

        public decimal? LongitudeMinuteValue { get; set; }

        public decimal? LongitudeSecondValue { get; set; }

        public virtual Organization Organization { get; set; }

        public int? OrganizationID { get; set; }
    }

    public class LocationComparer : IEqualityComparer<Location>
    {
        public static readonly LocationComparer Comparer = new LocationComparer();

        public bool Equals(Location x, Location y)
        {
            if (x == null || y == null)
                return false;
            if (object.ReferenceEquals(x, y))
                return true;
            if (x.LocationIdentification == y.LocationIdentification && x.OrganizationID == y.OrganizationID)
                return true;
            if (x.LocationID != 0 && x.LocationID == y.LocationID)
                return true;
            return false;
        }

        public int GetHashCode(Location obj)
        {
            return obj.LocationIdentification.GetHashCode();
        }
    }
}