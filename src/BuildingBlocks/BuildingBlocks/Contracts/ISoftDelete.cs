namespace BuildingBlocks.Contracts
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        void RestoreFromSoftDelete()
        {
            IsDeleted = false;
            DeletedAt = null;
        }

    }
}
