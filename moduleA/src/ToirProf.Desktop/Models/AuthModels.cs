using System;
using System.Collections.Generic;

namespace ToirProf.Desktop.Models;

public class LoginRequestDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = new();
}

public class UserProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Patronymic { get; set; }
    public string FullName { get; set; } = "";
    public int RoleId { get; set; }
    public string RoleCode { get; set; } = "";
    public string RoleName { get; set; } = "";
    public int? WorkshopId { get; set; }
    public string? WorkshopName { get; set; }

    public string Initials
    {
        get
        {
            var a = string.IsNullOrWhiteSpace(LastName) ? "" : LastName[..1].ToUpper();
            var b = string.IsNullOrWhiteSpace(FirstName) ? "" : FirstName[..1].ToUpper();
            var res = a + b;
            return string.IsNullOrEmpty(res) ? "U" : res;
        }
    }
}

public class WorkshopDto
{
    public int WorkshopId { get; set; }
    public string WorkshopCode { get; set; } = "";
    public string WorkshopName { get; set; } = "";
    public string? WorkshopDescription { get; set; }
}

public class TeamDto
{
    public int TeamId { get; set; }
    public string TeamCode { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Specialization { get; set; } = "";
    public int? WorkshopId { get; set; }
    public string? WorkshopName { get; set; }
    public List<string> WorkshopNames { get; set; } = new();
    public List<TeamMemberDto> Members { get; set; } = new();
    public int ActiveOrdersCount { get; set; }
    public List<TeamActiveOrderDto> ActiveOrders { get; set; } = new();
    public string WorkshopsDisplay => WorkshopNames.Count > 0 ? string.Join(", ", WorkshopNames) : (WorkshopName ?? "—");
    public string WorkloadLabel => ActiveOrdersCount switch
    {
        0 => "Свободна (0)",
        <= 2 => $"Умеренная ({ActiveOrdersCount})",
        _ => $"Высокая ({ActiveOrdersCount})"
    };
}

public class TeamMemberDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public bool IsTeamLeader { get; set; }
    public string DisplayName => IsTeamLeader ? $"★ {FullName} (Бригадир)" : FullName;
}

public class TeamActiveOrderDto
{
    public int WorkOrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string EquipmentName { get; set; } = "";
    public string StatusName { get; set; } = "";
    public string PriorityName { get; set; } = "";
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
}

public class PagedResultDto<T>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<T> Items { get; set; } = new();
}
