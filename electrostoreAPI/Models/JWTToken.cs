using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Constants;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class JwtTokens : BaseEntity
{    
    [Key]
    public Guid id_jwi { get; set; }

    public Guid id_user_session { get; set; }
    [ForeignKey("id_user_session")]
    public UsersSessions? Session { get; set; }

    public TokenType token_type { get; set; }

    public DateTime expires_at { get; set; }

    public DateTime? revoked_at { get; set; }

    [MaxLength(FieldLengths.MaxReasonLength)]
    public string? revoked_reason { get; set; }

    public required string auth_method { get; set; }

    [MaxLength(FieldLengths.MaxIpLength)]
    public required string created_by_ip { get; set; }

    [MaxLength(FieldLengths.MaxIpLength)]
    public string? revoked_by_ip { get; set; }
}