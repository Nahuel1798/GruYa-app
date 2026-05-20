using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public enum ServiceRequestStatus
    {
        Pendiente,
        EnProceso,
        Completado,
        Cancelado
    }
}