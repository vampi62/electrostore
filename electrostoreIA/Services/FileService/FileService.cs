using ElectrostoreIA.Dto;
using Minio;
using Minio.DataModel.Args;

namespace ElectrostoreIA.Services.FileService;

public class FileService : IFileService
{
    private readonly IMinioClient? _minioClient;
    private readonly bool _s3Enabled;
    private readonly string _s3BucketName;
    private readonly string _localFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    
    public FileService(IConfiguration configuration, IMinioClient? minioClient = null)
    {
        _minioClient = minioClient;
        _s3Enabled = configuration.GetValue<bool>("S3:Enable");
        _s3BucketName = configuration.GetValue<string>("S3:BucketName") ?? "ElectrostoreIA-bucket";
    }
    public async Task<GetFileResult> GetFile(string path)
    {
        // if S3 is used, upload the file to S3 and return the path
        if (_s3Enabled)
        {
            if (_minioClient == null)
            {
                return new GetFileResult
                {
                    Success = false,
                    ErrorMessage = "S3 is enabled but MinIO client is not configured",
                    MimeType = ""
                };
            }
            var objectContent = new MemoryStream();
            try
            {
                await _minioClient.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(_s3BucketName)
                    .WithObject(path)
                    .WithCallbackStream(stream => stream.CopyTo(objectContent))
                );
                objectContent.Position = 0;
                // try to get the mime type from the file extension
                var ext = Path.GetExtension(path).ToLower();
                var mimeType = ext switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".ppt" => "application/vnd.ms-powerpoint",
                    ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                    ".txt" => "text/plain",
                    _ => "application/octet-stream"
                };
                return new GetFileResult
                {
                    Success = true,
                    FileStream = objectContent,
                    MimeType = mimeType
                };
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                return new GetFileResult
                {
                    Success = false,
                    ErrorMessage = "File not found",
                    MimeType = ""
                };
            }
        }
        else
        {
            var localPath = Path.Combine(_localFilesPath, path);
            if (File.Exists(localPath))
            {
                // try to get the mime type from the file extension
                var ext = Path.GetExtension(path).ToLower();
                var mimeType = ext switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".ppt" => "application/vnd.ms-powerpoint",
                    ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                    ".txt" => "text/plain",
                    _ => "application/octet-stream"
                };
                var FileStream = new MemoryStream();
                using (var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(FileStream);
                }
                FileStream.Position = 0;
                return new GetFileResult
                {
                    Success = true,
                    FileStream = FileStream,
                    MimeType = mimeType
                };
            }
            else
            {
                return new GetFileResult
                {
                    Success = false,
                    ErrorMessage = "File not found",
                    MimeType = ""
                };
            }
        }
    }

    public async Task<bool> FileExists(string path)
    {
        if (_s3Enabled)
        {
            if (_minioClient == null)
            {
                return false;
            }
            try
            {
                await _minioClient.StatObjectAsync(new StatObjectArgs()
                    .WithBucket(_s3BucketName)
                    .WithObject(path)
                );
                return true;
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                return false;
            }
        }
        else
        {
            var localPath = Path.Combine(_localFilesPath, path);
            return File.Exists(localPath);
        }
    }

    public async Task<SaveFileResult> SaveFile(string basePath, string fullFileName, string contentType, Stream data, bool overwrite = false)
    {
        var fileName = Path.GetFileNameWithoutExtension(fullFileName);
        fileName = fileName.Replace(".", "").Replace("/", ""); // remove "." and "/" from the file name to prevent directory traversal attacks
        if (fileName.Length > 100) // cut the file name to 100 characters to prevent too long file names
        {
            fileName = fileName[..100];
        }
        var fileExt = Path.GetExtension(fullFileName);
        var newName = fileName + fileExt;
        var i = 1;
        string filePath;
        // if S3 is used, upload the file to S3 and return the path
        if (_s3Enabled)
        {
            if (_minioClient == null)
            {
                throw new InvalidOperationException("S3 is enabled but MinIO client is not configured");
            }
            var baseS3Url = basePath + Path.AltDirectorySeparatorChar;
            if (baseS3Url.StartsWith(Path.AltDirectorySeparatorChar))
            {
                baseS3Url = baseS3Url[1..];
            }
            filePath = baseS3Url + newName;
            while (!overwrite)
            {
                try
                {
                    await _minioClient.StatObjectAsync(new StatObjectArgs()
                        .WithBucket(_s3BucketName)
                        .WithObject(filePath)
                    );
                    newName = $"{fileName}({i}){fileExt}";
                    filePath = baseS3Url + newName;
                    if (filePath.StartsWith(Path.AltDirectorySeparatorChar))
                    {
                        filePath = filePath[1..];
                    }
                    i++;
                }
                catch (Minio.Exceptions.ObjectNotFoundException)
                {
                    break;
                }
            }
            using (var uploadStream = new MemoryStream())
            {
                await data.CopyToAsync(uploadStream);
                uploadStream.Position = 0;
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(_s3BucketName)
                    .WithObject(filePath)
                    .WithStreamData(uploadStream)
                    .WithObjectSize(uploadStream.Length)
                    .WithContentType(contentType)
                );
            }
        }
        else
        {
            if (!overwrite)
            {
                while (File.Exists(Path.Combine(_localFilesPath, basePath, newName)))
                {
                    newName = $"{fileName}({i}){fileExt}";
                    i++;
                }
            }
            filePath = Path.Combine(basePath, newName);
            var localPath = Path.Combine(_localFilesPath, filePath);
            using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
            {
                await data.CopyToAsync(fileStream);
            }
        }
        return new SaveFileResult
        {
            path = filePath,
            mimeType = contentType
        };
    }

    public async Task DeleteFile(string path)
    {
        if (_s3Enabled)
        {
            if (_minioClient == null)
            {
                throw new InvalidOperationException("S3 is enabled but MinIO client is not configured");
            }
            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_s3BucketName)
                .WithObject(path)
            );
        }
        else
        {
            var localPath = Path.Combine(_localFilesPath, path);
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }
        }
    }

    public async Task CreateDirectory(string path)
    {
        if (_s3Enabled)
        {
            // S3 does not have directories, so we do nothing here
        }
        else
        {
            var localPath = Path.Combine(_localFilesPath, path);
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
        }
    }

    public async Task DeleteDirectory(string path)
    {
        if (_s3Enabled)
        {
            if (_minioClient == null)
            {
                throw new InvalidOperationException("S3 is enabled but MinIO client is not configured");
            }
            // S3 removes objects, so we need to list all objects with the given prefix and delete them
            var listArgs = new ListObjectsArgs()
                .WithBucket(_s3BucketName)
                .WithPrefix(path)
                .WithRecursive(true);
            // Collect keys from the IAsyncEnumerable<Item>
            var keys = new List<string>();
            await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs))
            {
                if (item?.Key != null)
                {
                    keys.Add(item.Key);
                }
            }
            foreach (var key in keys)
            {
                await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(_s3BucketName)
                    .WithObject(key)
                );
            }
        }
        else
        {
            var localPath = Path.Combine(_localFilesPath, path);
            if (Directory.Exists(localPath))
            {
                Directory.Delete(localPath, true);
            }
        }
    }
}