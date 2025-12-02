using System.ComponentModel.DataAnnotations;
namespace TorneoDeportivo.Modelos;
using System.Collections.Generic; // Necesario para List<T>
using System.ComponentModel.DataAnnotations.Schema; // Necesario para [ForeignKey]
using System.Text.Json.Serialization;

public class Inscripcion
{
    [Key] public int Id { get; set; }
    
    // FKs
    public int TorneoId { get; set; }

    [JsonIgnore] 
    public Torneo? Torneo { get; set; }

    public int EquipoId { get; set; }

    [JsonIgnore] 
    public Equipo? Equipo { get; set; }

    public DateTime FechaInscripcion { get; set; } = DateTime.UtcNow;
}