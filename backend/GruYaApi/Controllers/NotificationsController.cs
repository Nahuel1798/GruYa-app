using GruYaApi.Data;
using GruYaApi.DTOs.Responses;
using GruYaApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GruYaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ServiceFilter(typeof(UserExists))]
public class NotificationsController : ControllerBase
{
    private readonly DataContext _context;

    public NotificationsController(DataContext context)
    {
        _context = context;
    }

    // GET: api/notifications
    // Devuelve las notificaciones del usuario autenticado, ordenadas por fecha descendente.
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var userId = (int)HttpContext.Items["idUsuario"]!;

        var query = _context
            .Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.SentAt)
            .AsNoTracking();

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                DataJson = n.DataJson,
                SentAt = n.SentAt,
                ReadAt = n.ReadAt,
                AssistanceId = n.AssistanceId,
            })
            .ToListAsync();

        return Ok(new PagedResponse<NotificationResponse>
        {
            Data = notifications,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages,
        });
    }

    // PATCH: api/notifications/{id}/read
    // Marca una notificación como leída.
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = (int)HttpContext.Items["idUsuario"]!;

        var notification = await _context.Notifications.FirstOrDefaultAsync(n =>
            n.Id == id && n.UserId == userId
        );

        if (notification == null)
            return NotFound(new { Message = "Notificación no encontrada" });

        if (notification.ReadAt == null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Body = notification.Body,
            DataJson = notification.DataJson,
            SentAt = notification.SentAt,
            ReadAt = notification.ReadAt,
            AssistanceId = notification.AssistanceId,
        });
    }

    // PATCH: api/notifications/read-all
    // Marca todas las notificaciones no leídas del usuario como leídas.
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = (int)HttpContext.Items["idUsuario"]!;

        var unread = await _context
            .Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ToListAsync();

        if (unread.Count > 0)
        {
            foreach (var n in unread)
            {
                n.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        return Ok();
    }
}
