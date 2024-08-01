using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class BoxsTags
{
    public int id_box { get; set; }
    [ForeignKey("id_box")]
    public Boxs Box { get; set; }

    public int id_tag { get; set; }
    [ForeignKey("id_tag")]
    public Tags Tag { get; set; }
}