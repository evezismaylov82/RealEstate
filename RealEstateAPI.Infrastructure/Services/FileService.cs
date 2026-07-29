using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Services
{
    public interface IFileService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName);
        Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folderName);
        Task<bool> DeleteImageAsync(string imageUrl);
        bool IsValidImage(IFormFile file);
    }

    public class FileService : IFileService
    {
        private readonly string _uploadPath;
        private readonly long _maxFileSizeInBytes;
        private readonly string[] _allowedExtensions;

        public FileService(string uploadPath, long maxFileSizeInMB, string[] allowedExtensions)
        {
            _uploadPath = uploadPath;
            _maxFileSizeInBytes = maxFileSizeInMB * 1024 * 1024;
            _allowedExtensions = allowedExtensions;

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty");
            }

            if (!IsValidImage(file))
            {
                throw new ArgumentException("Invalid file type or size");
            }

            var folderPath = Path.Combine(_uploadPath, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public async Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folderName)
        {
            var uploadedFiles = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var filePath = await UploadImageAsync(file, folderName);
                    uploadedFiles.Add(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading file {file.FileName}: {ex.Message}");
                }
            }

            return uploadedFiles;
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                var filePath = Path.Combine("wwwroot", imageUrl.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file {imageUrl}: {ex.Message}");
                return false;
            }
        }

        public bool IsValidImage(IFormFile file)
        {
            if (file.Length > _maxFileSizeInBytes)
            {
                return false;
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                return false;
            }

            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return false;
            }

            return true;
        }
    }
}
