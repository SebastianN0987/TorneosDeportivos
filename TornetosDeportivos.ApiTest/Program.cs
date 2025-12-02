using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace TornetosDeportivos.ApiTest
{
    internal class Program
    {
        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    return message?.RequestUri?.Host?.Equals("localhost", StringComparison.OrdinalIgnoreCase) == true;
                }
            };
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:7295/")
            };
            return client;
        }

        private static async Task<bool> WaitForApiAsync(HttpClient client, string relativeUrl, int maxAttempts, int delayMs)
        {
            for(int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var resp = await client.GetAsync(relativeUrl).ConfigureAwait(false);
                    if(resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"API disponible (intento {attempt}/{maxAttempts}).");
                        return true;
                    }
                    Console.WriteLine($"API aún no disponible (HTTP {(int)resp.StatusCode}). Reintentando...");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"API no responde ({ex.GetType().Name}). Reintentando...");
                }
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            return false;
        }

        private static void PrintTableFromApiResult(string json)
        {
            try
            {
                var root = JObject.Parse(json);
                var dataToken = root["data"];
                if(dataToken == null || dataToken.Type == JTokenType.Null)
                {
                    Console.WriteLine("Tabla de posiciones vacía (data=null).");
                    return;
                }
                if(dataToken is JArray arr && arr.Count > 0)
                {
                    // Obtener columnas desde el primer elemento
                    var first = (JObject)arr[0];
                    var columns = new List<string>();
                    foreach(var prop in first.Properties())
                    {
                        columns.Add(prop.Name);
                    }

                    // Encabezados
                    Console.WriteLine(string.Join(" | ", columns));
                    Console.WriteLine(new string('-', Math.Max(20, string.Join(" | ", columns).Length)));

                    // Filas
                    foreach(var item in arr)
                    {
                        var obj = (JObject)item;
                        var values = new List<string>();
                        foreach(var col in columns)
                        {
                            var val = obj.TryGetValue(col, out var token) ? token.ToString() : string.Empty;
                            values.Add(val);
                        }
                        Console.WriteLine(string.Join(" | ", values));
                    }
                }
                else
                {
                    Console.WriteLine("Tabla de posiciones: formato inesperado en 'data'.");
                    Console.WriteLine(json);
                }
            }
            catch(Exception)
            {
                Console.WriteLine("No se pudo parsear la respuesta de la tabla de posiciones.");
                Console.WriteLine(json);
            }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Esperando a que la API se inicie...");
            using var httpClient = CreateHttpClient();

            string rutaTorneos = "api/Torneos";
            // Ajuste: el controlador en la API se llama EquipossController, por lo que la ruta es 'api/Equiposs'
            string rutaEquipos = "api/Equiposs";
            string rutaPartidos = "api/Partidos";
            string rutaInscripciones = "api/Inscripciones";

            var apiReady = await WaitForApiAsync(httpClient, rutaTorneos, maxAttempts: 15, delayMs: 1000).ConfigureAwait(false);
            if(!apiReady)
            {
                Console.WriteLine("ERROR FATAL: La API no está disponible tras múltiples intentos.");
                return;
            }

            Console.WriteLine("Iniciando pruebas...");
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

            var response = await httpClient.PostAsync(rutaTorneos, content).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if(!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"ERROR FATAL: Falló la creación del torneo. Código: {(int)response.StatusCode}. Respuesta: {json}");
                return;
            }

            TorneoDeportivo.Modelos.ApiResult<TorneoDeportivo.Modelos.Torneo> torneoApiResult = null;
            TorneoDeportivo.Modelos.Torneo torneoCreado = null;
            try
            {
                torneoApiResult = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<TorneoDeportivo.Modelos.Torneo>>(json);
                torneoCreado = torneoApiResult?.Data;
            }
            catch
            {
                try
                {
                    torneoCreado = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.Torneo>(json);
                }
                catch(JsonException ex)
                {
                    Console.WriteLine($"ERROR FATAL: No se pudo deserializar la creación de torneo: {ex.Message}. Contenido: {json}");
                    return;
                }
            }

            if(torneoCreado == null || torneoCreado.Id <= 0)
            {
                Console.WriteLine($"ERROR FATAL: Torneo inválido tras creación. Contenido: {json}");
                return;
            }

            int torneoId = torneoCreado.Id;
            Console.WriteLine($"1. Torneo Creado: {torneoCreado.Nombre} (ID: {torneoId})");

            // --- 2. INSCRIBIR 16 EQUIPOS ---
            Console.WriteLine("2. Inscribiendo 16 equipos...");
            var idsEquipos = new List<int>();

            for(int i = 1; i <= 16; i++)
            {
                var equipo = new TorneoDeportivo.Modelos.Equipo { Nombre = $"Equipo {i}" };
                jsonPayload = JsonConvert.SerializeObject(equipo);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                response = await httpClient.PostAsync(rutaEquipos, content).ConfigureAwait(false);
                json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if(!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ERROR al crear Equipo {i}. Código: {response.StatusCode}. Respuesta: {json}");
                    throw new Exception($"Falló la creación del equipo: {json}");
                }

                TorneoDeportivo.Modelos.ApiResult<TorneoDeportivo.Modelos.Equipo> equipoCreado = null;
                try
                {
                    equipoCreado = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<TorneoDeportivo.Modelos.Equipo>>(json);
                }
                catch(JsonReaderException ex)
                {
                    Console.WriteLine($"ERROR al deserializar equipo {i}: {ex.Message}\nContenido recibido: {json}");
                    throw;
                }

                if(equipoCreado?.Data?.Id > 0)
                {
                    idsEquipos.Add(equipoCreado.Data.Id);
                    Console.WriteLine($"   -> Equipo {i} creado y añadido (ID: {equipoCreado.Data.Id})");
                }
                else
                {
                    Console.WriteLine($"ERROR al deserializar o el ID es inválido. JSON: {json}");
                    throw new Exception($"Respuesta API inválida para el equipo: {json}");
                }

                var inscripcionDto = new { TorneoId = torneoId, EquipoId = equipoCreado.Data.Id };
                jsonPayload = JsonConvert.SerializeObject(inscripcionDto);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var insResp = await httpClient.PostAsync(rutaInscripciones, content).ConfigureAwait(false);
                if(!insResp.IsSuccessStatusCode)
                {
                    var insJson = await insResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"ERROR al inscribir Equipo {i} en Torneo {torneoId}. Código: {insResp.StatusCode}. Respuesta: {insJson}");
                    throw new Exception($"Falló la inscripción del equipo: {insJson}");
                }
            }

            // --- 3. INICIAR TORNEO (Generar Calendario) ---
            Console.WriteLine("3. Iniciando torneo (Generando Fixture)...");
            response = await httpClient.PostAsync($"{rutaTorneos}/{torneoId}/iniciar", null).ConfigureAwait(false);
            if(response.IsSuccessStatusCode) Console.WriteLine("   -> Torneo Iniciado Correctamente.");
            else
            {
                var iniciarJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine($"ERROR al iniciar torneo. Código: {response.StatusCode}. Respuesta: {iniciarJson}");
                return;
            }

            // --- 4. JUGAR FASE DE GRUPOS (Registrar todos los resultados) ---
            Console.WriteLine("4. Jugando Fase de Grupos...");

            response = await httpClient.GetAsync($"{rutaPartidos}?torneoId={torneoId}&jugado=false").ConfigureAwait(false);
            json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>> listaPartidos = null;

            if(!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ERROR al obtener partidos. Código: {response.StatusCode}. Contenido: {json}");
                listaPartidos = new TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>> { Success = false, Data = new List<TorneoDeportivo.Modelos.Partido>() };
            }
            else if(string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("Respuesta vacía al solicitar partidos.");
                listaPartidos = new TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>> { Success = false, Data = new List<TorneoDeportivo.Modelos.Partido>() };
            }
            else
            {
                var trimmed = json.Trim();
                if(string.Equals(trimmed, "NaN", StringComparison.OrdinalIgnoreCase) ||
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
                    catch(JsonReaderException ex)
                    {
                        Console.WriteLine($"Error al deserializar listaPartidos: {ex.Message}\nContenido recibido: {json}");
                        throw;
                    }
                }
            }

            var rnd = new Random();

            if(listaPartidos?.Data != null)
            {
                foreach(var partido in listaPartidos.Data)
                {
                    partido.GolesLocal = rnd.Next(0, 4);
                    partido.GolesVisitante = rnd.Next(0, 4);
                    partido.Jugado = true;

                    jsonPayload = JsonConvert.SerializeObject(partido);
                    content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    response = await httpClient.PutAsync($"{rutaPartidos}/{partido.Id}/resultado", content).ConfigureAwait(false);
                    if(!response.IsSuccessStatusCode)
                    {
                        var putJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Console.WriteLine($"ERROR al registrar resultado partido {partido.Id}. Código: {response.StatusCode}. Respuesta: {putJson}");
                        throw new Exception($"Falló la actualización del resultado: {putJson}");
                    }
                }
            }
            Console.WriteLine("   -> Fase de Grupos finalizada.");

            // --- 5. CONSULTAR TABLA (Verificar clasificados) ---
            response = await httpClient.GetAsync($"{rutaTorneos}/{torneoId}/tabla-posiciones").ConfigureAwait(false);
            json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine("5. Tabla de posiciones calculada.");
            PrintTableFromApiResult(json);

            // --- 6. JUGAR ELIMINATORIAS (Cuartos, Semis, Final) ---
            Console.WriteLine("6. Jugando Eliminatorias...");

            bool hayPartidosPendientes = true;

            while(hayPartidosPendientes)
            {
                response = await httpClient.GetAsync($"{rutaPartidos}?torneoId={torneoId}&jugado=false").ConfigureAwait(false);
                json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if(!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ERROR al obtener partidos pendientes de eliminatorias. Código: {response.StatusCode}. Respuesta: {json}");
                    // Si no se puede listar, salir del bucle para no cerrar el test por excepción
                    break;
                }

                var partidosFases = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>>>(json);

                if(partidosFases?.Data == null || partidosFases.Data.Count == 0)
                {
                    hayPartidosPendientes = false;
                    break;
                }

                foreach(var p in partidosFases.Data)
                {
                    p.GolesLocal = rnd.Next(0, 3);
                    p.GolesVisitante = rnd.Next(0, 3);
                    p.Jugado = true;

                    if(p.GolesLocal == p.GolesVisitante)
                    {
                        p.PenalesLocal = rnd.Next(3, 6);
                        do
                        {
                            p.PenalesVisitante = rnd.Next(3, 6);
                        } while(p.PenalesVisitante == p.PenalesLocal);
                    }

                    jsonPayload = JsonConvert.SerializeObject(p);
                    content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    response = await httpClient.PutAsync($"{rutaPartidos}/{p.Id}/resultado", content).ConfigureAwait(false);
                    if(!response.IsSuccessStatusCode)
                    {
                        var putJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Console.WriteLine($"ERROR al registrar resultado eliminatoria {p.Id}. Código: {response.StatusCode}. Respuesta: {putJson}");
                        // Continuar con los demás partidos en lugar de lanzar excepción
                        continue;
                    }

                    Console.WriteLine($"   -> Jugado {p.Fase}: {p.EquipoLocalId} vs {p.EquipoVisitanteId}");
                }
            }

            // --- 7. CONSULTAR CAMPEÓN ---
            response = await httpClient.GetAsync($"{rutaPartidos}?torneoId={torneoId}&fase=Final").ConfigureAwait(false);
            json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var finalResult = JsonConvert.DeserializeObject<TorneoDeportivo.Modelos.ApiResult<List<TorneoDeportivo.Modelos.Partido>>>(json);

            if(finalResult?.Data != null && finalResult.Data.Count > 0)
            {
                var final = finalResult.Data[0];
                int idGanador = (final.GolesLocal > final.GolesVisitante || (final.PenalesLocal > final.PenalesVisitante))
                                ? final.EquipoLocalId : final.EquipoVisitanteId;
                Console.WriteLine($"7. EL CAMPEÓN ES EL EQUIPO ID: {idGanador}");
            }

            // --- 8. TABLA DE GOLEADORES ---
            response = await httpClient.GetAsync($"{rutaTorneos}/{torneoId}/goleadores").ConfigureAwait(false);
            json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine("8. Goleadores consultados.");

            // --- 9. HISTORIAL ENTRE DOS EQUIPOS ---
            if(idsEquipos.Count >= 2)
            {
                int eq1 = idsEquipos[0];
                int eq2 = idsEquipos[1];
                response = await httpClient.GetAsync($"api/Reportes/historial?equipo1Id={eq1}&equipo2Id={eq2}").ConfigureAwait(false);
                json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine($"9. Historial entre {eq1} y {eq2} consultado.");
            }
            else
            {
                Console.WriteLine("9. No se pudo consultar el historial (no hay suficientes equipos creados).");
            }
            Console.WriteLine("=== TEST FINALIZADO ===");
            Console.ReadKey();
        }
    }
}
