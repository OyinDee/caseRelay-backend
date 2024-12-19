using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System.Net;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private const int MaxFileSizeMB = 50; // Maximum file size limit
    private readonly string[] AllowedFileTypes = { "pdf", "docx", "jpg", "png" };

    public CloudinaryService(string cloudName, string apiKey, string apiSecret)
    {
        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }
public bool ValidateFileSize(IFormFile file, long maxSizeInBytes)
{
    return file.Length <= maxSizeInBytes;
}

public async Task<string> UploadDocumentAsync(IFormFile file)
{
    ValidateFileSize(file, 10 * 1024 * 1024); // 10 MB limit


    if (file.Length > 10 * 1024 * 1024) // Hard limit for Cloudinary free plan
        throw new Exception($"File size too large. Got {file.Length} bytes. Maximum is 10 MB. " +
                            "Upgrade your plan to enjoy higher limits: https://www.cloudinary.com/pricing/upgrades/file-limit");

    try
    {
        using var stream = file.OpenReadStream();
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "documents"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
        {
            var errorMessage = uploadResult.Error?.Message ?? "Unknown upload error";
            throw new Exception($"Cloudinary upload failed: {errorMessage}");
        }

        return uploadResult.SecureUrl.ToString();
    }
    catch (Exception ex)
    {
        throw new Exception($"File upload failed: {ex.Message}");
    }
}

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length <= 0)
            throw new ArgumentException("File is empty or null.");

        if (file.Length > MaxFileSizeMB * 1024 * 1024)
            throw new ArgumentException($"File exceeds maximum size of {MaxFileSizeMB} MB.");

        var fileExtension = GetFileExtension(file.FileName);
        if (!AllowedFileTypes.Contains(fileExtension))
            throw new ArgumentException($"File type '{fileExtension}' is not allowed.");
    }

    private string GetFileExtension(string fileName)
    {
        return fileName.Split('.').Last().ToLower();
    }
}
