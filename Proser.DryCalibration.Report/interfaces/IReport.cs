namespace Proser.DryCalibration.Report.interfaces
{
    public interface IReport
    {
        string Generate(ReportModel report, string reportPath);
    }
}
