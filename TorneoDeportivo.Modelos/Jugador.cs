using System.ComponentModel.DataAnnotations;

namespace TorneoDeportivo.Modelos;
public class Jugador
{
    [Key] public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    // Fks
    public int EquipoId { get; set; }
    public Equipo Equipo { get; set; } = null!;

    // Para estadísticas rápidas
    public List<Gol>? Goles { get; set; } = new List<Gol>();
    public List<Tarjeta>? Tarjetas { get; set; } = new List<Tarjeta>();
}