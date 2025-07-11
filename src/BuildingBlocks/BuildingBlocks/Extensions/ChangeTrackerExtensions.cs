using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BuildingBlocks.Extensions
{
    public static class ChangeTrackerExtensions
    {
        public static void SetAuditProperties(this ChangeTracker changeTracker)
        {
            var entities = changeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

            var currentTime = DateTime.UtcNow;

            foreach (var entry in entities)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        SetPropertyValue(entry, "CreatedAt", currentTime);
                        SetPropertyValue(entry, "UpdatedAt", currentTime);
                        break;

                    case EntityState.Modified:
                        SetPropertyValue(entry, "UpdatedAt", currentTime);
                        break;

                    case EntityState.Deleted when entry.Entity is ISoftDelete softDeleteEntity:
                        SetPropertyValue(entry, "UpdatedAt", currentTime);
                        SetPropertyValue(entry, nameof(ISoftDelete.IsDeleted), true);
                        SetPropertyValue(entry, nameof(ISoftDelete.DeletedAt), currentTime);
                        entry.State = EntityState.Modified;
                        break;
                }
            }
        }

        private static void SetPropertyValue(EntityEntry entry, string propertyName, object value)
        {
            var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
            if (property != null)
            {
                property.CurrentValue = value;
            }
        }
    }
}