using jsreport.Binary;
using jsreport.Local;
using Newtonsoft.Json;
using Proser.DryCalibration.Report.interfaces;
using System;
using System.IO;

namespace Proser.DryCalibration.Report
{
    public class ReportApp : IReport
    {
        public string Generate(ReportModel report, string reportPath)
        {
            try
            {
                // generar los datos para el reporte
                string jsonData = JsonConvert.SerializeObject(report);

                var rs = new LocalReporting()
                    .RunInDirectory(Path.Combine(Directory.GetCurrentDirectory(), "jsreport"))
                    .KillRunningJsReportProcesses()
                    .UseBinary(JsReportBinary.GetBinary())
                    .Configure(cfg => cfg.AllowedLocalFilesAccess().FileSystemStore().BaseUrlAsWorkingDirectory())
                    .AsUtility()
                    .Create();

                string reportName = string.Format("IT_{0}_{1}.pdf", report.Header.CalibrationInformation.ReportNumber, DateTime.Now.ToString("dd-MM-yyyy_HH_mm_ss"));
                string fullReportPath = Path.Combine(reportPath, reportName);

                Console.WriteLine(string.Format("Rendering {0}", reportName));

                var certificado = rs.RenderByNameAsync("certificado", jsonData).Result;

                if (!Directory.Exists(reportPath))
                {
                    Directory.CreateDirectory(reportPath);
                }

                certificado.Content.CopyTo(File.OpenWrite(fullReportPath));

                return fullReportPath;
            }
            catch(Exception e)
            {
                
                return string.Empty; 
            }
        }
    }
}
