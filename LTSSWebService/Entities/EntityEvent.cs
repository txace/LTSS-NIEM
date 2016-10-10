using System.ComponentModel.DataAnnotations;

namespace LTSSWebService.Models
{
    public class EntityEvent
    {
        public string Diff { get; set; }

        public int? EnittyID { get; set; }

        [Key]
        public int EntityEventID { get; set; }

        public EventEntityType? EntityType { get; set; }

        public int RequestID { get; set; }
    }
}