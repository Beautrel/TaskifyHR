using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using TaskifyHR.Models;

namespace TaskifyHR.Services;

public class FileExportService : IExportService
{
    /// <summary>Mettre à true (temporairement) pour tester l'état "Export impossible".</summary>
    public bool FailNext { get; set; }

    public async Task<string> CreateFileAsync(MonthlyReport report, ExportFormat format)
    {
        await Task.Delay(600); // laisse voir l'état "Génération en cours"
        if (FailNext)
        {
            FailNext = false;
            throw new IOException("Écriture impossible (simulation)");
        }

        var extension = format == ExportFormat.Pdf ? "pdf" : "csv";
        var path = Path.Combine(FileSystem.CacheDirectory, $"Taskify_Export_{report.Month:yyyy-MM}.{extension}");

        var bytes = format == ExportFormat.Pdf
            ? SimplePdf.Create(ReportBuilder.ToLines(report))
            : ReportBuilder.ToCsv(report);
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    public Task ShareAsync(string filePath) =>
        Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Export mensuel Taskify HR",
            File = new ShareFile(filePath)
        });
}
