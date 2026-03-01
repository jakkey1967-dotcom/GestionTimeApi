namespace GestionTime.Api.Contracts.Admin;

/// <summary>Respuesta de ejecución de campaña de emails Desktop.</summary>
public sealed class DesktopCampaignRunResponse
{
    public string PeriodKey { get; set; } = "";
    public int Candidates { get; set; }
    public int Enqueued { get; set; }
    public int Skipped { get; set; }
    public bool DryRun { get; set; }
}
