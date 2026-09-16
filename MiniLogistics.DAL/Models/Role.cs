using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
