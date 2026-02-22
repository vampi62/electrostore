namespace electrostore.Dto;

public record PaginationDto
{
    public int limit { get; set; }
    public int offset { get; set; }
    public int total { get; set; }
    public int nextOffset { get; set; }
    public bool hasMore { get; set; }
}

public record PaginatedResponseDto<T>
{
    public required IEnumerable<T> data { get; set; }
    public required PaginationDto pagination { get; set; }
    public required IEnumerable<FilterDto>? filters { get; set; }
    public required IEnumerable<SorterDto>? sort { get; set; }
}