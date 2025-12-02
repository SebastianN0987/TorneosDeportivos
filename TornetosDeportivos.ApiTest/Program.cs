using Newtonsoft.Json;
using System.Text;

namespace TornetosDeportivos.ApiTest
{
    internal class Program
    {
         static void Main(string[] args)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://localhost:7011/"); // Ajusta tu puerto aquí

            string rutaTorneos = "api/Torneos";
            string rutaEquipos = "api/Equipos";
            string rutaPartidos = "api/Partidos";
            string rutaInscripciones = "api/Inscripciones";

            Console.WriteLine("=== INICIANDO TEST DE TORNEO ===");

            // 1. CREAR TORNEO MIXTO
            // ------------------------------------------------------------
            var nuevoTorneo = new TorneoDeportivo.Modelos.Torneo
            {
                Nombre = "Copa Primavera 2024",
                Tipo = TorneoDeportivo.Modelos.TipoTorneo.Mixto,
                FechaInicio = DateTime.UtcNow.AddDays(1),
                FechaFin = DateTime.UtcNow.AddDays(30)
            };

            var jsonPayload = JsonConvert.SerializeObject(nuevoTorneo);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
            var response = httpClient.PostAsync(rutaTorneos, content).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            
            // Deserializar respuesta (Ajusta Modelos.ApiResult según tu clase real)
            var torneoCreado = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<TorneoDeportivo.Modelos.Torneo>>(json);
            int torneoId = torneoCreado.Data.Id;
            Console.WriteLine($"1. Torneo Creado: {torneoCreado.Data.Nombre} (ID: {torneoId})");


            // 2. INSCRIBIR 16 EQUIPOS
            // ------------------------------------------------------------
            Console.WriteLine("2. Inscribiendo 16 equipos...");
            var idsEquipos = new List<int>();

            for (int i = 1; i <= 16; i++)
            {
                // A. Crear Equipo
                var equipo = new TorneoDeportivo.Modelos.Equipo { Nombre = $"Equipo {i}" };
                jsonPayload = JsonConvert.SerializeObject(equipo);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                response = httpClient.PostAsync(rutaEquipos, content).Result;
                json = response.Content.ReadAsStringAsync().Result;
                var equipoCreado = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<TorneoDeportivo.Modelos.Equipo>>(json);
                idsEquipos.Add(equipoCreado.Data.Id);

                // B. Inscribir
                var inscripcion = new TorneoDeportivo.Modelos.Inscripcion { TorneoId = torneoId, EquipoId = equipoCreado.Data.Id };
                jsonPayload = JsonConvert.SerializeObject(inscripcion);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                response = httpClient.PostAsync(rutaInscripciones, content).Result;
            }


            // 3. INICIAR TORNEO (Generar Calendario)
            // ------------------------------------------------------------
            Console.WriteLine("3. Iniciando torneo (Generando Fixture)...");
            response = httpClient.PostAsync($"{rutaTorneos}/{torneoId}/iniciar", null).Result;
            // Validar status code si es necesario
            if(response.IsSuccessStatusCode) Console.WriteLine("   -> Torneo Iniciado Correctamente.");


            // 4. JUGAR FASE DE GRUPOS (Registrar todos los resultados)
            // ------------------------------------------------------------
            Console.WriteLine("4. Jugando Fase de Grupos...");
            
            // Obtener partidos pendientes
            response = httpClient.GetAsync($"{rutaPartidos}?torneoId={torneoId}&jugado=false").Result;
            json = response.Content.ReadAsStringAsync().Result;
            var listaPartidos = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>>>(json);

            var rnd = new Random();

            foreach (var partido in listaPartidos.Data)
            {
                partido.GolesLocal = rnd.Next(0, 4);
                partido.GolesVisitante = rnd.Next(0, 4);
                partido.Jugado = true;

                jsonPayload = JsonConvert.SerializeObject(partido);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // Actualizar resultado
                response = httpClient.PutAsync($"{rutaPartidos}/{partido.Id}/resultado", content).Result;
            }
            Console.WriteLine("   -> Fase de Grupos finalizada.");


