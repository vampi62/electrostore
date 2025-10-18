using electrostore.Dto;
using Minio;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace electrostore.Services.FileService;

public class FileService
{
    private readonly IConfiguration _configuration;
    private readonly IMinioClient _minioClient;
    
    public FileService(IConfiguration configuration, IMinioClient minioClient)
    {
        _configuration = configuration;
        _minioClient = minioClient;
    }
    public async Task<GetFileResult> GetFile(string url)
    {
        // if S3 is used, upload the file to S3 and return the url
        if (_configuration.GetValue<bool>("S3:Enable"))
        {
            var bucketName = _configuration.GetValue<string>("S3:BucketName");
            var objectContent = new MemoryStream();
            try
            {
                await _minioClient.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(bucketName)
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
            if (File.Exists(url))
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
                using (var stream = new FileStream(url, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(FileStream);
                }
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
        if (_configuration.GetValue<bool>("S3:Enable"))
        {
            var bucketName = _configuration.GetValue<string>("S3:BucketName");
            var baseS3Url = basePath.Replace(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "") + Path.AltDirectorySeparatorChar;
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
                        .WithBucket(bucketName)
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
                    .WithBucket(bucketName)
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
            while (File.Exists(Path.Combine(basePath, newName)))
            {
                newName = $"{fileName}({i}){fileExt}";
                i++;
            }
            fileUrl = Path.Combine(basePath, newName);
            using (var fileStream = new FileStream(fileUrl, FileMode.Create))
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

    public async Task<SaveFileResult> GenerateThumbnail(string sourcePath, string destPath, int width, int height)
    {
        // if S3 is used, get the file from S3, generate the thumbnail and upload it back to S3
        var file = await GetFile(sourcePath);
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
        IFormFile imageFile = new FormFile(ms, 0, ms.Length, "", Path.GetFileName(destPath))
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
        return await SaveFile(destPath, imageFile);
    }

    public async Task DeleteFile(string url)
    {
        if (_configuration.GetValue<bool>("S3:Enable"))
        {
            var bucketName = _configuration.GetValue<string>("S3:BucketName");
            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(url)
            );
        }
        else
        {
            if (File.Exists(url))
            {
                File.Delete(url);
            }
        }
    }

    public async Task CreateDirectory(string path)
    {
        if (_configuration.GetValue<bool>("S3:Enable"))
        {
            // S3 does not have directories, so we do nothing here
        }
        else
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    public async Task DeleteDirectory(string path)
    {
        if (_configuration.GetValue<bool>("S3:Enable"))
        {
            // S3 removes objects, so we need to list all objects with the given prefix and delete them
            var bucketName = _configuration.GetValue<string>("S3:BucketName");
            var objects = _minioClient.ListObjectsAsync(new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithPrefix(path.Replace("wwwroot/", ""))
                .WithRecursive(true)
            );
            // Collect keys from the IObservable<Item> since it does not support await foreach
            var keys = new List<string>();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var subscription = objects.Subscribe(
                item => { if (item?.Key != null) keys.Add(item.Key); },
                ex => tcs.TrySetException(ex),
                () => tcs.TrySetResult(true)
            );
            await tcs.Task;
            subscription.Dispose();
            foreach (var key in keys)
            {
                await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(key)
                );
            }
        }
        else
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}