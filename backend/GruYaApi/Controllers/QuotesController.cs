using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Responses;
using GruYaApi.Filters;
using GruYaApi.Models;
using GruYaApi.Service;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GruYaApi.Controllers
{
    [ApiController]
    [Route("api/quotes")]
    [Authorize]
    [ServiceFilter(typeof(UserExists))]
    public class QuotesController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly INotificationService? _notificationService;
        private static readonly TimeSpan ExpirationWindow = TimeSpan.FromHours(1);

        public QuotesController(DataContext context, INotificationService? notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private static bool IsExpired(Quote quote)
        {
            return quote.Status == QuoteStatus.Pendiente
                && DateTime.UtcNow - quote.CreatedAt > ExpirationWindow;
        }

        private async Task<List<int>> GetProviderProfileIdsAsync(int userId)
        {
            return await _context
                .ProviderProfiles.Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Persist lazy expiration for pending quotes older than 1h.
        /// Call before any query that filters by status on the DB side.
        /// </summary>
        private async Task ExpireStaleQuotesAsync(IQueryable<Quote> baseQuery)
        {
            var cutoff = DateTime.UtcNow - ExpirationWindow;
            var stale = await baseQuery
                .Where(q => q.Status == QuoteStatus.Pendiente && q.CreatedAt < cutoff)
                .ToListAsync();

            if (stale.Count == 0)
                return;

            foreach (var q in stale)
            {
                q.Status = QuoteStatus.Expirada;
                q.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        private async Task<List<QuoteResponse>> MapToResponseAsync(IQueryable<Quote> query)
        {
            var quotes = await query
                .Include(q => q.ProviderProfile)
                    .ThenInclude(pp => pp.User)
                .Include(q => q.Assistance)
                    .ThenInclude(a => a.Client)
                .Include(q => q.Assistance)
                    .ThenInclude(a => a.Vehicle)
                .ToListAsync();

            return quotes
                .Select(q => new QuoteResponse
                {
                    Id = q.Id,
                    AssistanceId = q.AssistanceId,
                    Price = q.Price,
                    Status = q.Status,
                    CreatedAt = q.CreatedAt,
                    UpdatedAt = q.UpdatedAt,
                    ProviderName =
                        $"{q.ProviderProfile.User.FirstName} {q.ProviderProfile.User.LastName}",
                    Assistance = q.Assistance.Adapt<AssistanceResponse>(),
                })
                .ToList();
        }

        // POST /api/quotes — Create a quote (Provider only)
        [HttpPost]
        public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteRequest request)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var profileIds = await GetProviderProfileIdsAsync(userId);
            if (profileIds.Count == 0)
                return Forbid();

            if (request.Price <= 0)
                return BadRequest(new { Message = "El precio debe ser mayor a 0" });

            var assistance = await _context.Assistances.FirstOrDefaultAsync(a =>
                a.Id == request.AssistanceId
            );
            if (assistance == null)
                return NotFound(new { Message = "Solicitud de auxilio no encontrada" });

            if (assistance.Status != AssistanceStatus.Pendiente)
                return BadRequest(
                    new { Message = "La solicitud no está abierta para cotizaciones" }
                );

            // Check directed request — only the targeted provider may quote
            if (
                assistance.RequestedProviderProfileId.HasValue
                && !profileIds.Contains(assistance.RequestedProviderProfileId.Value)
            )
                return Forbid();

            var providerProfileId = profileIds.First();

            // Expire stale quotes before checking for duplicate active quote
            var myQuoteQuery = _context.Quotes.Where(q =>
                q.AssistanceId == request.AssistanceId
                && q.ProviderProfileId == providerProfileId);
            await ExpireStaleQuotesAsync(myQuoteQuery);

            // Check duplicate active quote
            var hasActiveQuote = await _context.Quotes.AnyAsync(q =>
                q.AssistanceId == request.AssistanceId
                && q.ProviderProfileId == providerProfileId
                && q.Status == QuoteStatus.Pendiente
            );

            if (hasActiveQuote)
                return Conflict(
                    new { Message = "Ya tienes una cotización pendiente para esta solicitud" }
                );

            var quote = request.Adapt<Quote>();
            quote.ProviderProfileId = providerProfileId;
            quote.Status = QuoteStatus.Pendiente;
            quote.CreatedAt = DateTime.UtcNow;
            quote.UpdatedAt = DateTime.UtcNow;

            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            var response = (
                await MapToResponseAsync(_context.Quotes.Where(q => q.Id == quote.Id))
            ).First();

            // Notify client of new quote (NotificationService never throws)
            if (_notificationService is not null)
            {
                await _notificationService.SendToUserAsync(
                    assistance.ClientId,
                    "Recibiste una cotización",
                    $"{response.ProviderName} cotizó ${request.Price}",
                    new Dictionary<string, string>
                    {
                        ["type"] = "new_quote",
                        ["assistanceId"] = assistance.Id.ToString(),
                        ["quoteId"] = quote.Id.ToString(),
                        ["providerName"] = response.ProviderName,
                        ["price"] = request.Price.ToString(),
                    });
            }

            return CreatedAtAction(nameof(CreateQuote), new { id = quote.Id }, response);
        }

        // GET /api/quotes/mine — List caller's quotes, optional ?status= filter (multi-value)
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyQuotes([FromQuery] List<QuoteStatus>? status)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;
            var profileIds = await GetProviderProfileIdsAsync(userId);

            var baseQuery = _context.Quotes.Where(q => profileIds.Contains(q.ProviderProfileId));
            await ExpireStaleQuotesAsync(baseQuery);

            var query = baseQuery;
            if (status?.Count > 0)
                query = query.Where(q => status.Contains(q.Status));

            query = query.OrderByDescending(q => q.CreatedAt);

            return Ok(await MapToResponseAsync(query));
        }

        // GET /api/quotes/by-assistance/{assistanceId} — List quotes for an assistance (owner only)
        [HttpGet("by-assistance/{assistanceId}")]
        public async Task<IActionResult> GetQuotesByAssistance(int assistanceId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var assistance = await _context
                .Assistances.Include(a => a.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assistanceId);

            if (assistance == null)
                return NotFound(new { Message = "Solicitud de auxilio no encontrada" });

            if (assistance.Client.Id != userId)
                return Forbid();

            var query = _context
                .Quotes.Where(q => q.AssistanceId == assistanceId)
                .OrderByDescending(q => q.CreatedAt);

            return Ok(await MapToResponseAsync(query));
        }

        // GET /api/quotes/requests-for-me — List assistances available for caller to quote
        [HttpGet("requests-for-me")]
        public async Task<IActionResult> GetRequestsForMe()
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;
            var profileIds = await GetProviderProfileIdsAsync(userId);

            if (profileIds.Count == 0)
                return Forbid();

            // Expire stale quotes before checking for pending ones
            var myQuotesQuery = _context.Quotes.Where(q => profileIds.Contains(q.ProviderProfileId));
            await ExpireStaleQuotesAsync(myQuotesQuery);

            // Open assistances where caller has no pending quote from any of their profiles
            var openAssistances = await _context
                .Assistances.Include(a => a.Client)
                .Include(a => a.Vehicle)
                .Where(a =>
                    a.Status == AssistanceStatus.Pendiente
                    && a.RequestedProviderProfileId == null
                    && !_context.Quotes.Any(q =>
                        q.AssistanceId == a.Id
                        && profileIds.Contains(q.ProviderProfileId)
                        && q.Status == QuoteStatus.Pendiente
                    )
                )
                .ToListAsync();

            // Directed assistances (always shown, regardless of existing quotes)
            var directedAssistances = await _context
                .Assistances.Include(a => a.Client)
                .Include(a => a.Vehicle)
                .Where(a =>
                    a.Status == AssistanceStatus.Pendiente
                    && a.RequestedProviderProfileId != null
                    && profileIds.Contains(a.RequestedProviderProfileId.Value)
                )
                .ToListAsync();

            var result = openAssistances
                .Select(a =>
                {
                    var resp = a.Adapt<AssistanceResponse>();
                    resp.IsDirected = false;
                    return resp;
                })
                .Concat(
                    directedAssistances.Select(a =>
                    {
                        var resp = a.Adapt<AssistanceResponse>();
                        resp.IsDirected = true;
                        return resp;
                    })
                )
                .OrderByDescending(r => r.Id)
                .ToList();

            return Ok(result);
        }

        // PUT /api/quotes/{quoteId}/accept — Accept a pending quote (assistance owner)
        [HttpPut("{quoteId}/accept")]
        public async Task<IActionResult> AcceptQuote(int quoteId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var quote = await _context
                .Quotes.Include(q => q.Assistance)
                    .ThenInclude(a => a.Client)
                .Include(q => q.ProviderProfile)
                    .ThenInclude(pp => pp.User)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quote == null)
                return NotFound(new { Message = "Cotización no encontrada" });

            // Caller must own the assistance
            if (quote.Assistance.Client.Id != userId)
                return Forbid();

            // Provider cannot accept own quote
            var profileIds = await GetProviderProfileIdsAsync(userId);
            if (profileIds.Contains(quote.ProviderProfileId))
                return Forbid();

            // Lazy expiration check — expire and persist if needed
            if (IsExpired(quote))
            {
                quote.Status = QuoteStatus.Expirada;
                quote.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Conflict(new { Message = "La cotización ha expirado" });
            }

            if (quote.Status != QuoteStatus.Pendiente)
                return Conflict(
                    new
                    {
                        Message = $"La cotización no está pendiente. Estado actual: {quote.Status}",
                    }
                );

            // Transactional accept
            quote.Status = QuoteStatus.Aceptada;
            quote.UpdatedAt = DateTime.UtcNow;
            quote.Assistance.Provider = quote.ProviderProfile.User;
            quote.Assistance.Status = AssistanceStatus.EnProceso;

            // Auto-reject other pending quotes for the same assistance
            var otherPending = await _context
                .Quotes.Where(q =>
                    q.AssistanceId == quote.AssistanceId
                    && q.Id != quoteId
                    && q.Status == QuoteStatus.Pendiente
                )
                .ToListAsync();

            foreach (var other in otherPending)
            {
                other.Status = QuoteStatus.Rechazada;
                other.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Notify provider and client (NotificationService never throws)
            if (_notificationService is not null)
            {
                var companyName = quote.ProviderProfile.CompanyName ?? quote.ProviderProfile.User.FirstName;

                // Notify winning provider
                await _notificationService.SendToUserAsync(
                    quote.ProviderProfile.UserId,
                    "¡Servicio asignado!",
                    "Tu cotización fue aceptada",
                    new Dictionary<string, string>
                    {
                        ["type"] = "quote_accepted_provider",
                        ["assistanceId"] = quote.AssistanceId.ToString(),
                        ["providerProfileId"] = quote.ProviderProfileId.ToString(),
                    });

                // Notify client (confirmation)
                await _notificationService.SendToUserAsync(
                    quote.Assistance.Client.Id,
                    "Tu solicitud está siendo atendida",
                    $"{companyName} está en camino",
                    new Dictionary<string, string>
                    {
                        ["type"] = "quote_accepted_client",
                        ["assistanceId"] = quote.AssistanceId.ToString(),
                    });
            }

            var response = (
                await MapToResponseAsync(_context.Quotes.Where(q => q.Id == quoteId))
            ).First();

            return Ok(response);
        }

        // PUT /api/quotes/{quoteId}/reject — Reject a pending quote (assistance owner)
        [HttpPut("{quoteId}/reject")]
        public async Task<IActionResult> RejectQuote(int quoteId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var quote = await _context
                .Quotes.Include(q => q.Assistance)
                    .ThenInclude(a => a.Client)
                .Include(q => q.ProviderProfile)
                    .ThenInclude(pp => pp.User)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quote == null)
                return NotFound(new { Message = "Cotización no encontrada" });

            // Caller must own the assistance
            if (quote.Assistance.Client.Id != userId)
                return Forbid();

            // Lazy expiration check
            if (IsExpired(quote))
            {
                quote.Status = QuoteStatus.Expirada;
                quote.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Conflict(new { Message = "La cotización ha expirado" });
            }

            if (quote.Status != QuoteStatus.Pendiente)
                return Conflict(
                    new
                    {
                        Message = $"La cotización no está pendiente. Estado actual: {quote.Status}",
                    }
                );

            quote.Status = QuoteStatus.Rechazada;
            quote.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notify rejected provider (NotificationService never throws)
            if (_notificationService is not null)
            {
                await _notificationService.SendToUserAsync(
                    quote.ProviderProfile.UserId,
                    "Cotización rechazada",
                    "Tu cotización fue rechazada",
                    new Dictionary<string, string>
                    {
                        ["type"] = "quote_rejected",
                        ["assistanceId"] = quote.AssistanceId.ToString(),
                    });
            }

            var response = (
                await MapToResponseAsync(_context.Quotes.Where(q => q.Id == quoteId))
            ).First();

            return Ok(response);
        }

        // PUT /api/quotes/{quoteId}/cancel — Cancel own pending quote (provider)
        [HttpPut("{quoteId}/cancel")]
        public async Task<IActionResult> CancelQuote(int quoteId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var quote = await _context
                .Quotes.Include(q => q.ProviderProfile)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quote == null)
                return NotFound(new { Message = "Cotización no encontrada" });

            // Caller must be the quote creator (any of their profiles)
            var profileIds = await GetProviderProfileIdsAsync(userId);
            if (!profileIds.Contains(quote.ProviderProfileId))
                return Forbid();

            // Lazy expiration check
            if (IsExpired(quote))
            {
                quote.Status = QuoteStatus.Expirada;
                quote.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Conflict(new { Message = "La cotización ha expirado" });
            }

            if (quote.Status != QuoteStatus.Pendiente)
                return Conflict(
                    new
                    {
                        Message = $"La cotización no está pendiente. Estado actual: {quote.Status}",
                    }
                );

            quote.Status = QuoteStatus.Cancelada;
            quote.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = (
                await MapToResponseAsync(_context.Quotes.Where(q => q.Id == quoteId))
            ).First();

            return Ok(response);
        }
    }
}
