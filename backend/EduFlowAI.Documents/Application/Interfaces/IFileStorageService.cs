using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default);
        Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
        Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    }
}