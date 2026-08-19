using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class Stores : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_store { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_store { get; set; }

    public int xlength_store { get; set; }

    public int ylength_store { get; set; }

    public StorePositionMode position_mode_store { get; set; } = StorePositionMode.Grid;

    [MaxLength(Constants.MaxNameLength)]
    public required string mqtt_name_store { get; set; }

    [Required]
    [Column(TypeName = "varbinary(512)")]
    public byte[] mqtt_password_store { get; set; } = new byte[512];

    [Required]
    [Column(TypeName = "varbinary(16)")]
    public byte[] mqtt_password_encryption_iv_store { get; set; } = new byte[16];

    [Required]
    [Column(TypeName = "varbinary(16)")]
    public byte[] mqtt_password_encryption_tag_store { get; set; } = new byte[16];
    public bool is_mqtt_connected_store { get; set; } = false;

    public DateTime? mqtt_last_seen_store { get; set; }

    public ICollection<Boxs> Boxs { get; set; } = new List<Boxs>();
    public ICollection<Leds> Leds { get; set; } = new List<Leds>();
    public ICollection<StoresTags> StoresTags { get; set; } = new List<StoresTags>();
}