using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ToirProf.Domain;

namespace ToirProf.Api;

public static class JwtHelper
{
    public static int? GetUserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("user_id")?.Value, out var id) ? id : null;

    public static string HashPassword(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();

    public static bool VerifyPassword(string input, string storedHash) =>
        !string.IsNullOrEmpty(storedHash) && string.Equals(HashPassword(input), storedHash, StringComparison.OrdinalIgnoreCase);

    public static (string Token, DateTime ExpiresAt) CreateToken(User user, IConfiguration cfg)
    {
        var key = cfg["Jwt:Key"] ?? "SuperSecretToirProfKey2027ForJwtValidationMustBe32CharsLong!";
        var mins = int.TryParse(cfg["Jwt:DurationInMinutes"], out var m) ? m : 480;
        var expiresAt = DateTime.UtcNow.AddMinutes(mins);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.LastName} {user.FirstName}".Trim()),
            new(ClaimTypes.Role, user.Role?.RoleCode ?? "operator"),
            new("user_id", user.UserId.ToString()),
            new("role_code", user.Role?.RoleCode ?? "operator")
        };
        if (user.WorkshopId.HasValue) claims.Add(new("workshop_id", user.WorkshopId.Value.ToString()));

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(cfg["Jwt:Issuer"] ?? "ToirProfApi", cfg["Jwt:Audience"] ?? "ToirProfClients", claims, expires: expiresAt, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }
}

// Request records
public record LoginReq(string Email, string Password, bool RememberMe = false);
public record CreateEquipmentReq(int WorkshopId, int StatusId, string InventoryNumber, string EquipmentName, string ModelName, string? SerialNumber, string? Manufacturer, DateOnly CommissioningDate, DateOnly OperationDate, DateOnly? WarrantyUntil, string? PassportUrl);
public record UpdateEquipmentReq(int WorkshopId, int StatusId, string EquipmentName, string ModelName, string? SerialNumber, string? Manufacturer, DateOnly CommissioningDate, DateOnly OperationDate, DateOnly? WarrantyUntil, string? PassportUrl, string? StatusChangeReason);
public record CreateWorkOrderReq(string OrderType, int PriorityId, int EquipmentId, int? ScheduleId, string Description, string? FailureReason, DateTime PlannedStartDate, DateTime PlannedEndDate);
public record AssignTeamReq(int TeamId);
public record UpdateOrderStatusReq(int OrderStatusId, DateTime? ActualStartDate, DateTime? ActualEndDate, string? ReportText, string? FailureReason, decimal? DowntimeHours);
public record AddWorkOrderMaterialReq(int? WarehouseId, int MaterialId, decimal QuantityUsed, string? CommentText);
public record CreateScheduleReq(int EquipmentId, int MaintenanceTypeId, int? AssignedTeamId, DateOnly PlannedDate);
public record GenerateOrderReq(int PriorityId = 2, string? Description = null);
public record AdjustStockReq(int? WarehouseId, decimal QuantityDelta, string? Reason = null);

// Compact DTOs with computed properties needed by Web/Desktop clients
public record UserProfileDto(int UserId, string Email, string Phone, string FirstName, string LastName, string? Patronymic, int RoleId, string RoleCode, string RoleName, int? WorkshopId, string? WorkshopName)
{
    public string FullName => $"{LastName} {FirstName} {Patronymic}".Trim();
}

public record WorkOrderMaterialDto(int WarehouseId, string WarehouseName, int MaterialId, string MaterialCode, string MaterialName, string UnitOfMeasure, decimal QuantityUsed, decimal HistoricalUnitPrice, DateTime UsedAt, string? CommentText)
{
    public decimal TotalCost => QuantityUsed * HistoricalUnitPrice;
}

public record MaterialItemDto(int MaterialId, int WarehouseId, string WarehouseName, string MaterialCode, string MaterialName, string UnitOfMeasure, decimal CurrentStock, decimal MinimumStock, string? StorageCell, decimal UnitPrice, string? SupplierName)
{
    public bool IsLowStock => CurrentStock <= MinimumStock;
}
