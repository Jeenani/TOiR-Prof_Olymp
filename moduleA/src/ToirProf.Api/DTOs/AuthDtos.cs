namespace ToirProf.Api.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = null!;
}

public class UserProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string FullName => $"{LastName} {FirstName} {Patronymic}".Trim();
    public int RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int? WorkshopId { get; set; }
    public string? WorkshopName { get; set; }
}

public class RoleDto
{
    public int RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? RoleDescription { get; set; }
}

public class TeamDto
{
    public int TeamId { get; set; }
    public string TeamCode { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int? WorkshopId { get; set; }
    public string? WorkshopName { get; set; }
    public List<string> WorkshopNames { get; set; } = new();
    public List<TeamMemberDto> Members { get; set; } = new();
    public int ActiveOrdersCount { get; set; }
    public List<TeamActiveOrderDto> ActiveOrders { get; set; } = new();
}

public class TeamActiveOrderDto
{
    public int WorkOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string PriorityName { get; set; } = string.Empty;
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
}

public class TeamMemberDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsTeamLeader { get; set; }
}
