using ClosedXML.Excel;
using InventoryReconciliation.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace InventoryReconciliation.Infrastructure.Imports;

public sealed class FileUploadedWorkbookStorage(IConfiguration configuration, IHostEnvironment environment) : IUploadedWorkbookStorage
{
    private const long MaxUploadBytes = 50 * 1024 * 1024;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileInfo GetWorkbookFile()
    {
        var configuredPath = configuration["InventorySource:UploadedWorkbookPath"];
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("App_Data", "uploads", "active-inventory.xlsx")
            : Environment.ExpandEnvironmentVariables(configuredPath);

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(environment.ContentRootPath, path);
        }

        return new FileInfo(path);
    }

    public async Task<UploadedWorkbookSaveResult> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        if (!Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Upload a valid .xlsx workbook.");
        }

        var target = GetWorkbookFile();
        Directory.CreateDirectory(target.DirectoryName!);
        var tempPath = Path.Combine(target.DirectoryName!, $"upload-{Guid.NewGuid():N}.xlsx");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using (var tempStream = File.Create(tempPath))
            {
                await content.CopyToAsync(tempStream, cancellationToken);
            }

            var tempFile = new FileInfo(tempPath);
            if (tempFile.Length == 0)
            {
                throw new InvalidOperationException("The selected workbook is empty.");
            }

            if (tempFile.Length > MaxUploadBytes)
            {
                throw new InvalidOperationException("Workbook exceeds the 50 MB upload limit.");
            }

            ValidateWorkbook(tempPath);
            File.Copy(tempPath, target.FullName, overwrite: true);

            return new UploadedWorkbookSaveResult(
                fileName,
                target.FullName,
                tempFile.Length,
                DateTimeOffset.UtcNow);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            _lock.Release();
        }
    }

    private static void ValidateWorkbook(string path)
    {
        try
        {
            using var workbook = new XLWorkbook(path);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet?.RangeUsed() is null)
            {
                throw new InvalidOperationException("The workbook does not contain any usable rows.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("The selected file could not be opened as a valid .xlsx workbook.", exception);
        }
    }
}
