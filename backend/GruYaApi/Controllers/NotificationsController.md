# NotificationsController

`/api/notifications` — requiere JWT + filtro `UserExists`

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/notifications?page=1&pageSize=20` | Notificaciones del usuario autenticado, paginadas, ordenadas por `SentAt` desc. Máximo `pageSize` 100. |
| `PATCH` | `/api/notifications/{id}/read` | Marca una como leída. Devuelve la notificación actualizada. `404` si no existe o no pertenece al usuario. |
| `PATCH` | `/api/notifications/read-all` | Marca **todas** las no leídas como leídas. `200` sin body siempre. |

## Response del GET

```jsonc
{
  "data": [
    {
      "id": 1,
      "type": "new_assistance",
      "title": "Nueva solicitud de auxilio",
      "body": "Un conductor cerca tuyo necesita ayuda",
      "dataJson": "{\"type\":\"new_assistance\",\"title\":\"Nueva solicitud...\",\"body\":\"Tipo: ...\",\"assistanceId\":42,\"serviceType\":\"Auxilio\",\"issueType\":\"NEUMATICO_PINCHADO\",\"originLat\":-33.3,\"originLon\":-66.3}",
      "sentAt": "2026-07-02T15:00:00Z",
      "readAt": null,
      "assistanceId": 42
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 42,
  "totalPages": 3
}
```

## dataJson — campos por tipo de notificación

`dataJson` es un JSON string con la serialización completa de `NotificationPayload`. Los campos comunes a todos los tipos son:

| Campo | Tipo | Siempre presente |
|---|---|---|
| `type` | string | sí |
| `title` | string | sí |
| `body` | string | sí |
| `assistanceId` | int | sí |

Campos adicionales **según el `type`**:

| `type` | Campos extra | Quién lo recibe |
|---|---|---|
| `directed_assistance` | `serviceType`, `issueType` | Proveedor (solicitud dirigida) |
| `new_assistance` | `serviceType`, `issueType`, `originLat`, `originLon` | Proveedores cercanos (broadcast) |
| `trip_started` | `providerId`, `trackingSessionId` | Cliente |
| `provider.arrived` | `providerId` | Cliente |
| `provider.heading_to_destination` | `providerId` | Cliente |
| `provider.service_completed` | `providerId` | Cliente |
| `new_quote` | `quoteId`, `providerName`, `price` | Cliente |
| `quote_accepted_provider` | `providerProfileId` | Proveedor (su cotización fue aceptada) |
| `quote_accepted_client` | `providerName` | Cliente (confirmación) |
| `quote_rejected` | *(ninguno extra)* | Proveedor (su cotización fue rechazada) |

### Tipos de los campos extra

```csharp
int? ProviderId
int? ProviderProfileId
string? TrackingSessionId
int? QuoteId
string? ProviderName
decimal? Price
string? ServiceType
string? IssueType
decimal? OriginLat
decimal? OriginLon
```

## DTOs

- `NotificationResponse` — `DTOs/Responses/NotificationResponse.cs`
- `PagedResponse<T>` — `DTOs/Responses/PagedResponse.cs` (genérico reutilizable)
