using ElectrostoreIA.Enums;

namespace ElectrostoreIA.Dto;

class TrainRequestMessage
{
    public string action { get; set; } = string.Empty;
    public int id_ia { get; set; }
    public DateTime requested_at { get; set; }
    public int requested_by { get; set; }
}