using p48_lessons_asp.Services.Slug;

namespace p48_lessons_asp.Services.StorageService;

public class ServerFileStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ServerFileStorageService> _logger;
    private readonly ISlugService _slugService;

    public ServerFileStorageService(IWebHostEnvironment env, ILogger<ServerFileStorageService> logger, ISlugService slugService)
    {
        _env = env;
        _logger = logger;
        _slugService = slugService;
    }


     public async Task<string> SaveFileAsync(string entityType, string entityName, IFormFile file, string fileName = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is not send or empty");
            }
            var slugEntityType = _slugService.GetSlug(entityType); 
            var slugEntityName = _slugService.GetSlug(entityName); 

            var uploadFolderPath = Path.Combine(_env.WebRootPath, "images", slugEntityType, slugEntityName);

            if (!Directory.Exists(uploadFolderPath)) Directory.CreateDirectory(uploadFolderPath);

            var extension = Path.GetExtension(file.FileName);
            string resultFileName;

            if (!string.IsNullOrEmpty(fileName)) resultFileName = $"{fileName}{extension}";
            else resultFileName = $"{Guid.NewGuid()}{extension}";

            var filePath = Path.Combine(uploadFolderPath, resultFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/{slugEntityType}/{slugEntityName}/{resultFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while try to save file");
            throw new Exception($"Error while try to save file: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string url)
    {
        try
        {
            if (url == null || url == "" || _env.WebRootPath == null || _env.WebRootPath == "")
            {
                return false;
            }

            var cleanUrl = url.TrimStart('/');
            var relativePath = cleanUrl.Replace('/', '\\');
            

            var webRootFullPath = Path.GetFullPath(_env.WebRootPath);
            var fullFilePath = Path.GetFullPath(Path.Combine(webRootFullPath, relativePath));

            if (!fullFilePath.StartsWith(_env.WebRootPath))
            {
                _logger.LogWarning("Attempt to delete file outside WebRootPath: {Url}", url);
                return false;
            }

            if (!File.Exists(fullFilePath))
            {
                _logger.LogWarning("File not found on disk: {FilePath}", fullFilePath);
                return false;
            }

            File.Delete(fullFilePath);
            _logger.LogInformation("File deleted from disk: {FilePath}", fullFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting file by url: {Url}", url);
            return false;
        }
    }

   
}