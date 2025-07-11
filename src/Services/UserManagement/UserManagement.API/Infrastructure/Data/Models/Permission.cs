using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;

namespace UserManagement.API.Infrastructure.Data.Models
{
    public class Permission : BaseEntity<int>, ISoftDelete
    {
        public string Module { get; set; } = default!;   // e.g., "Flight", "Wallet"
        public string Action { get; set; } = default!;   // e.g., "View", "Edit"
        public string Description { get; set; } = default!;
        public string Code => $"{Module}.{Action}";
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

}
