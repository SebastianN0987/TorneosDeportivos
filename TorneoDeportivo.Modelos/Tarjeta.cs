
using System.ComponentModel.DataAnnotations;
public class Tarjeta
{
    [Key] public int Id { get; set; }
    
    public int PartidoId { get; set; }
    public Partido Partido { get; set; } = null!;

    public int JugadorId { get; set; }
    public Jugador Jugador { get; set; } = null!;

    public TipoTarjeta Tipo { get; set; }
    public int Minuto { get; set; }
}