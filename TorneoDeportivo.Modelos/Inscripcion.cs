using System.ComponentModel.DataAnnotations;

namespace TorneoDeportivo.Modelos;
public class Inscripcion
{
    [Key] public int Id { get; set; }
    
    // FKs
    public int TorneoId { get; set; }
    public Torneo Torneo { get; set; } = null!;

    public int EquipoId { get; set; }
    public Equipo Equipo { get; set; } = null!;

    public DateTime FechaInscripcion { get; set; } = DateTime.UtcNow;
}