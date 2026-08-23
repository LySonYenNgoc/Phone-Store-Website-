namespace PhoneStore.Models
{
    public partial class Admin
    {
        public int AdminId { get; set; }

        public string? FullName { get; set; }

        public string? Username { get; set; }

        public string? PasswordHash { get; set; }        public DateOnly? BirthDate { get; set; }

        public string? NationalId { get; set; }

        public bool IsApproved { get; set; } = false;

        public bool IsBlocked { get; set; } = false;

        public int? RoleId { get; set; }

        public virtual Role? Role { get; set; }
    }
}