            // 5. CONSULTAR TABLA (Verificar clasificados)
            // ------------------------------------------------------------
            response = httpClient.GetAsync($"{rutaTorneos}/{torneoId}/tabla-posiciones").Result;
            json = response.Content.ReadAsStringAsync().Result;
            // Aquí podrías deserializar e imprimir la tabla, pero solo lo mostramos como paso cumplido
            Console.WriteLine($"5. Tabla de posiciones calculada.\n   Respuesta: {json.Substring(0, Math.Min(json.Length, 100))}..."); 


            // 6. JUGAR ELIMINATORIAS (Cuartos, Semis, Final)
            // ------------------------------------------------------------
            Console.WriteLine("6. Jugando Eliminatorias...");
            
            bool hayPartidosPendientes = true;
            while (hayPartidosPendientes)
            {
                // Buscar partidos nuevos generados (Cuartos -> Semis -> Final)
                response = httpClient.GetAsync($"{rutaPartidos}?torneoId={torneoId}&jugado=false").Result;
                json = response.Content.ReadAsStringAsync().Result;
                var partidosFases = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>>>(json);

                if (partidosFases.Data.Count == 0)
                {
                    hayPartidosPendientes = false;
                    break;
                }

                foreach (var p in partidosFases.Data)
                {
                    p.GolesLocal = rnd.Next(0, 3);
                    p.GolesVisitante = rnd.Next(0, 3);
                    p.Jugado = true;

                    // REGLA: En eliminatoria NO hay empates (Penales)
                    if (p.GolesLocal == p.GolesVisitante)
                    {
                        p.PenalesLocal = rnd.Next(3, 6);
                        do {
                            p.PenalesVisitante = rnd.Next(3, 6);
                        } while (p.PenalesVisitante == p.PenalesLocal);
                    }

                    jsonPayload = JsonConvert.SerializeObject(p);
                    content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    response = httpClient.PutAsync($"{rutaPartidos}/{p.Id}/resultado", content).Result;
                    
                    Console.WriteLine($"   -> Jugado {p.Fase}: {p.EquipoLocalId} vs {p.EquipoVisitanteId}");
                }
            }


            // 7. CONSULTAR CAMPEÓN
            // ------------------------------------------------------------
            // Buscamos el partido de la Final
            response = httpClient.GetAsync($"{rutaPartidos}?torneoId={torneoId}&fase=Final").Result; // Ajusta tu enum/string fase
            json = response.Content.ReadAsStringAsync().Result;
            var finalResult = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>>>(json);
            
            if (finalResult.Data != null && finalResult.Data.Count > 0)
            {
                var final = finalResult.Data[0];
                int idGanador = (final.GolesLocal > final.GolesVisitante || (final.PenalesLocal > final.PenalesVisitante)) 
                                ? final.EquipoLocalId : final.EquipoVisitanteId;
                Console.WriteLine($"7. EL CAMPEÓN ES EL EQUIPO ID: {idGanador}");
            }


            // 8. TABLA DE GOLEADORES
            // ------------------------------------------------------------
            response = httpClient.GetAsync($"{rutaTorneos}/{torneoId}/goleadores").Result;
            json = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("8. Goleadores consultados.");


            // 9. HISTORIAL ENTRE DOS EQUIPOS
            // ------------------------------------------------------------
            int eq1 = idsEquipos[0];
            int eq2 = idsEquipos[1];
            response = httpClient.GetAsync($"api/Reportes/historial?equipo1Id={eq1}&equipo2Id={eq2}").Result;
            json = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"9. Historial entre {eq1} y {eq2} consultado.");


            Console.WriteLine("\nTest finalizado. Presiona Enter.");
            Console.ReadLine();
        }
    }
}
