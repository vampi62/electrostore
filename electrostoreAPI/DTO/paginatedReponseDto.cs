namespace electrostore.Dto;

public record PaginationDto
{
    public int total { get; set; }
    public int nextOffset { get; set; }
    public bool hasMore { get; set; }
}

public record PaginatedResponseDto<T>
{
    public required IEnumerable<T> data { get; set; }
    public required PaginationDto pagination { get; set; }
    public required IEnumerable<string>? filter { get; set; }
    public required IEnumerable<string>? sort { get; set; }
}