using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Leds
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_led { get; set; }

    public int id_store { get; set; }
    [ForeignKey("id_store")]
    public Stores Store { get; set; }

    public int x_led { get; set; }
    public int y_led { get; set; }
    public int mqtt_led_id { get; set; }
}