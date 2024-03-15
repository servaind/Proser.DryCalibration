using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proser.DryCalibration.controller;

namespace Proser.DryCalibration.controller.data.interfaces
{
    public interface IDataSql
    {
        bool AddReport(model.Report report);
        bool DeleteReport(int ReportId);
        bool UpdateReport(model.Report report);
        model.Report GetReport(int reportId);
        List<model.Report> GetReportList();

        bool AddUltrasonic(model.Ultrasonic ultrasonic);
        bool DeleteUltrasonic(int ultrasId);
        bool UpdateUltrasonic(model.Ultrasonic ultrasonic);
        model.Ultrasonic GetUltrasonic(int ultrasonicId);

        bool AddCustomer(model.Customer customer);
        bool DeleteCustomer(int customerId);
        bool UpdateCustomer(model.Customer customer);
        model.Customer GetCustomer(int customerId);

        bool AddPlace(model.Place place);
        bool DeletePlace(int placeId);
        bool UpdatePlace(model.Place place);
        model.Place GetPlace(int placeId);

        bool AddEnvCondition(model.EnvironmentCondition condition);
        bool DeleteEnvCondition(int reportId);
        bool UpddateEnvCondition(model.EnvironmentCondition condition);
        model.EnvironmentCondition GetEnvCondition(int reportId);

        bool AddResponsible(model.Responsible responsible);
        bool DeleteResponsible(int reportId);
        bool UpdateResponsible(model.Responsible responsible);
        List<model.Responsible> GetResponsibleList(int reportId);

        bool AddReportEquipment(model.ReportEquipment equipment);
        bool DeleteReportEquipment(int reportId);
        bool UpdateReportEquipment(model.ReportEquipment equipment);
        List<model.ReportEquipment> GetReportEquipmentList(int reportId);

        bool AddCalculation(model.Calculation calculation);
        bool DeleteCalculation(int reportId);
        bool UpdateCalculation(model.Calculation calculation);
        model.Calculation GetCalcultaion(int reportId);

        bool AddUncertainty(model.Uncertainty uncertainty);
        bool DeleteUncertainty(int calculationId);
        bool UpdateUncertainty(model.Uncertainty uncertainty);
        List<model.Uncertainty> GetUncertaintyList(int calculationId);

        bool AddRopeError(model.RopeError ropeError);
        bool DeleteRopeError(int calculationId);
        bool UpdateRopeError(model.RopeError ropeError);
        List<model.RopeError> GetRopeErrorList(int calculationId);

        bool AddReportSample(model.ReportSample reportSample);
        bool DeleteReportSample(int reportId);
        bool UpdateReportSample(model.ReportSample reportSample);
        List<model.ReportSample> GetReportSampleList(int reportId);

        bool AddSampleCondition(model.SampleCondition condition);
        bool DeleteSampleCondition(int sampleId);
        bool UpdateSampleCondition(model.SampleCondition condition);
        List<model.SampleCondition> GetSampleConditionList(int sampleId);

        bool AddSampleRope(model.SampleRope rope);
        bool DeleteSampleRope(int sampleId);
        bool UpdateSampleRope(model.SampleRope rope);
        List<model.SampleRope> GetSampleRopeList(int sampleId);

        bool AddSampleTempDatail(model.SampleTempDetail sampleTempDetail);
        bool DeleteSampleTempDetail(int sampleId);
        bool UpdateSampleTempDetail(model.SampleTempDetail sampleTempDetail);
        List<model.SampleTempDetail> GetSampleTempDetailList(int sampleId);


   

        // TRANSACTION CON SQL LITE

        /*
         BEGIN TRANSACTION;

        UPDATE accounts
           SET balance = balance - 1000
         WHERE account_no = 100;

        UPDATE accounts
           SET balance = balance + 1000
         WHERE account_no = 200;
 
        INSERT INTO account_changes(account_no,flag,amount,changed_at) 
        VALUES(100,'-',1000,datetime('now'));

        INSERT INTO account_changes(account_no,flag,amount,changed_at) 
        VALUES(200,'+',1000,datetime('now'));

        COMMIT;
         
         */




        //List<Person> GetRegisterList();
        //List<Person> GetTodayRegisterList();
        //List<Person> GetRegisterList(DateTime dateFrom, DateTime dateTo);

        //bool AddRegister(Person person);
        //Person GetLastRegister();
        //Person GetLastIncoming(long rfId);
        //RegTotal GetAnyDayStatistic(DateTime findDate);
        //List<RegTotal> GetAnyWeekStatistic(int weekOfYear);

        //List<PersonInfo> GetUserList();
        //bool AddUser(long rfId, string name);
        //bool UpdateUser(long rfId, string name);
        //bool DeleteUser(long rfid);
        //int? GetEmpId(Person person);

        //List<Device> GetDeviceList();
        //bool AddDevice(int serialNum, string desc, Threshold threshold);
        //bool UpdateDevice(int serialNum, string desc);
        //bool UpdateDevice(int serialNum, string tempMin, string tempMax);
        //bool DeleteDevice(int serialNum);
    }
}
