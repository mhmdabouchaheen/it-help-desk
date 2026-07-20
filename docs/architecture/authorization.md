# Authorization model

The API uses ASP.NET Core policy-based authorization over role claims from validated JWT bearer access tokens. Role names are defined centrally in `AppRoles`; controllers and future application features should not duplicate role strings.

## Seeded roles

The exact Identity role names are:

- `Admin`
- `IT Support Agent`
- `Employee`
- `Manager`

`AppRoles.All` contains all four roles. `AppRoles.SupportStaff` contains Admin and IT Support Agent. `AppRoles.Management` contains Admin and Manager. These collections are read-only.

## Registered policies

| Policy | Requirement |
|---|---|
| `AuthenticatedUser` | Authenticated principal |
| `AdminOnly` | Admin |
| `SupportStaff` | Admin or IT Support Agent |
| `Management` | Admin or Manager |

Policy names are defined in `AppPolicies`. Role claims originate from the user's Identity roles when the access-token service creates a JWT. JWT bearer validation uses `ClaimTypes.Role`, and authorization trusts roles only after token signature, issuer, audience, and lifetime validation.

## Scope and future ticket authorization

These policies provide coarse role-based access only. Ticket authorization will also require resource-specific ownership, assignment, and status checks. Those rules belong in application services or dedicated resource authorization handlers, not duplicated in controllers and not represented by role strings alone.

`AuthorizationProbeController` is a temporary internal verification surface with no injected services or business logic. It is ignored by API Explorer so it does not appear in production OpenAPI documentation.
