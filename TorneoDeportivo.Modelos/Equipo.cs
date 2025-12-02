using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Necesario para List<T>
using System.ComponentModel.DataAnnotations.Schema; // Necesario para [ForeignKey]


namespace TorneoDeportivo.Modelos;
public class Equipo
{
    [Key] public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    // Relaciones
    public List<Jugador>? Jugadores { get; set; } = new List<Jugador>();
    public List<Inscripcion>? Inscripciones { get; set; } = new List<Inscripcion>();
}