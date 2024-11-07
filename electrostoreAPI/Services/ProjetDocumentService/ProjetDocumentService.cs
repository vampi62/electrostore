using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetDocumentService;

public class ProjetDocumentService : IProjetDocumentService
{
    private readonly ApplicationDbContext _context;

    public ProjetDocumentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadProjetDocumentDto>> GetProjetDocumentsByProjetId(int projetId, int limit = 100, int offset = 0)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id {projetId} not found");
        }
        return await _context.ProjetsDocuments
            .Where(id => id.id_projet == projetId)
            .Skip(offset)
            .Take(limit)
            .Select(projetDocument => new ReadProjetDocumentDto
            {
                id_projet_document = projetDocument.id_projet_document,
                id_projet = projetDocument.id_projet,
                url_projet_document = projetDocument.url_projet_document,
                name_projet_document = projetDocument.name_projet_document,
                type_projet_document = projetDocument.type_projet_document,
                size_projet_document = projetDocument.size_projet_document,
                date_projet_document = projetDocument.date_projet_document
            })
            .ToListAsync();
    }

    public async Task<ReadProjetDocumentDto> GetProjetDocumentById(int id, int? projetId = null)
    {
        var projetDocument = await _context.ProjetsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjetDocument with id {id} not found");
        if (projetId != null && projetDocument.id_projet != projetId)
        {
            throw new KeyNotFoundException($"ProjetDocument with id {id} not found for projet with id {projetId}");
        }
        return new ReadProjetDocumentDto
        {
                id_projet_document = projetDocument.id_projet_document,
                id_projet = projetDocument.id_projet,
                url_projet_document = projetDocument.url_projet_document,
                name_projet_document = projetDocument.name_projet_document,
                type_projet_document = projetDocument.type_projet_document,
                size_projet_document = projetDocument.size_projet_document,
                date_projet_document = projetDocument.date_projet_document
        };
    }

    public async Task<ReadProjetDocumentDto> CreateProjetDocument(CreateProjetDocumentDto projetDocumentDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetDocumentDto.id_projet))
        {
            throw new KeyNotFoundException($"Projet with id {projetDocumentDto.id_projet} not found");
        }
        if (projetDocumentDto.document == null || projetDocumentDto.document.Length == 0)
        {
            throw new ArgumentException("Image file is required");
        }
        if (projetDocumentDto.document.Length > (30 * 1024 * 1024)) // 30MB max
        {
            throw new ArgumentException("Image file size should not exceed 30MB");
        }
        var fileName = Path.GetFileNameWithoutExtension(projetDocumentDto.document.FileName);
        var fileExt = Path.GetExtension(projetDocumentDto.document.FileName);
        if (!new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" }.Contains(fileExt)) // if extension is not allowed
        {
            throw new ArgumentException("Document file extension not allowed");
        }
        var i = 1;
        // verifie si un document avec le meme nom existe deja sur le serveur dans "wwwroot/projetDocuments"
        // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
        var newName = projetDocumentDto.document.FileName;
        while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", projetDocumentDto.id_projet.ToString(), newName)))
        {
            newName = $"{fileName}({i}){fileExt}";
            i++;
        }
        var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", projetDocumentDto.id_projet.ToString(), newName);
        using (var fileStream = new FileStream(savePath, FileMode.Create))
        {
            await projetDocumentDto.document.CopyToAsync(fileStream);
        }
        var projetDocument = new ProjetsDocuments
        {
            id_projet = projetDocumentDto.id_projet,
            url_projet_document = projetDocumentDto.id_projet + "/" + newName,
            name_projet_document = projetDocumentDto.name_projet_document,
            type_projet_document = projetDocumentDto.type_projet_document,
            size_projet_document = projetDocumentDto.document.Length,
            date_projet_document = DateTime.Now
        };
        await _context.ProjetsDocuments.AddAsync(projetDocument);
        await _context.SaveChangesAsync();
        return new ReadProjetDocumentDto
        {
            id_projet_document = projetDocument.id_projet_document,
            id_projet = projetDocument.id_projet,
            url_projet_document = projetDocument.url_projet_document,
            name_projet_document = projetDocument.name_projet_document,
            type_projet_document = projetDocument.type_projet_document,
            size_projet_document = projetDocument.size_projet_document,
            date_projet_document = projetDocument.date_projet_document
        };
    }

    public async Task<ReadProjetDocumentDto> UpdateProjetDocument(int id, UpdateProjetDocumentDto projetDocumentDto, int? projetId = null)
    {
        var projetDocument = await _context.ProjetsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjetDocument with id {id} not found");
        if (projetId != null && projetDocument.id_projet != projetId)
        {
            throw new KeyNotFoundException($"ProjetDocument with id {id} not found for projet with id {projetId}");
        }
        if (projetDocumentDto.document != null && projetDocumentDto.document.Length > 0)
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", projetDocument.id_projet.ToString(), projetDocument.url_projet_document);
            if (projetDocumentDto.document.Length > (30 * 1024 * 1024)) // 30MB max
            {
                throw new ArgumentException("Image file size should not exceed 30MB");
            }
            var fileName = Path.GetFileNameWithoutExtension(projetDocumentDto.document.FileName);
            var fileExt = Path.GetExtension(projetDocumentDto.document.FileName);
            if (!new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" }.Contains(fileExt)) // if extension is not allowed
            {
                throw new ArgumentException("Document file extension not allowed");
            }
            var i = 1;
            // verifie si un document avec le meme nom existe deja sur le serveur dans "wwwroot/projetDocuments"
            // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
            var newName = projetDocumentDto.document.FileName;
            while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", projetDocument.id_projet.ToString(), newName)))
            {
                newName = $"{fileName}({i}){fileExt}";
                i++;
            }
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", projetDocument.id_projet.ToString(), newName);
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await projetDocumentDto.document.CopyToAsync(fileStream);
            }
            projetDocument.url_projet_document = projetDocument.id_projet + "/" + newName;
            projetDocument.size_projet_document = projetDocumentDto.document.Length;
            projetDocument.date_projet_document = DateTime.Now;
            // remove old file
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }
        if (projetDocumentDto.name_projet_document != null)
        {
            projetDocument.name_projet_document = projetDocumentDto.name_projet_document;
        }
        if (projetDocumentDto.type_projet_document != null)
        {
            projetDocument.type_projet_document = projetDocumentDto.type_projet_document;
        }
        await _context.SaveChangesAsync();
        return new ReadProjetDocumentDto
        {
            id_projet_document = projetDocument.id_projet_document,
            id_projet = projetDocument.id_projet,
            url_projet_document = projetDocument.url_projet_document,
            name_projet_document = projetDocument.name_projet_document,
            type_projet_document = projetDocument.type_projet_document,
            size_projet_document = projetDocument.size_projet_document,
            date_projet_document = projetDocument.date_projet_document
        };
    }

    public async Task DeleteProjetDocument(int id, int? projetId = null)
    {
        var projetDocument = await _context.ProjetsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjetDocument with id {id} not found");
        if (projetId != null && projetDocument.id_projet != projetId)
        {
            throw new KeyNotFoundException($"ProjetDocument with id {id} not found for projet with id {projetId}");
        }
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", projetDocument.id_projet.ToString(), projetDocument.url_projet_document);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        _context.ProjetsDocuments.Remove(projetDocument);
        await _context.SaveChangesAsync();
    }

    public async Task<GetFileResult> GetFile(string url)
    {
        var pathImg = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", url);
        if (!File.Exists(pathImg))
        {
            return new GetFileResult
            {
                Success = false,
                ErrorMessage = "File not found",
                FilePath = "",
                MimeType = ""
            };
        } else {
            var ext = Path.GetExtension(pathImg);
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
            return await Task.FromResult(new GetFileResult
            {
                Success = true,
                FilePath = pathImg,
                MimeType = mimeType
            });
        }
    }
}