using System;
using System.Collections.Generic;
using System.Linq;

namespace ROM.EditorTools
{
    // Pure analysis of stored evidence. No Unity, physics, input, scene or networking API.
    public static class CiudadDelSolWalkEvidence
    {
        [Serializable] public sealed class Point { public float x, y, z; }
        [Serializable] public sealed class Sample
        {
            public double seconds;
            public uint character;
            public Point position;
            public int collisionFlags;
            public bool grounded, support, nav, physicalNotChecked;
            public float edge;
        }
        [Serializable] public sealed class Participant
        {
            public uint characterNetId;
            public int connection;
            public Point initialPosition;
        }
        [Serializable] public sealed class Event
        {
            public double seconds;
            public uint character;
            public string kind, detail;
            public Point position;
        }
        [Serializable] public sealed class Capture
        {
            public string result, startedUTC, endedUTC, sourceCapture, sourceSHA256;
            public string sceneBefore, sceneAfter, navBefore, navAfter, populationBefore, populationAfter;
            public bool playMode, realConnections, protectedFilesUnchanged;
            public int requested, initialCheckpoint;
            public Point[] checkpoints;
            public List<Participant> participants = new List<Participant>();
            public List<Sample> samples = new List<Sample>();
            public List<Event> events = new List<Event>();
        }
        [Serializable] public sealed class Hit
        {
            public int checkpoint;
            public double seconds, distance;
            public Point position;
            public string evidence;
        }
        [Serializable] public sealed class Gap { public double from, to, duration; public Point start, end; }
        [Serializable] public sealed class PersonReport
        {
            public uint character;
            public int next, sampleCount, supportFailures, navFailures, sideContacts, ungrounded, nearEdges;
            public bool complete;
            public float minimumEdge = float.MaxValue;
            public Point lastPosition;
            public List<Hit> hits = new List<Hit>();
            public List<Gap> gaps = new List<Gap>();
            public double nextClosestDistance = -1, nextClosestTime = -1;
        }
        [Serializable] public sealed class Report
        {
            public string status, originalResult, limitation = "Muestras discretas; no certifica todos los instantes ni cuatro jugadores por una prueba individual.";
            public bool routeValid, verifiedPrefix, complete, physicalWarnings;
            public string stopReason;
            public bool stoppedOutsideXZ, stoppedWrongHeight;
            public Point terminalPosition;
            public double stoppedAt;
            public int requested;
            public List<PersonReport> people = new List<PersonReport>();
            public List<string> warnings = new List<string>();
        }

