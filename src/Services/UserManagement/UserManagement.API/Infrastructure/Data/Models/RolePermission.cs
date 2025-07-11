using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;

namespace UserManagement.API.Infrastructure.Data.Models
{
    public class RolePermission : BaseEntity<int>, ISoftDelete
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public virtual Role Role { get; set; } = default!;
        public virtual Permission Permission { get; set; } = default!;
    }
}
