namespace Greenhouse.API.ViewModels;

public class DashboardViewModel
{
    public int EstufasAtivas { get; set; }
    public int SensoresOnline { get; set; }
    public int AlertasPendentes { get; set; }
    public double MediaTemperatura { get; set; }
}
