# NexaFlow — Data Model / ERD (T-002)

Multi-tenant SaaS. **كل جدول business بيحمل `TenantId`** وبيتفلتر تلقائياً بـ global query filter (شوف T-004).
الـ Identity tables بتاعة ASP.NET Core Identity مستخدمة كأساس للـ users/roles.

## ERD

```mermaid
erDiagram
    Tenant ||--o{ ApplicationUser : "has"
    Tenant ||--o{ TeamInvitation : "issues"
    Tenant ||--o{ ApplicationRole : "scopes (nullable)"
    ApplicationUser ||--o{ RefreshToken : "owns"
    ApplicationUser ||--o{ TeamInvitation : "invited_by"
    ApplicationUser }o--o{ ApplicationRole : "AspNetUserRoles"

    Tenant {
        guid Id PK
        string Name
        string Slug UK "unique, subdomain-safe"
        int Status "Active|Suspended|PendingSetup"
        int Plan "Free|Pro|Enterprise"
        datetime CreatedAt
        datetime UpdatedAt
    }

    ApplicationUser {
        guid Id PK
        guid TenantId FK
        string Email "Identity"
        string UserName "Identity"
        string PasswordHash "Identity hasher"
        string FirstName
        string LastName
        bool IsActive
        datetime CreatedAt
        datetime LastLoginAt
    }

    ApplicationRole {
        guid Id PK
        guid TenantId FK "nullable = system role"
        string Name "Identity"
    }

    RefreshToken {
        guid Id PK
        guid UserId FK
        string Token UK
        datetime ExpiresAt
        datetime CreatedAt
        string CreatedByIp
        datetime RevokedAt "nullable"
        string ReplacedByToken "nullable"
    }

    TeamInvitation {
        guid Id PK
        guid TenantId FK
        string Email
        string RoleName
        string Token UK
        int Status "Pending|Accepted|Expired|Revoked"
        datetime ExpiresAt
        guid InvitedByUserId FK
        datetime CreatedAt
        datetime AcceptedAt "nullable"
    }
```

## Indexes
| Table | Index | Reason |
|---|---|---|
| Tenant | `UX_Tenant_Slug` (unique) | lookup بالـ subdomain |
| ApplicationUser | `IX_User_TenantId` | فلترة per-tenant |
| ApplicationUser | `UX_User_TenantId_Email` (unique) | إيميل فريد داخل الـ tenant |
| RefreshToken | `UX_RefreshToken_Token` (unique) | lookup عند الـ refresh |
| RefreshToken | `IX_RefreshToken_UserId` | تنظيف tokens المستخدم |
| TeamInvitation | `UX_Invitation_Token` (unique) | accept link |
| TeamInvitation | `IX_Invitation_TenantId_Email` | منع دعوات مكررة |

## Roles (seeded)
| Role | Scope | Notes |
|---|---|---|
| `SuperAdmin` | System (TenantId = null) | بيدير المنصة كلها |
| `CompanyAdmin` | Tenant | أول user عند الـ onboarding |
| `Manager` | Tenant | |
| `Employee` | Tenant | افتراضي للـ invited users |

## Multi-tenant strategy
- **Shared database, shared schema** + discriminator column `TenantId`.
- العزل بيتفرض على مستوى الـ `DbContext` (global query filter) مش الـ query — مفيش query بتنسى الفلتر.
- الـ `TenantId` بييجي من الـ JWT claim → `ITenantContext` → الـ DbContext.
