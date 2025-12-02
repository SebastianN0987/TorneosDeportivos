using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Necesario para List<T>
using System.ComponentModel.DataAnnotations.Schema; // Necesario para [ForeignKey]

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