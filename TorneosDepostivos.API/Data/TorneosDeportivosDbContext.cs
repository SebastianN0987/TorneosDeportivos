using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

    public class TorneosDeportivosDbContext : DbContext
    {
        public TorneosDeportivosDbContext (DbContextOptions<TorneosDeportivosDbContext> options)
            : base(options)
        {
        }

        public DbSet<Equipo> Equipos { get; set; } = default!;

public DbSet<Gol> Goles { get; set; } = default!;

public DbSet<Inscripcion> Inscripciones { get; set; } = default!;

public DbSet<Jugador> Jugadores { get; set; } = default!;

public DbSet<Partido> Partidos { get; set; } = default!;

public DbSet<Torneo> Torneos { get; set; } = default!;
    }
