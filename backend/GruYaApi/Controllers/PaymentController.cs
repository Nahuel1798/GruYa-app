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
        // Crea un pago asociado a una asistencia. Solo lo puede crear el cliente propietario de la asistencia.
        [HttpPost("{assistanceId}")]
        public async Task<IActionResult> CreatePayment(int assistanceId, [FromBody] CreatePaymentRequest request)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var assistance = await _context.Assistances
                .Include(a => a.Payment)
                .FirstOrDefaultAsync(a => a.Id == assistanceId);

            if (assistance == null)
                return NotFound(new { Message = "Asistencia no encontrada" });

            if (assistance.ClientId != userId)
                return Forbid();

            if (assistance.Payment != null)
                return Conflict(new { Message = "La asistencia ya tiene un pago registrado" });

            var payment = new Payment
            {
                Amount = request.Amount,
                Method = request.Method,
                Status = PaymentStatus.Pagado,
                AssistanceId = assistanceId,
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(payment.Adapt<PaymentResponse>());
        }

        // GET: api/payments/{id}
        // Devuelve un pago por su id. Solo el cliente propietario puede consultarlo.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var payment = await _context.Payments
                .Include(p => p.Assistance)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            if (payment.Assistance.ClientId != userId)
                return Forbid();

            return Ok(payment.Adapt<PaymentResponse>());
        }

        // GET: api/payments/assistance/{assistanceId}
        // Devuelve el pago asociado a una asistencia concreta. Solo el cliente propietario puede consultarlo.
        [HttpGet("assistance/{assistanceId}")]
        public async Task<IActionResult> GetPaymentByAssistance(int assistanceId)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var payment = await _context.Payments
                .Include(p => p.Assistance)
                .FirstOrDefaultAsync(p => p.AssistanceId == assistanceId);

            if (payment == null)
                return NotFound();

            if (payment.Assistance.ClientId != userId)
                return Forbid();

            return Ok(payment.Adapt<PaymentResponse>());
        }

        // GET: api/payments
        // Lista los pagos del cliente autenticado.
        [HttpGet]
        public async Task<IActionResult> GetMyPayments()
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var payments = await _context.Payments
                .Include(p => p.Assistance)
                .Where(p => p.Assistance.ClientId == userId)
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            return Ok(payments.Adapt<List<PaymentResponse>>());
        }
    }
}
