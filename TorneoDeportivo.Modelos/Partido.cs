using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoDeportivo.Modelos;
public class Partido
{
    [Key] public int Id { get; set; }

    public int TorneoId { get; set; }
    public Torneo Torneo { get; set; } = null!;

    // Claves foráneas para los equipos
    public int EquipoLocalId { get; set; }
    public Equipo EquipoLocal { get; set; } = null!;

    public int EquipoVisitanteId { get; set; }
    public Equipo EquipoVisitante { get; set; } = null!;

    public DateTime FechaProgramada { get; set; }
    public bool Jugado { get; set; } = false;

    // Resultados
    public int GolesLocal { get; set; } = 0;
    public int GolesVisitante { get; set; } = 0;
    
    // Para eliminación directa (si hay penales)
    public int? PenalesLocal { get; set; }
    public int? PenalesVisitante { get; set; }

    // Metadatos del partido
    public FasePartido Fase { get; set; }
    
    [MaxLength(1)] 
    public string? Grupo { get; set; } // "A", "B", etc. Null si es eliminación directa

    // REQUISITO TÉCNICO: Control de concurrencia
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    // Detalles
    public List<Gol>? GolesDetalle { get; set; } = new List<Gol>();
    public List<Tarjeta>? TarjetasDetalle { get; set; } = new List<Tarjeta>();
}