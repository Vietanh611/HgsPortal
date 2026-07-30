using Domain.Entities.System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Identity
{
    [Table("Modules")]
    public class Modules
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        public bool IsActive { get; set; } = true;

        public int? SortOrder { get; set; }

        #region Navigation Properties

        public virtual ICollection<UserModules> UserModules { get; set; } = new List<UserModules>();
        public virtual ICollection<Menus> Menus { get; set; } = new List<Menus>();
        #endregion
    }
}
