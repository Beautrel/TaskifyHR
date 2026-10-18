using System.Globalization;
using System.Text;
using TaskifyHR.Helpers;
using TaskifyHR.Models;

namespace TaskifyHR.Services;

/// <summary>Transforme un rapport mensuel en contenu de fichier (CSV ou lignes de texte pour le PDF).</summary>
public static class ReportBuilder
{
    private static string D(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string MonthName(DateTime month)
    {
        var s = month.ToString("MMMM yyyy", new CultureInfo("fr-FR"));
        return char.ToUpper(s[0]) + s[1..];
    }

    /// <summary>CSV avec séparateur ";" (Excel en français) et BOM UTF-8 (pour que les accents s'affichent bien).</summary>
    public static byte[] ToCsv(MonthlyReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Employé;Type;Début;Fin;Jours ouvrés dans le mois;Statut");
        foreach (var l in report.Lines)
        {
            sb.AppendLine(string.Join(";",
                Escape(l.Employee), Escape(l.Type), D(l.Start), D(l.End),
                l.WorkingDaysInMonth.ToString(CultureInfo.InvariantCulture), FrFormat.Status(l.Status)));
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return utf8.GetPreamble().Concat(utf8.GetBytes(sb.ToString())).ToArray();
    }

    /// <summary>Lignes de texte du PDF (une page A4 en contient environ 48).</summary>
    public static List<string> ToLines(MonthlyReport report)
    {
        var lines = new List<string>
        {
            "Taskify HR - Export mensuel",
            "",
            $"Mois : {MonthName(report.Month)}",
            $"Équipe : {report.Team} - Manager : {report.Manager}",
            $"Généré le {DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}",
            "",
            $"Demandes : {report.Lines.Count}   Jours approuvés : {report.ApprovedDays}   " +
            $"Jours en attente : {report.PendingDays}   Refusées : {report.RefusedCount}",
            "",
            "Employé | Type | Période | Jours ouvrés | Statut",
            "------------------------------------------------------------"
        };

        foreach (var l in report.Lines)
        {
            lines.Add($"{l.Employee} | {l.Type} | {D(l.Start)} - {D(l.End)} | {l.WorkingDaysInMonth} j | {FrFormat.Status(l.Status)}");
        }

        return lines;
    }

    // Entoure de guillemets si le champ contient un séparateur, un guillemet ou un retour à la ligne.
    private static string Escape(string value) =>
        value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
