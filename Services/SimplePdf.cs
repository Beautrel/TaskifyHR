using System.Text;

namespace TaskifyHR.Services;

/// <summary>
/// Générateur de PDF minimal (texte seul, police Helvetica, A4) : aucune bibliothèque externe,
/// donc aucun poids ajouté à l'application. Suffisant pour un relevé texte.
/// </summary>
public static class SimplePdf
{
    private const int LinesPerPage = 48;

    public static byte[] Create(IReadOnlyList<string> lines)
    {
        var enc = Encoding.Latin1; // les lettres accentuées françaises passent en Latin-1
        var pages = lines.Chunk(LinesPerPage).ToList();
        if (pages.Count == 0)
            pages.Add(Array.Empty<string>());

        // Numérotation des objets PDF : 1 = catalogue, 2 = liste des pages, 3 = police,
        // puis pour chaque page i : 4 + 2i = la page, 5 + 2i = son contenu.
        var kids = string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => $"{4 + 2 * i} 0 R"));
        var objects = new List<byte[]>
        {
            enc.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            enc.GetBytes($"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>"),
            enc.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>")
        };

        for (var i = 0; i < pages.Count; i++)
        {
            var text = new StringBuilder("BT /F1 10 Tf 14 TL 50 800 Td\n");
            foreach (var line in pages[i])
                text.Append('(').Append(Escape(line)).Append(") Tj T*\n");
            text.Append("ET");
            var content = enc.GetBytes(text.ToString());

            objects.Add(enc.GetBytes(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] " +
                $"/Resources << /Font << /F1 3 0 R >> >> /Contents {5 + 2 * i} 0 R >>"));

            var stream = new List<byte>();
            stream.AddRange(enc.GetBytes($"<< /Length {content.Length} >>\nstream\n"));
            stream.AddRange(content);
            stream.AddRange(enc.GetBytes("\nendstream"));
            objects.Add(stream.ToArray());
        }

        using var ms = new MemoryStream();
        void Write(string s) => ms.Write(enc.GetBytes(s));

        Write("%PDF-1.4\n");
        var offsets = new List<long>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            Write($"{i + 1} 0 obj\n");
            ms.Write(objects[i]);
            Write("\nendobj\n");
        }

        // Table de références : une entrée de 20 octets par objet
        var xref = ms.Position;
        Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            Write($"{offset:D10} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");

        return ms.ToArray();
    }

    // Dans un PDF, \ ( et ) sont spéciaux dans une chaîne de texte.
    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
