using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Responses;
using GruYaApi.Filters;
using GruYaApi.Models;
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
        private static readonly TimeSpan ExpirationWindow = TimeSpan.FromHours(1);

        public QuotesController(DataContext context)
        {
            _context = context;
        }

        private static bool IsExpired(Quote quote)
        {
            return quote.Status == QuoteStatus.Pendiente
                && DateTime.UtcNow - quote.CreatedAt > ExpirationWindow;
        }

        private async Task<List<QuoteResponse>> MapToResponseAsync(IQueryable<Quote> query)
        {
            var quotes = await query
                .Include(q => q.Provider)
                .Include(q => q.Assistance)
                    .ThenInclude(a => a.Client)
                .Include(q => q.Assistance)
                    .ThenInclude(a => a.Vehicle)
                .ToListAsync();

            return quotes.Select(q => new QuoteResponse
            {
                Id = q.Id,
                AssistanceId = q.AssistanceId,
                Price = q.Price,
                Status = IsExpired(q) ? QuoteStatus.Expirada : q.Status,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                ProviderName = $"{q.Provider.FirstName} {q.Provider.LastName}",
                Assistance = q.Assistance.Adapt<AssistanceResponse>(),
            }).ToList();
        }

        // POST /api/quotes — Create a quote (Provider only)
        [HttpPost]
        public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteRequest request)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.Role != Role.Provider)
                return Forbid();

            if (request.Price <= 0)
                return BadRequest(new { Message = "El precio debe ser mayor a 0" });

            var assistance = await _context.Assistances.FirstOrDefaultAsync(a => a.Id == request.AssistanceId);
            if (assistance == null)
                return NotFound(new { Message = "Solicitud de auxilio no encontrada" });

            if (assistance.Status != AssistanceStatus.Pendiente)
                return BadRequest(new { Message = "La solicitud no está abierta para cotizaciones" });

            // Check directed request — only the targeted provider may quote
            if (assistance.RequestedProviderId.HasValue && assistance.RequestedProviderId.Value != userId)
                return Forbid();

            // Check duplicate active quote
            var hasActiveQuote = await _context.Quotes.AnyAsync(q =>
                q.AssistanceId == request.AssistanceId
                && q.ProviderId == userId
                && q.Status == QuoteStatus.Pendiente);

            if (hasActiveQuote)
                return Conflict(new { Message = "Ya tienes una cotización pendiente para esta solicitud" });

            var quote = request.Adapt<Quote>();
            quote.ProviderId = userId;
            quote.Status = QuoteStatus.Pendiente;
            quote.CreatedAt = DateTime.UtcNow;
            quote.UpdatedAt = DateTime.UtcNow;

            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            var response = (await MapToResponseAsync(
                _context.Quotes.Where(q => q.Id == quote.Id))).First();

            return CreatedAtAction(nameof(CreateQuote), new { id = quote.Id }, response);
        }

        // GET /api/quotes/mine — List caller's quotes, optional ?status= filter
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyQuotes([FromQuery] QuoteStatus? status)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var query = _context.Quotes.Where(q => q.ProviderId == userId);

            if (status.HasValue)
                query = query.Where(q => q.Status == status.Value);

            query = query.OrderByDescending(q => q.CreatedAt);

            return Ok(await MapToResponseAsync(query));
        }

        // GET /api/quotes/by-assistance/{assistanceId} — List quotes for an assistance (owner only)
        [HttpGet("by-assistance/{assistanceId}")]
        public async Task<IActionResult> GetQuotesByAssistance(int assistanceId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var assistance = await _context.Assistances
                .Include(a => a.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assistanceId);

            if (assistance == null)
                return NotFound(new { Message = "Solicitud de auxilio no encontrada" });

            if (assistance.Client.Id != userId)
                return Forbid();

            var query = _context.Quotes
                .Where(q => q.AssistanceId == assistanceId)
                .OrderByDescending(q => q.CreatedAt);

            return Ok(await MapToResponseAsync(query));
        }

        // GET /api/quotes/requests-for-me — List assistances available for caller to quote
        [HttpGet("requests-for-me")]
        public async Task<IActionResult> GetRequestsForMe()
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.Role != Role.Provider)
                return Forbid();

            // Open assistances where caller has no pending quote
            var openAssistances = await _context.Assistances
                .Include(a => a.Client)
                .Include(a => a.Vehicle)
                .Where(a => a.Status == AssistanceStatus.Pendiente
                    && a.RequestedProviderId == null
                    && !_context.Quotes.Any(q =>
                        q.AssistanceId == a.Id
                        && q.ProviderId == userId
                        && q.Status == QuoteStatus.Pendiente))
                .ToListAsync();

            // Directed assistances (always shown, regardless of existing quotes)
            var directedAssistances = await _context.Assistances
                .Include(a => a.Client)
                .Include(a => a.Vehicle)
                .Where(a => a.Status == AssistanceStatus.Pendiente
                    && a.RequestedProviderId == userId)
                .ToListAsync();

            var result = openAssistances
                .Select(a => new
                {
                    a.Id,
                    a.ServiceType,
                    a.IssueType,
                    a.Status,
                    ClientName = $"{a.Client.FirstName} {a.Client.LastName}",
                    OriginLatitude = a.Origin.Latitude,
                    OriginLongitude = a.Origin.Longitude,
                    DestinationLatitude = a.Destination.Latitude,
                    DestinationLongitude = a.Destination.Longitude,
                    VehicleBrand = a.Vehicle != null ? a.Vehicle.Brand : null,
                    VehicleModel = a.Vehicle != null ? a.Vehicle.Model : null,
                    IsDirected = false,
                })
                .Concat(directedAssistances.Select(a => new
                {
                    a.Id,
                    a.ServiceType,
                    a.IssueType,
                    a.Status,
                    ClientName = $"{a.Client.FirstName} {a.Client.LastName}",
                    OriginLatitude = a.Origin.Latitude,
                    OriginLongitude = a.Origin.Longitude,
                    DestinationLatitude = a.Destination.Latitude,
                    DestinationLongitude = a.Destination.Longitude,
                    VehicleBrand = a.Vehicle != null ? a.Vehicle.Brand : null,
                    VehicleModel = a.Vehicle != null ? a.Vehicle.Model : null,
                    IsDirected = true,
                }))
                .OrderByDescending(r => r.Id)
                .ToList();

            return Ok(result);
        }

        // PUT /api/quotes/{quoteId}/accept — Accept a pending quote (assistance owner)
        [HttpPut("{quoteId}/accept")]
        public async Task<IActionResult> AcceptQuote(int quoteId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var quote = await _context.Quotes
                .Include(q => q.Assistance)
                    .ThenInclude(a => a.Client)
                .Include(q => q.Provider)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quote == null)
                return NotFound(new { Message = "Cotización no encontrada" });

            // Caller must own the assistance
            if (quote.Assistance.Client.Id != userId)
                return Forbid();

            // Provider cannot accept own quote
            if (quote.ProviderId == userId)
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
                return Conflict(new { Message = $"La cotización no está pendiente. Estado actual: {quote.Status}" });

            // Transactional accept
            quote.Status = QuoteStatus.Aceptada;
            quote.UpdatedAt = DateTime.UtcNow;
            quote.Assistance.Provider = quote.Provider;
            quote.Assistance.Status = AssistanceStatus.EnProceso;

            // Auto-reject other pending quotes for the same assistance
            var otherPending = await _context.Quotes
                .Where(q => q.AssistanceId == quote.AssistanceId
                    && q.Id != quoteId
                    && q.Status == QuoteStatus.Pendiente)
                .ToListAsync();

            foreach (var other in otherPending)
            {
                other.Status = QuoteStatus.Rechazada;
                other.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var response = (await MapToResponseAsync(
                _context.Quotes.Where(q => q.Id == quoteId))).First();

            return Ok(response);
        }

        // PUT /api/quotes/{quoteId}/reject — Reject a pending quote (assistance owner)
        [HttpPut("{quoteId}/reject")]
        public async Task<IActionResult> RejectQuote(int quoteId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var quote = await _context.Quotes
                .Include(q => q.Assistance)
                    .ThenInclude(a => a.Client)
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
                return Conflict(new { Message = $"La cotización no está pendiente. Estado actual: {quote.Status}" });

            quote.Status = QuoteStatus.Rechazada;
            quote.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = (await MapToResponseAsync(
                _context.Quotes.Where(q => q.Id == quoteId))).First();

            return Ok(response);
        }

        // PUT /api/quotes/{quoteId}/cancel — Cancel own pending quote (provider)
        [HttpPut("{quoteId}/cancel")]
        public async Task<IActionResult> CancelQuote(int quoteId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var quote = await _context.Quotes
                .Include(q => q.Provider)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quote == null)
                return NotFound(new { Message = "Cotización no encontrada" });

            // Caller must be the quote creator
            if (quote.ProviderId != userId)
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
                return Conflict(new { Message = $"La cotización no está pendiente. Estado actual: {quote.Status}" });

            quote.Status = QuoteStatus.Cancelada;
            quote.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = (await MapToResponseAsync(
                _context.Quotes.Where(q => q.Id == quoteId))).First();

            return Ok(response);
        }
    }
}
