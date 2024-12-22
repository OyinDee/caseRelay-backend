using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CaseRelayAPI.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadDocumentAsync(IFormFile file);
    }
}
