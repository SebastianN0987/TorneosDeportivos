using System.ComponentModel.DataAnnotations;

namespace TorneoDeportivo.Modelos;
public class Gol
{
    [Key] public int Id { get; set; }
    
    public int PartidoId { get; set; }
    public Partido Partido { get; set; } = null!;

    public int JugadorId { get; set; }
    public Jugador Jugador { get; set; } = null!;
    
    public int Minuto { get; set; }
    public bool EsAutogol { get; set; } = false;
}