        public static Report Analyze(Capture c, Report verifiedPrior = null)
        {
            var r = new Report { originalResult = c == null ? "" : c.result, requested = c == null ? 0 : c.requested };
            if (c == null || !RouteMatches(c.checkpoints) || !c.playMode || !c.realConnections || c.participants == null ||
                c.samples == null || c.participants.Count != c.requested || (c.requested != 1 && c.requested != 4) ||
                c.participants.Select(p => p.characterNetId).Distinct().Count() != c.requested ||
                c.participants.Select(p => p.connection).Distinct().Count() != c.requested)
            { r.status = "CAPTURA_NO_RECONOCIDA; SIN_CREDITOS"; return r; }
            r.routeValid = true;
            var stop = c.events == null ? null : c.events.LastOrDefault(e => e.kind == "FUERA_AMBITO_O_CAIDA" || e.kind == "FUERA_AMBITO_XZ" || e.kind == "CAIDA_O_COTA_INCOMPATIBLE");
            if(stop != null && stop.position != null)
            {
                r.terminalPosition=stop.position; r.stoppedAt=stop.seconds;
                r.stoppedOutsideXZ=stop.position.x<102 || stop.position.x>180 || stop.position.z<90 || stop.position.z>144;
                r.stoppedWrongHeight=stop.position.y< -16.5f || stop.position.y> -15.4f;
                r.stopReason=r.stoppedWrongHeight ? "COTA_FUERA_RANGO; NO_EQUIVALE_POR_SI_SOLA_A_CAIDA" : r.stoppedOutsideXZ ? "SALIDA_AMBITO_XZ; CAIDA_NO_DEMOSTRADA" : "REVISAR_EVENTO_TERMINAL";
            }
            int prefix = c.initialCheckpoint;
            if (prefix != 0)
            {
                // Caller must have verified the source file SHA-256 before supplying prior.
                if (c.requested != 1 || prefix < 1 || prefix > 7 || verifiedPrior == null || !verifiedPrior.routeValid ||
                    verifiedPrior.people.Count != 1 || verifiedPrior.people[0].next != prefix + 1 ||
                    c.participants[0].initialPosition == null || Distance(c.participants[0].initialPosition, c.checkpoints[prefix]) >= 1)
                { r.status = "CONTINUACION_SIN_PREFIJO_ACREDITADO"; return r; }
                r.verifiedPrefix = true;
                r.warnings.Add("Tramos separados: no se acredita continuidad física durante la pausa entre capturas.");
                r.physicalWarnings = verifiedPrior.physicalWarnings;
            }
            foreach (var p in c.participants)
            {
                var person = new PersonReport { character = p.characterNetId, next = prefix == 0 ? 0 : prefix + 1 };
                r.people.Add(person);
                if (prefix == 0 && p.initialPosition != null && Distance(p.initialPosition, c.checkpoints[0]) < 1)
                { person.hits.Add(new Hit { checkpoint = 0, seconds = 0, position = p.initialPosition, distance = Distance(p.initialPosition, c.checkpoints[0]), evidence = "posición inicial registrada" }); person.next = 1; }
                Sample previous = null;
                foreach (var s in c.samples.Where(s => s.character == p.characterNetId))
                {
                    if (s.position == null || double.IsNaN(s.seconds) || double.IsInfinity(s.seconds) || s.seconds < 0 ||
                        !Finite(s.position) || (previous != null && s.seconds < previous.seconds))
                    { r.physicalWarnings = true; r.warnings.Add("Muestras inválidas/no cronológicas para " + p.characterNetId); break; }
                    if (previous != null && Distance(s.position,previous.position) > Math.Max(5,(s.seconds-previous.seconds)*20))
                    { r.physicalWarnings = true; r.warnings.Add("Salto de posición: no se acreditan puntos posteriores al salto."); break; }
                    if (previous != null && s.seconds - previous.seconds > .5)
                        person.gaps.Add(new Gap { from = previous.seconds, to = s.seconds, duration = s.seconds - previous.seconds, start = previous.position, end = s.position });
                    previous = s; person.sampleCount++; person.lastPosition = s.position;
                    if (s.physicalNotChecked) { r.warnings.Add("Muestra terminal sin consultas físicas: no acredita un punto."); continue; }
                    if (!s.support) person.supportFailures++;
                    if (!s.nav) person.navFailures++;
                    if (!s.grounded) person.ungrounded++;
                    if ((s.collisionFlags & 1) != 0) person.sideContacts++;
                    if (s.edge >= 0) { person.minimumEdge = Math.Min(person.minimumEdge, s.edge); if (s.edge < .57f) person.nearEdges++; }
                    // Exact original strict 3D radius. Never interpolate across missing samples.
                    if (person.next < c.checkpoints.Length && Inside(s.position) && Distance(s.position, c.checkpoints[person.next]) < 1)
                    {
                        person.hits.Add(new Hit { checkpoint = person.next, seconds = s.seconds, position = s.position,
                            distance = Distance(s.position, c.checkpoints[person.next]), evidence = "muestra original" }); person.next++;
                    }
                }
                person.complete = person.next == c.checkpoints.Length;
                if (!person.complete)
                {
                    // Only samples after the last accredited point may close the next one.
                    double after = person.hits.Count == 0 ? 0 : person.hits.Last().seconds;
                    foreach (var s in c.samples.Where(s => s.character == p.characterNetId && s.seconds >= after && s.position != null && !s.physicalNotChecked))
                    {
                        double d = Distance(s.position, c.checkpoints[person.next]);
                        if (person.nextClosestDistance < 0 || d < person.nextClosestDistance) { person.nextClosestDistance = d; person.nextClosestTime = s.seconds; }
                    }
                }
                r.physicalWarnings |= person.supportFailures > 0 || person.navFailures > 0 || person.gaps.Count > 0;
            }
            if (c.events != null)
                foreach (var e in c.events.Where(e => e.kind == "POSIBLE_ATASCO" || e.kind == "SALTO_DE_POSICION" || e.kind == "ERROR_OBSERVADOR" || e.kind == "CAIDA_O_COTA_INCOMPATIBLE"))
                { r.physicalWarnings = true; r.warnings.Add(e.kind + " a " + e.seconds + " s: " + e.detail); }
            if (!c.protectedFilesUnchanged) { r.physicalWarnings=true; r.warnings.Add("Integridad de archivos protegidos no acreditada al cerrar la captura."); }
            r.complete = r.people.Count > 0 && r.people.All(p => p.complete);
            r.status = r.complete ? (r.verifiedPrefix ? "TRAMOS_ACREDITADOS_EN_CAPTURAS_SEPARADAS; REVISION_PENDIENTE" : "PUNTOS_IDA_VUELTA_ACREDITADOS; REVISION_PENDIENTE") : "EVIDENCIA_PARCIAL; SOLO_PUNTOS_FALTANTES_PENDIENTES";
            return r;
        }
        static bool RouteMatches(Point[] points)
        {
            float[] xs = { 156,174,174,120,117,120,174,174,156 }, zs = { 99,99,117,117,123,117,117,99,99 };
            if (points == null || points.Length != xs.Length) return false;
            for (int i = 0; i < xs.Length; i++) if (points[i] == null || !Finite(points[i]) || Math.Abs(points[i].x-xs[i]) > .0001 || Math.Abs(points[i].y+16) > .0001 || Math.Abs(points[i].z-zs[i]) > .0001) return false;
            return true;
        }
        static bool Finite(Point p) => !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.x) && !float.IsInfinity(p.y) && !float.IsInfinity(p.z);
        static bool Inside(Point p) => p.x >= 102 && p.x <= 180 && p.z >= 90 && p.z <= 144 && p.y >= -16.5f && p.y <= -15.4f;
        static double Distance(Point a, Point b) { double x = a.x-b.x, y = a.y-b.y, z = a.z-b.z; return Math.Sqrt(x*x+y*y+z*z); }
    }
}
