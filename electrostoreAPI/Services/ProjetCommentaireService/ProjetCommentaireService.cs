using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SessionService;

namespace electrostore.Services.ProjetCommentaireService;

public class ProjetCommentaireService : IProjetCommentaireService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public ProjetCommentaireService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<IEnumerable<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesByProjetId(int projetId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id {projetId} not found");
        }
        var query = _context.ProjetsCommentaires.AsQueryable();
        query = query.Where(p => p.id_projet == projetId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderByDescending(p => p.created_at);
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(p => p.Projet);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(p => p.User);
        }
        var projetCommentaire = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedProjetCommentaireDto>>(projetCommentaire);
    }

    public async Task<int> GetProjetCommentairesCountByProjetId(int projetId)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id {projetId} not found");
        }
        return await _context.ProjetsCommentaires
            .CountAsync(p => p.id_projet == projetId);
    }

    public async Task<IEnumerable<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesByUserId(int userId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        var query = _context.ProjetsCommentaires.AsQueryable();
        query = query.Where(pc => pc.id_user == userId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderByDescending(p => p.created_at);
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(pc => pc.Projet);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(pc => pc.User);
        }
        var projetCommentaire = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedProjetCommentaireDto>>(projetCommentaire);
    }

    public async Task<int> GetProjetCommentairesCountByUserId(int userId)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        return await _context.ProjetsCommentaires
            .CountAsync(p => p.id_user == userId);
    }

    public async Task<ReadExtendedProjetCommentaireDto> GetProjetCommentairesById(int id, int? userId = null, int? projetId = null, List<string>? expand = null)
    {
        var query = _context.ProjetsCommentaires.AsQueryable();
        query = query.Where(pc => pc.id_projet_commentaire == id && (projetId == null || pc.id_projet == projetId) && (userId == null || pc.id_user == userId));
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(pc => pc.Projet);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(pc => pc.User);
        }
        var projetCommentaire = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjetCommentaire with id {id} not found");
        return _mapper.Map<ReadExtendedProjetCommentaireDto>(projetCommentaire);
    }

    public async Task<ReadProjetCommentaireDto> CreateProjetCommentaire(CreateProjetCommentaireDto projetCommentaireDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetCommentaireDto.id_projet))
        {
            throw new KeyNotFoundException($"Projet with id {projetCommentaireDto.id_projet} not found");
        }
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == projetCommentaireDto.id_user))
        {
            throw new KeyNotFoundException($"User with id {projetCommentaireDto.id_user} not found");
        }
        var newProjetCommentaire = new ProjetsCommentaires
        {
            id_projet = projetCommentaireDto.id_projet,
            id_user = projetCommentaireDto.id_user,
            contenu_projet_commentaire = projetCommentaireDto.contenu_projet_commentaire
        };
        _context.ProjetsCommentaires.Add(newProjetCommentaire);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetCommentaireDto>(newProjetCommentaire);
    }

    public async Task<ReadProjetCommentaireDto> UpdateProjetCommentaire(int id, UpdateProjetCommentaireDto projetCommentaireDto, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToUpdate = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaireToUpdate is null) || (projetId is not null && projetCommentaireToUpdate.id_projet != projetId) || (userId is not null && projetCommentaireToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != projetCommentaireToUpdate.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to update this commentaire");
        }
        projetCommentaireToUpdate.contenu_projet_commentaire = projetCommentaireDto.contenu_projet_commentaire ?? projetCommentaireToUpdate.contenu_projet_commentaire;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetCommentaireDto>(projetCommentaireToUpdate);
    }

    public async Task DeleteProjetCommentaire(int id, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToDelete = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaireToDelete is null) || (projetId is not null && projetCommentaireToDelete.id_projet != projetId) || (userId is not null && projetCommentaireToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"ProjetCommentaire with id {id} not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != projetCommentaireToDelete.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to delete this commentaire");
        }
        _context.ProjetsCommentaires.Remove(projetCommentaireToDelete);
        await _context.SaveChangesAsync();
    }
}