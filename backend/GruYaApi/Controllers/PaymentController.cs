using System.Security.Claims;
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
    [Route("api/[controller]")]
    [Authorize]
    [ServiceFilter(typeof(UserExists))]
    public class PaymentController : ControllerBase
    {
        private readonly DataContext _context;

        public PaymentController(DataContext context)
        {
            _context = context;
        }

        // POST: api/payments/{assistanceId}
        // Crea un pago asociado a una asistencia. Puede crearlo el cliente propietario o el proveedor asignado.
        [HttpPost("{assistanceId}")]
        public async Task<IActionResult> CreatePayment(int assistanceId, [FromBody] CreatePaymentRequest request)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!Enum.TryParse<Role>(roleClaim, out var userRole))
                return BadRequest(new { Message = "Rol de usuario inválido" });

            var assistance = await _context.Assistances
                .Include(a => a.Payment)
                .Include(a => a.Provider)
                .FirstOrDefaultAsync(a => a.Id == assistanceId);

            if (assistance == null)
                return NotFound(new { Message = "Asistencia no encontrada" });

            var isClient = assistance.ClientId == userId;
            var isProvider = assistance.Provider != null && assistance.Provider.Id == userId;

            if (!isClient && !isProvider)
                return Forbid();

            if (assistance.Payment != null)
                return Conflict(new { Message = "La asistencia ya tiene un pago registrado" });

            var allowedStatuses = new[]
            {
                AssistanceStatus.Aceptada,
                AssistanceStatus.EnCaminoAlCliente,
                AssistanceStatus.EnOrigen,
                AssistanceStatus.EnCaminoAlDestino,
            };

            if (!allowedStatuses.Contains(assistance.Status))
                return Conflict(new { Message = "El pago solo se puede registrar cuando la asistencia está en camino o aceptada" });

            var payment = new Payment
            {
                Amount = request.Amount,
                Method = request.Method,
                Status = PaymentStatus.Pagado,
                AssistanceId = assistanceId,
            };

            _context.Payments.Add(payment);

            if (assistance.Status == AssistanceStatus.EnCaminoAlDestino)
            {
                assistance.Status = AssistanceStatus.Completado;
                assistance.TrackingSessionId = null;

                var acceptedQuote = await _context.Quotes.FirstOrDefaultAsync(q =>
                    q.AssistanceId == assistance.Id && q.Status == QuoteStatus.Aceptada
                );

                if (acceptedQuote != null)
                {
                    acceptedQuote.Status = QuoteStatus.Completado;
                    acceptedQuote.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(payment.Adapt<PaymentResponse>());
        }

        // GET: api/payments/{id}
        // Devuelve un pago por su id. Solo el cliente propietario o el proveedor asignado puede consultarlo.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!Enum.TryParse<Role>(roleClaim, out var userRole))
                return BadRequest(new { Message = "Rol de usuario inválido" });

            var payment = await _context.Payments
                .Include(p => p.Assistance)
                .ThenInclude(a => a.Provider)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            var isClient = payment.Assistance.ClientId == userId;
            var isProvider = payment.Assistance.Provider != null && payment.Assistance.Provider.Id == userId;

            if (!isClient && !isProvider)
                return Forbid();

            return Ok(payment.Adapt<PaymentResponse>());
        }

        // GET: api/payments/assistance/{assistanceId}
        // Devuelve el pago asociado a una asistencia concreta. Solo el cliente propietario o el proveedor asignado puede consultarlo.
        [HttpGet("assistance/{assistanceId}")]
        public async Task<IActionResult> GetPaymentByAssistance(int assistanceId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!Enum.TryParse<Role>(roleClaim, out var userRole))
                return BadRequest(new { Message = "Rol de usuario inválido" });

            var payment = await _context.Payments
                .Include(p => p.Assistance)
                .ThenInclude(a => a.Provider)
                .FirstOrDefaultAsync(p => p.AssistanceId == assistanceId);

            if (payment == null)
                return NotFound();

            var isClient = payment.Assistance.ClientId == userId;
            var isProvider = payment.Assistance.Provider != null && payment.Assistance.Provider.Id == userId;

            if (!isClient && !isProvider)
                return Forbid();

            return Ok(payment.Adapt<PaymentResponse>());
        }

        // GET: api/payments
        // Lista los pagos del usuario autenticado. El cliente ve sus pagos y el proveedor ve los pagos de sus asistencias.
        [HttpGet]
        public async Task<IActionResult> GetMyPayments()
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!Enum.TryParse<Role>(roleClaim, out var userRole))
                return BadRequest(new { Message = "Rol de usuario inválido" });

            var query = _context.Payments
                .Include(p => p.Assistance)
                .ThenInclude(a => a.Provider)
                .AsQueryable();

            if (userRole == Role.Provider)
            {
                query = query.Where(p => p.Assistance.Provider != null && p.Assistance.Provider.Id == userId);
            }
            else
            {
                query = query.Where(p => p.Assistance.ClientId == userId);
            }

            var payments = await query
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            return Ok(payments.Adapt<List<PaymentResponse>>());
        }
    }
}
