using PDV.Application.DTOs;
using PDV.Domain.Enums;

namespace PDV.WPF.Helpers;

/// <summary>
/// Holds the currently authenticated user and exposes centralized permission helpers.
/// All permission checks in the entire WPF layer must go through this class.
/// </summary>
public static class SessionManager
{
    public static UserDto? CurrentUser { get; private set; }

    public static void Login(UserDto user)  => CurrentUser = user;
    public static void Logout()             => CurrentUser = null;

    public static bool IsLoggedIn => CurrentUser != null;

    // ── Role checks ────────────────────────────────────────────────────────
    public static UserRole Role => CurrentUser?.Role ?? UserRole.Operador;

    /// <summary>Administrador only.</summary>
    public static bool IsAdmin   => Role == UserRole.Administrador;

    /// <summary>Administrador OR Gerente.</summary>
    public static bool IsManager => Role <= UserRole.Gerente;   // enum: Admin=0, Gerente=1

    // ── Navigation permissions ─────────────────────────────────────────────
    /// <summary>All roles can access sales.</summary>
    public static bool CanAccessSales       => IsLoggedIn;

    /// <summary>Gerente and above can access product/customer/supplier/stock/cash/reports.</summary>
    public static bool CanAccessManagement  => IsManager;

    /// <summary>Only Administrador can manage users.</summary>
    public static bool CanAccessUsers       => IsAdmin;

    // ── In-screen permissions ──────────────────────────────────────────────
    /// <summary>Gerente+ can apply or edit discounts on sales.</summary>
    public static bool CanApplyDiscount     => IsManager;

    /// <summary>Gerente+ can override the unit price of a sale item (preço manual).</summary>
    public static bool CanEditPrice          => IsManager;

    /// <summary>Gerente+ can create and edit products.</summary>
    public static bool CanEditProducts      => IsManager;

    /// <summary>Only Administrador can permanently delete products.</summary>
    public static bool CanDeleteProducts    => IsAdmin;

    /// <summary>Gerente+ can create and edit customers/suppliers.</summary>
    public static bool CanEditContacts      => IsManager;

    /// <summary>Human-readable role label for display in UI.</summary>
    public static string RoleLabel => Role switch
    {
        UserRole.Administrador => "Administrador",
        UserRole.Gerente       => "Gerente",
        _                      => "Operador"
    };
}
