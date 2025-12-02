using System.ComponentModel.DataAnnotations;

public class Torneo
{
    [Key] public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public TipoTorneo Tipo { get; set; }
    
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    
    public bool Iniciado { get; set; } = false;

    // Relaciones
    public List<Inscripcion>? Inscripciones { get; set; } = new List<Inscripcion>();
    public List<Partido>? Partidos { get; set; } = new List<Partido>();
}