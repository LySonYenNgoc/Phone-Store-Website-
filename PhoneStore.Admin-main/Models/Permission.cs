using System;
using System.Collections.Generic;

namespace PhoneStore.Models;

public class Permission
{
    public int PermissionId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Area { get; set; } = "";
    public string Action { get; set; } = "";
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
