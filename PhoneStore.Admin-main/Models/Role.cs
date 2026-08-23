using System;
using System.Collections.Generic;

namespace PhoneStore.Models;

public partial class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();
    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
