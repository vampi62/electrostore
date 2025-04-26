using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.IAService;

public class IAService : IIAService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public IAService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadIADto>> GetIA(int limit = 100, int offset = 0, List<int>? idResearch = null)
    {
        var query = _context.IA.AsQueryable();
        if (idResearch != null)
        {
            query = query.Where(b => idResearch.Contains(b.id_ia));
        }
        query = query.Skip(offset).Take(limit);
        var ia = await query.ToListAsync();
        return _mapper.Map<List<ReadIADto>>(ia);
    }

    public async Task<int> GetIACount()
    {
        return await _context.IA.CountAsync();
    }

    public async Task<ReadIADto> GetIAById(int id)
    {
        var ia = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id {id} not found");
        return _mapper.Map<ReadIADto>(ia);
    }

    public async Task<ReadIADto> CreateIA(CreateIADto iaDto)
    {
        var newIA = new IA
        {
            nom_ia = iaDto.nom_ia,
            description_ia = iaDto.description_ia
        };
        _context.IA.Add(newIA);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadIADto>(newIA);
    }

    public async Task<ReadIADto> UpdateIA(int id, UpdateIADto iaDto)
    {
        var iaToUpdate = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id {id} not found");
        if (iaDto.nom_ia is not null)
        {
            iaToUpdate.nom_ia = iaDto.nom_ia;
        }
        if (iaDto.description_ia is not null)
        {
            iaToUpdate.description_ia = iaDto.description_ia;
        }
        // if model exists set trained_ia to true
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras")) && !iaToUpdate.trained_ia)
        {
            iaToUpdate.trained_ia = true;
        }
        else if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras")) && iaToUpdate.trained_ia)
        {
            iaToUpdate.trained_ia = false;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadIADto>(iaToUpdate);
    }

    public async Task DeleteIA(int id)
    {
        var iaToDelete = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id {id} not found");
        // remove model if exists
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras")))
        {
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras"));
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","ItemList" + id.ToString() + ".txt"));
        }

        _context.IA.Remove(iaToDelete);
        await _context.SaveChangesAsync();
    }
}