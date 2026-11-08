using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string WestRoot = "Subsuelo_Ala_Occidental_Etapa03";
        const string NorthRoot = "Subsuelo_Ala_Norte_Etapa04";
        const string SouthRoot = "Subsuelo_Central_Sureste_Etapa02";
        const string EastRoot = "Subsuelo_Ala_Este_Cierre_Etapa05";

        // Footprints and capital IDs shortlisted from the existing measurements and inventory.
        static readonly Sector[] ConnectionProposals =
        {
            Link("O1 Pórtico hacia Jardines", WestRoot, -150, -78, -48, false, 981306585, 2046656265),
            Link("O2 Nave sur de Jardines", WestRoot, -90, -84, -54, false, 1752911320, 1448740102),
            Link("O3 Galería norte de Jardines", WestRoot, -90, 66, 96, false, 389892144, 1827189537),
            Link("O4 Jardines hacia Archivos", WestRoot, -90, 102, 132, false, 1888306012, 1013512404),
            Link("O5 Patio bajo Archivos", WestRoot, -114, 72, 108, false, 831448273, 252334206, 257566326),
            Link("O6 Galería occidental de Archivos", WestRoot, -138, 84, 126, false, 81718399, 458126880, 2009004128),
            Link("O7 Jardines hacia eje central", WestRoot, -66, 30, 66, false, 786692939, 703139566),
            Link("N1 Ala oeste del Claustro", NorthRoot, -54, 78, 108, false, 150946337, 977109924),
            Link("N2 Enlace noroeste del Claustro", NorthRoot, -30, 108, 132, false, 1451771354),
            Link("N3 Acceso sur del Claustro", NorthRoot, 18, 102, 126, false, 1043655424),
            Link("N4 Galería este del Claustro", NorthRoot, 42, 144, 174, false, 710785414, 1763290661),
            Link("N5 Galería norte hacia el Sol", NorthRoot, 66, 102, 138, false, 1512333573, 880992777),
            Link("S1 Santuario hacia el sur", SouthRoot, 138, -126, -102, false, 670354912, 1522184970),
            Link("S2 Galería junto al Santuario", SouthRoot, 162, -96, -60, false, 1624669142, 2090509370),
            Link("S3 Enlace oriental del Santuario", SouthRoot, 174, -24, 12, false, 1820626025, 711826637),
            Link("E1 Santuario hacia el borde este", EastRoot, 198, -54, -18, false, 477434246, 983808960, 2005529955),
            Link("E2 Galería este hacia el sur", EastRoot, 204, 78, 108, true),
            Link("E3 Galería oriental del Sol", EastRoot, 204, 150, 186, true),
            Link("E4 Galería del Sol hacia el oeste", EastRoot, 156, 126, 162, true, 267085350, 1649879030, 1933163054),
            Link("E5 Enlace Claustro y Sol", EastRoot, 126, 144, 180, false, 1272862306, 856710452),
            Link("E6 Entrada occidental al ala del Sol", EastRoot, 96, 150, 180, true, 1140412574, 2106005164),
        };

        static Sector Link(string name, string root, float west, float south, float north, bool eastBearing,
            params ulong[] capitals) =>
            new Sector(name, root, west, west + 18, south, north, .5f,
                new[] { south + 6, north - 6 }, capitals, eastBearing, profilePower: 4, minimumPeak: 13);

        static readonly HashSet<string> ValidatedConnections = new HashSet<string>
        {
            "O3", "O4", "O5", "O6", "N1", "N3", "N5", "S2", "S3", "E2", "E3", "E4", "E6",
        };

        static IEnumerable<Sector> ConnectionDesigns() =>
            ConnectionProposals.Where(proposal => ValidatedConnections.Contains(proposal.Name.Substring(0, 2)))
                .Select(proposal => WithCrown(proposal, proposal.Name.StartsWith("S3 ", StringComparison.Ordinal) ? -2.9f : .5f));

        static Sector WithCrown(Sector proposal, float crown) =>
            new Sector(proposal.Name, proposal.Root, proposal.West, proposal.East, proposal.South,
                proposal.North, crown, proposal.Ribs, proposal.CapitalIds, proposal.EastBearing,
                profilePower: crown < -2 ? 6 : 4, minimumPeak: 13);

        public static void ScreenConnectionsInBatch()
        {
            var source = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            CheckExpansionSource();
            foreach (var proposal in ConnectionProposals)
            {
                string reason = "";
                foreach (float crown in new[] { .5f, -1.15f, -2.9f })
                {
                    var design = WithCrown(proposal, crown);
                    try
                    {
                        Validate(source, design);
                        Debug.Log(FormattableString.Invariant($"CONEXION_APTA {proposal.Name}: clave Y={crown}"));
                        reason = null;
                        break;
                    }
                    catch (InvalidOperationException error)
                    {
                        reason = error.Message;
                        if (!reason.StartsWith("Interferencia superior", StringComparison.Ordinal)) break;
                    }
                }
                if (reason != null) Debug.Log($"CONEXION_RESTRINGIDA {proposal.Name}: {reason}");
            }
            CheckExpansionSource();
        }
    }
}
