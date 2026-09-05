namespace ElectrostoreAPI.Dto;

public record SorterDto
{
    public required string field { get; set; }
    public required string order { get; set; }
}

public record FilterDto
{
    public required string field { get; set; }
    public required string search_type { get; set; }
    public required string value { get; set; }
}