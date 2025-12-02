using Newtonsoft.Json;
using System.Text;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TornetosDeportivos.ApiTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Esperando 5 segundos para que la API se inicie...");
            Thread.Sleep(5000);
            Console.WriteLine("Iniciando pruebas...");

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://localhost:7295/"); // Ajustar puerto

            string rutaTorneos = "api/Torneos";
            // CORRECCIÓN: rutaEquipos tenía un typo "Equiposs"
            string rutaEquipos = "api/Equipos";
            string rutaPartidos = "api/Partidos";
            string rutaInscripciones = "api/Inscripciones";

            Console.WriteLine("=== INICIANDO TEST DE TORNEO ===");

            // --- 1. CREAR TORNEO MIXTO ---
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

            // Validación básica para Torneo (si falla, el resto fallará)
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ERROR FATAL: Falló la creación del torneo. Respuesta: {json}");
                return;
            }

            var torneoCreado = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.Torneo>(json);
            int torneoId = torneoCreado.Id;

            Console.WriteLine($"1. Torneo Creado: {torneoCreado.Nombre} (ID: {torneoId})");

            // --- 2. INSCRIBIR 16 EQUIPOS ---
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

                // 1. Validar Status Code HTTP
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ERROR al crear Equipo {i}. Código: {response.StatusCode}. Respuesta: {json}");
                    throw new Exception($"Falló la creación del equipo: {json}");
                }

                // 2. Deserializar el ApiResult
                var equipoCreado = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<TorneoDeportivo.Modelos.Equipo>>(json);

                // 3. Validar el objeto deserializado
                if (equipoCreado?.Data?.Id > 0)
                {
                    idsEquipos.Add(equipoCreado.Data.Id);
                    Console.WriteLine($"   -> Equipo {i} creado y añadido (ID: {equipoCreado.Data.Id})");
                }
                else
                {
                    Console.WriteLine($"ERROR al deserializar o el ID es inválido. JSON: {json}");
                    throw new Exception($"Respuesta API inválida para el equipo: {json}");
                }

                // B. Inscribir
                var inscripcion = new TorneoDeportivo.Modelos.Inscripcion { TorneoId = torneoId, EquipoId = equipoCreado.Data.Id };
                jsonPayload = JsonConvert.SerializeObject(inscripcion);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                response = httpClient.PostAsync(rutaInscripciones, content).Result;
            }

            // --- 3. INICIAR TORNEO (Generar Calendario) ---
            Console.WriteLine("3. Iniciando torneo (Generando Fixture)...");
            response = httpClient.PostAsync($"{rutaTorneos}/{torneoId}/iniciar", null).Result;
            if (response.IsSuccessStatusCode) Console.WriteLine("   -> Torneo Iniciado Correctamente.");

            // --- 4. JUGAR FASE DE GRUPOS (Registrar todos los resultados) ---
            Console.WriteLine("4. Jugando Fase de Grupos...");

            // Obtener partidos pendientes
            response = httpClient.GetAsync($"{rutaPartidos}?torneoId={torneoId}&jugado=false").Result;
            json = response.Content.ReadAsStringAsync().Result;

            // VALIDACIÓN Y DESERIALIZACIÓN SEGURA: evita JsonReaderException por "NaN" u otras respuestas no JSON
            TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>> listaPartidos = null;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ERROR al obtener partidos. Código: {response.StatusCode}. Contenido: {json}");
                listaPartidos = new TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>> { Success = false, Data = new List<TorneoDeportivo.Modelos.Partido>() };
            }
            else if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("Respuesta vacía al solicitar partidos.");
                listaPartidos = new TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>> { Success = false, Data = new List<TorneoDeportivo.Modelos.Partido>() };
            }
            else
            {
                var trimmed = json.Trim();
                // Si el API devolviera literalmente "NaN" (u otros valores primitivos inesperados), lo manejamos explícitamente
                if (string.Equals(trimmed, "NaN", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(trimmed, "Infinity", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(trimmed, "-Infinity", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"La API devolvió un valor numérico especial en lugar de JSON válido: {trimmed}");
                    listaPartidos = new TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>> { Success = false, Data = new List<TorneoDeportivo.Modelos.Partido>() };
                }
                else
                {
                    try
                    {
                        listaPartidos = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>>>(json);
                    }
                    catch (JsonReaderException ex)
                    {
                        Console.WriteLine($"Error al deserializar listaPartidos: {ex.Message}\nContenido recibido: {json}");
                        throw;
                    }
                }
            }

            var rnd = new Random();

            if (listaPartidos?.Data != null)
            {
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
            }
            Console.WriteLine("   -> Fase de Grupos finalizada.");



            // --- 5. CONSULTAR TABLA (Verificar clasificados) ---

            response = httpClient.GetAsync($"{rutaTorneos}/{torneoId}/tabla-posiciones").Result;

            json = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine($"5. Tabla de posiciones calculada.\n   Respuesta: {json.Substring(0, Math.Min(json.Length, 100))}...");



            // --- 6. JUGAR ELIMINATORIAS (Cuartos, Semis, Final) ---

            Console.WriteLine("6. Jugando Eliminatorias...");


            bool hayPartidosPendientes = true;

            while (hayPartidosPendientes)

            {

                // Buscar partidos nuevos generados (Cuartos -> Semis -> Final)

                response = httpClient.GetAsync($"{rutaPartidos}?torneoId={torneoId}&jugado=false").Result;

                json = response.Content.ReadAsStringAsync().Result;

                var partidosFases = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>>>(json);


                if (partidosFases.Data == null || partidosFases.Data.Count == 0)

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

                        do

                        {

                            p.PenalesVisitante = rnd.Next(3, 6);

                        } while (p.PenalesVisitante == p.PenalesLocal);

                    }


                    jsonPayload = JsonConvert.SerializeObject(p);

                    content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    response = httpClient.PutAsync($"{rutaPartidos}/{p.Id}/resultado", content).Result;


                    Console.WriteLine($"   -> Jugado {p.Fase}: {p.EquipoLocalId} vs {p.EquipoVisitanteId}");

                }

            }



            // --- 7. CONSULTAR CAMPEÓN ---

            // Buscamos el partido de la Final

            response = httpClient.GetAsync($"{rutaPartidos}?torneoId={torneoId}&fase=Final").Result; // Ajusta tu enum/string fase

            json = response.Content.ReadAsStringAsync().Result;

            var finalResult = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>>>(json);


            if (finalResult?.Data != null && finalResult.Data.Count > 0)

            {

                var final = finalResult.Data[0];

                int idGanador = (final.GolesLocal > final.GolesVisitante || (final.PenalesLocal > final.PenalesVisitante))

                                ? final.EquipoLocalId : final.EquipoVisitanteId;

                Console.WriteLine($"7. EL CAMPEÓN ES EL EQUIPO ID: {idGanador}");

            }



            // --- 8. TABLA DE GOLEADORES ---

            response = httpClient.GetAsync($"{rutaTorneos}/{torneoId}/goleadores").Result;

            json = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine("8. Goleadores consultados.");



            // --- 9. HISTORIAL ENTRE DOS EQUIPOS ---

            if (idsEquipos.Count >= 2)

            {

                int eq1 = idsEquipos[0];

                int eq2 = idsEquipos[1];

                response = httpClient.GetAsync($"api/Reportes/historial?equipo1Id={eq1}&equipo2Id={eq2}").Result;

                json = response.Content.ReadAsStringAsync().Result;

                Console.WriteLine($"9. Historial entre {eq1} y {eq2} consultado.");

            }

            else

            {

                Console.WriteLine("9. No se pudo consultar el historial (no hay suficientes equipos creados).");

            }



            Console.WriteLine("\nTest finalizado. Presiona Enter.");

            Console.ReadLine();
        }
    }
}