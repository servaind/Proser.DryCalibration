using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.Report
{
    public class ReportModel
    {
        public ReportHeader Header { get; set; }
        public ReportBody Body { get; set; }

        public ReportModel()
        {
            Header = new ReportHeader();
            Body = new ReportBody();
        }
    }

    public class ReportHeader
    {
        public Measurer Measurer { get; set; }
        public Petitioner Petitioner { get; set; }
        public CalibrationInformation CalibrationInformation { get; set; }
        public EnvironmentalCondition EnvironmentalCondition { get; set; }

        public ReportHeader()
        {
            Measurer = new Measurer();
            Petitioner = new Petitioner();
            CalibrationInformation = new CalibrationInformation();
            EnvironmentalCondition = new EnvironmentalCondition();
        }
    }

    public class Measurer
    {
        public string Brand { get; set; }
        public string Maker { get; set; }
        public string Model { get; set; }
        public string DN { get; set; }
        public string Sch { get; set; }
        public string Serie { get; set; }
        public string SerieNumber { get; set; }
        public string Identification { get; set; }
        public string FirmwareVersion { get; set; }
    }

    public class Petitioner
    {
        public string BusinessName { get; set; }
        public string BusinessAddress { get; set; }
        public string RealizationPlace { get; set; }
        public string RealizationAddress { get; set; }
    }

    public class CalibrationInformation
    {
        public string ReportNumber { get; set; }
        public string Responsible { get; set; }
        public string CalibrationDate { get; set; }
        public string EmitionDate { get; set; }
        public string CalibrationObject { get; set; }
        public string RequiredDetermination { get; set; }
    }

    public class EnvironmentalCondition
    {
        public string AtmosphericPressure { get; set; }
        public string EnvironmentTemperature { get; set; }
        public string EnvironmentTempDifference { get; set; }
    }

    public class ReportBody
    {
        public CalibrationTerms CalibrationTerms { get; set; }
        public GainResult GainResults { get; set; }
        public FlowSpeedResult FlowSpeedResults { get; set; }
        public SoundSpeedResult SoundSpeedResults { get; set; }
        public CalibrationMeasuring CalibrationMeasuring { get; set; }

        public ReportBody()
        {
            CalibrationTerms = new CalibrationTerms();
            FlowSpeedResults = new FlowSpeedResult();
            SoundSpeedResults = new SoundSpeedResult();
        }
    }

    public class CalibrationTerms
    {
        public string ReferenceFlow { get; set; }
        public string TemperatureAverage { get; set; }
        public string PressureAverage { get; set; }
        public string PressureAverageUncertainty { get; set; }
        public string PressureSensorType { get; set; }
        public string TemperatureUncertainty { get; set; }
        public string TemperatureDifference { get; set; }
        public string Gradiente { get; set; }
        public string EfficiencyAverage { get; set; }
        public string Duration { get; set; }
        
    }

    public class GainResult
    {
        public List<RopeResult> AverageResults { get; set; }

        public GainResult()
        {
            AverageResults = new List<RopeResult>();
        }
    }



    public class FlowSpeedResult
    {
        public List<RopeResult> AverageResults { get; set; }

        public FlowSpeedResult()
        {
            AverageResults = new List<RopeResult>();
        }
    }

    public class SoundSpeedResult
    {
        public List<RopeResult> AverageResults { get; set; }

        public string TheoreticalSoundSpeed { get; set; }
        public string SoundSpeedValMax { get; set; }
        public string SoundSpeedValMin { get; set; }
        public string SoundSpeedDifferece { get; set; }

        public SoundSpeedResult()
        {
            AverageResults = new List<RopeResult>();
        }
    }

    public class RopeResult
    {
        public string Name { get; set; }
        public string Min { get; set; }
        public string Max { get; set; }
        public string Value { get; set; }
        public string Uncertainty { get; set; }
        public string Error { get; set; }
    }

  

    public class CalibrationMeasuring
    {
        public List<ReportMeasuringInstrument> MeasuringInstruments { get; set; }
        public string Observations { get; set; }

        public CalibrationMeasuring()
        {
            MeasuringInstruments = new List<ReportMeasuringInstrument>();
        }
    }

    public class ReportMeasuringInstrument
    {
        public string BrandName { get; set; }
        public string InternalIdentification { get; set; }
        public string CalibrationCode { get; set; }
    }
}


