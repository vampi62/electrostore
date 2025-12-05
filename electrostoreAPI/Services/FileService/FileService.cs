using electrostore.Dto;
using Minio;
using Minio.DataModel.Args;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace electrostore.Services.FileService;

public class FileService : IFileService
{
    private readonly IMinioClient _minioClient;
    private readonly bool _s3Enabled;
    private readonly string _s3BucketName;
    private readonly string _localFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    
    public FileService(IConfiguration configuration, IMinioClient minioClient)
    {
        _minioClient = minioClient;
        _s3Enabled = configuration.GetValue<bool>("S3:Enable");
        _s3BucketName = configuration.GetValue<string>("S3:BucketName") ?? "electrostore-bucket";
    }
    public async Task<GetFileResult> GetFile(string url)
    {
        // if S3 is used, upload the file to S3 and return the url
        if (_s3Enabled)
        {
            var objectContent = new MemoryStream();
            try
            {
                await _minioClient.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(_s3BucketName)
                    .WithObject(url)
                    .WithCallbackStream(stream => stream.CopyTo(objectContent))
                );
                objectContent.Position = 0;
                // try to get the mime type from the file extension
                var ext = Path.GetExtension(url).ToLower();
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
            var localPath = Path.Combine(_localFilesPath, url);
            if (File.Exists(localPath))
            {
                // try to get the mime type from the file extension
                var ext = Path.GetExtension(url).ToLower();
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

    public async Task<SaveFileResult> SaveFile(string basePath, IFormFile file)
    {
        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
        fileName = fileName.Replace(".", "").Replace("/", ""); // remove "." and "/" from the file name to prevent directory traversal attacks
        if (fileName.Length > 100) // cut the file name to 100 characters to prevent too long file names
        {
            fileName = fileName[..100];
        }
        var fileExt = Path.GetExtension(file.FileName);
        var newName = fileName + fileExt;
        var i = 1;
        string fileUrl;
        // if S3 is used, upload the file to S3 and return the url
        if (_s3Enabled)
        {
            var baseS3Url = basePath + Path.AltDirectorySeparatorChar;
            if (baseS3Url.StartsWith(Path.AltDirectorySeparatorChar))
            {
                baseS3Url = baseS3Url[1..];
            }
            fileUrl = baseS3Url + newName;
            // verifie si un document avec le meme nom existe deja sur le serveur S3
            // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
            while (true)
            {
                try
                {
                    await _minioClient.StatObjectAsync(new StatObjectArgs()
                        .WithBucket(_s3BucketName)
                        .WithObject(fileUrl)
                    );
                    // si on arrive ici, c'est que le fichier existe deja
                    newName = $"{fileName}({i}){fileExt}";
                    fileUrl = baseS3Url + newName;
                    if (fileUrl.StartsWith(Path.AltDirectorySeparatorChar))
                    {
                        fileUrl = fileUrl[1..];
                    }
                    i++;
                }
                catch (Minio.Exceptions.ObjectNotFoundException)
                {
                    // le fichier n'existe pas, on peut sortir de la boucle
                    break;
                }
            }
            using (var uploadStream = new MemoryStream())
            {
                await file.CopyToAsync(uploadStream);
                uploadStream.Position = 0;
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(_s3BucketName)
                    .WithObject(fileUrl)
                    .WithStreamData(uploadStream)
                    .WithObjectSize(uploadStream.Length)
                    .WithContentType(file.ContentType)
                );
            }
        }
        else
        {
            // verifie si un document avec le meme nom existe deja sur le serveur dans "wwwroot/commandDocuments"
            // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
            while (File.Exists(Path.Combine(_localFilesPath, basePath, newName)))
            {
                newName = $"{fileName}({i}){fileExt}";
                i++;
            }
            fileUrl = Path.Combine(basePath, newName);
            var localPath = Path.Combine(_localFilesPath, fileUrl);
            using (var fileStream = new FileStream(localPath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
        }
        return new SaveFileResult
        {
            url = fileUrl,
            mimeType = file.ContentType
        };
    }

    public async Task<SaveFileResult> GenerateThumbnail(string sourceFilePath, string destPath, int width, int height)
    {
        // if S3 is used, get the file from S3, generate the thumbnail and upload it back to S3
        var file = await GetFile(sourceFilePath);
        if (!file.Success || file.FileStream == null)
        {
            return new SaveFileResult
            {
                url = "",
                mimeType = ""
            };
        }
        var image = await Image.LoadAsync(file.FileStream);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max
        }));
        // convert Image to IFormFile
        var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);
        ms.Position = 0;
        IFormFile imageFile = new FormFile(ms, 0, ms.Length, "", Path.GetFileName(sourceFilePath))
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
        return await SaveFile(destPath, imageFile);
    }

    public async Task DeleteFile(string url)
    {
        if (_s3Enabled)
        {
            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_s3BucketName)
                .WithObject(url)
            );
        }
        else
        {
            var localPath = Path.Combine(_localFilesPath, url);
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