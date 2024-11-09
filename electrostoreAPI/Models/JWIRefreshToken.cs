using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace electrostore.Models;

public class JWIRefreshToken
{    
    [Key]
    public Guid id_jwi_refresh { get; set; }
    public DateTime expires_at { get; set; }
    public bool is_revoked { get; set; }

    public DateTime created_at { get; set; }
    public string created_by_ip { get; set; }
    public DateTime? revoked_at { get; set; }
    public string? revoked_by_ip { get; set; }

    public int id_user { get; set; }
    [ForeignKey("id_user")]
    public Users User { get; set; }
}