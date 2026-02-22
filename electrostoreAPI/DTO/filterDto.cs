namespace electrostore.Dto;

public record SorterDto
{
    public required string Field { get; set; }
    public required string Order { get; set; }
}

public record FilterDto
{
    public required string Field { get; set; }
    public required string SearchType { get; set; }
    public required string Value { get; set; }
}