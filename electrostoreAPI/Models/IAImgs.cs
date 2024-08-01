using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class IAImgs
{
    public int id_ia { get; set; }
    [ForeignKey("id_ia")]
    public IA IA { get; set; }

    public int id_img { get; set; }
    [ForeignKey("id_img")]
    public Imgs Imgs { get; set; }
}