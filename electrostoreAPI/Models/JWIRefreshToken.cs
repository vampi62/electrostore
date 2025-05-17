using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;


namespace electrostore.Models;

public class JwiRefreshTokens : BaseEntity
{    
    [Key]
    public Guid id_jwi_refresh { get; set; }

    public Guid session_id { get; set; }

    public DateTime expires_at { get; set; }

    public bool is_revoked { get; set; }


    [MaxLength(Constants.MaxIpLength)]
    public string created_by_ip { get; set; }

    public DateTime? revoked_at { get; set; }

    [MaxLength(Constants.MaxIpLength)]
    public string? revoked_by_ip { get; set; }

    [MaxLength(Constants.MaxReasonLength)]
    public string? revoked_reason { get; set; }

    public int id_user { get; set; }
    [ForeignKey("id_user")]
    public Users User { get; set; }

    public Guid id_jwi_access { get; set; }
    [ForeignKey("id_jwi_access")]
    public JwiAccessTokens JWIAccessToken { get; set; }
}