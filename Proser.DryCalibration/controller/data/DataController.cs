using Proser.DryCalibration.controller.data.interfaces;
using Proser.DryCalibration.controller.data.model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Proser.DryCalibration.controller.data
{
    public class DataController : DataBaseLite, IDataSql
    {
        public bool AddCalculation(Calculation calculation)
        {
            throw new NotImplementedException();
        }

        public bool AddCustomer(Customer customer)
        {
            throw new NotImplementedException();
        }

        public bool AddEnvCondition(EnvironmentCondition condition)
        {
            throw new NotImplementedException();
        }

        public bool AddPlace(Place place)
        {
            throw new NotImplementedException();
        }

        public bool AddReport(model.Report report)
        {
            throw new NotImplementedException();
        }

        public bool AddReportEquipment(ReportEquipment equipment)
        {
            throw new NotImplementedException();
        }

        public bool AddReportSample(ReportSample reportSample)
        {
            throw new NotImplementedException();
        }

        public bool AddResponsible(Responsible responsible)
        {
            throw new NotImplementedException();
        }

        public bool AddRopeError(RopeError ropeError)
        {
            throw new NotImplementedException();
        }

        public bool AddSampleCondition(SampleCondition condition)
        {
            throw new NotImplementedException();
        }

        public bool AddSampleRope(SampleRope rope)
        {
            throw new NotImplementedException();
        }

        public bool AddSampleTempDatail(SampleTempDetail sampleTempDetail)
        {
            throw new NotImplementedException();
        }

        public bool AddUltrasonic(Ultrasonic ultrasonic)
        {
            throw new NotImplementedException();
        }

        public bool AddUncertainty(Uncertainty uncertainty)
        {
            throw new NotImplementedException();
        }

        public bool DeleteCalculation(int reportId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteCustomer(int customerId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteEnvCondition(int reportId)
        {
            throw new NotImplementedException();
        }

        public bool DeletePlace(int placeId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteReport(int ReportId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteReportEquipment(int reportId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteReportSample(int reportId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteResponsible(int reportId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteRopeError(int calculationId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSampleCondition(int sampleId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSampleRope(int sampleId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSampleTempDetail(int sampleId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteUltrasonic(int ultrasId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteUncertainty(int calculationId)
        {
            throw new NotImplementedException();
        }

        public Calculation GetCalcultaion(int reportId)
        {
            throw new NotImplementedException();
        }

        public Customer GetCustomer(int customerId)
        {
            throw new NotImplementedException();
        }

        public EnvironmentCondition GetEnvCondition(int reportId)
        {
            throw new NotImplementedException();
        }

        public Place GetPlace(int placeId)
        {
            throw new NotImplementedException();
        }

        public model.Report GetReport(int reportId)
        {
            throw new NotImplementedException();
        }

        public List<ReportEquipment> GetReportEquipmentList(int reportId)
        {
            throw new NotImplementedException();
        }

        public List<model.Report> GetReportList()
        {
            throw new NotImplementedException();
        }

        public List<ReportSample> GetReportSampleList(int reportId)
        {
            throw new NotImplementedException();
        }

        public List<Responsible> GetResponsibleList(int reportId)
        {
            throw new NotImplementedException();
        }

        public List<RopeError> GetRopeErrorList(int calculationId)
        {
            throw new NotImplementedException();
        }

        public List<SampleCondition> GetSampleConditionList(int sampleId)
        {
            throw new NotImplementedException();
        }

        public List<SampleRope> GetSampleRopeList(int sampleId)
        {
            throw new NotImplementedException();
        }

        public List<SampleTempDetail> GetSampleTempDetailList(int sampleId)
        {
            throw new NotImplementedException();
        }

        public Ultrasonic GetUltrasonic(int ultrasonicId)
        {
            throw new NotImplementedException();
        }

        public List<Uncertainty> GetUncertaintyList(int calculationId)
        {
            throw new NotImplementedException();
        }

        public bool UpdateCalculation(Calculation calculation)
        {
            throw new NotImplementedException();
        }

        public bool UpdateCustomer(Customer customer)
        {
            throw new NotImplementedException();
        }

        public bool UpdatePlace(Place place)
        {
            throw new NotImplementedException();
        }

        public bool UpdateReport(model.Report report)
        {
            throw new NotImplementedException();
        }

        public bool UpdateReportEquipment(ReportEquipment equipment)
        {
            throw new NotImplementedException();
        }

        public bool UpdateReportSample(ReportSample reportSample)
        {
            throw new NotImplementedException();
        }

        public bool UpdateResponsible(Responsible responsible)
        {
            throw new NotImplementedException();
        }

        public bool UpdateRopeError(RopeError ropeError)
        {
            throw new NotImplementedException();
        }

        public bool UpdateSampleCondition(SampleCondition condition)
        {
            throw new NotImplementedException();
        }

        public bool UpdateSampleRope(SampleRope rope)
        {
            throw new NotImplementedException();
        }

        public bool UpdateSampleTempDetail(SampleTempDetail sampleTempDetail)
        {
            throw new NotImplementedException();
        }

        public bool UpdateUltrasonic(Ultrasonic ultrasonic)
        {
            throw new NotImplementedException();
        }

        public bool UpdateUncertainty(Uncertainty uncertainty)
        {
            throw new NotImplementedException();
        }

        public bool UpddateEnvCondition(EnvironmentCondition condition)
        {
            throw new NotImplementedException();
        }

    }
}




        //public bool AddRegister(Person person)
        //{
        //    try
        //    {
        //        string query = string.Format("insert into Register (PersonId, PersonName, RegDate, RegType, Temperature, EnvTemperature, CondType, Document, VisitReason)" +
        //         " values ({0}, '{1}', '{2}', {3}, '{4}', '{5}', {6}, '{7}', '{8}')",
        //           person.Id.ToString(),
        //           person.Name,
        //           person.Date.ToString("dd-MM-yyyy HH:mm:ss"),
        //           ((int)person.RegType).ToString(),
        //           person.Temperature,
        //           person.EnvTemperature,
        //           ((int)person.CondType).ToString(),
        //           person.Document,
        //           person.VisitReason

        //           );

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: AddRegister", e);
        //        Console.WriteLine(e.Message);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: AddRegister", e);
        //        Console.WriteLine(e.Message);
        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public List<Person> GetRegisterList()
        //{
        //    List<Person> list = new List<Person>();

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {
        //            string query = "select PersonId, PersonName, RegDate, RegType, Temperature, EnvTemperature, CondType, Document, VisitReason from Register";

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        Thread.CurrentThread.CurrentCulture = new CultureInfo("es-AR");

        //                        int regType = dr.GetInt32(3);
        //                        RegisterType regTypeEnum = (RegisterType)regType;
                                
        //                        int tempCond = dr.GetInt32(6);
        //                        TempCondition tempCondEnum = (TempCondition)tempCond;

        //                        string document = "";

        //                        try
        //                        {
        //                            document = dr.GetString(7);
        //                        }
        //                        catch { }
                             
        //                        string visitReason = "";

        //                        try
        //                        {
        //                            visitReason = dr.GetString(8);
        //                        }
        //                        catch { }
                            
        //                        string strFecha = Convert.ToDateTime(dr.GetString(2)).ToString("dd/MM/yyyy HH:mm:ss");

        //                        string name = "-";

        //                        try
        //                        {
        //                          name =  dr.GetString(1);
        //                        }
        //                        catch { }
                                

        //                        Person person = new Person()
        //                        {
        //                            Id = Convert.ToString(dr.GetInt64(0)),
        //                            Name = name,
        //                            Date = Convert.ToDateTime(dr.GetString(2)),
        //                            Register = Utils.GetEnumDescription(regTypeEnum),
        //                            RegType = regType,
        //                            Temperature = dr.GetString(4),
        //                            EnvTemperature = dr.GetString(5),
        //                            Condition = Utils.GetEnumDescription(tempCondEnum),
        //                            CondType = tempCond,
        //                            Document = document,
        //                            VisitReason = visitReason,
        //                            StrDate = strFecha
        //                        };

        //                        list.Add(person);
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetRegisterList", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetRegisterList", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetRegisterList", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            connection.Close();
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return list;
        //}

        //public List<ConditionInfo> GetTempConditionList() 
        //{
        //    List<ConditionInfo> list = new List<ConditionInfo>(); 

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {
        //            string query = "select PersonId, RegDate, CondType from Register";

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        Thread.CurrentThread.CurrentCulture = new CultureInfo("es-AR");

        //                        int tempCond = dr.GetInt32(2);
        //                        TempCondition tempCondEnum = (TempCondition)tempCond;

        //                        ConditionInfo info = new ConditionInfo()
        //                        {
        //                            PersonId = dr.GetInt64(0),
        //                            RegDate = Convert.ToDateTime(dr.GetString(1)),
        //                            Condition = tempCondEnum
        //                        };
                 
        //                        list.Add(info);
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetTempConditionList", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetTempConditionList", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetTempConditionList", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            connection.Close();
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return list;

        //}

        //public List<ConditionInfo> GetTempConditionList(DateTime dateFrom, DateTime dateTo) 
        //{
        //    List<ConditionInfo> list = GetTempConditionList();

        //    if (list.Count > 0)
        //    {
        //        list = list.Where(p => (p.RegDate >= dateFrom && p.RegDate <= dateTo)).ToList();

        //        if (list.Count > 0) 
        //        {
        //            list = list.GroupBy(p => new { p.PersonId, p.Condition })
        //           .Select(g => g.First())
        //           .ToList();
        //        }           
        //    }

        //    return list;
        //}

        //public List<Person> GetTodayRegisterList()
        //{
        //    List<Person> list = new List<Person>();

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {
        //            string today = DateTime.Today.ToString("dd-MM-yyyy");

        //            string query = string.Format("select PersonId, PersonName, RegDate, RegType, Temperature, EnvTemperature, CondType, Document, VisitReason from Register where RegDate LIKE '%{0}%'", today);

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        Thread.CurrentThread.CurrentCulture = new CultureInfo("es-AR");

        //                        int regType = dr.GetInt32(3);
        //                        RegisterType regTypeEnum = (RegisterType)regType;

        //                        int tempCond = dr.GetInt32(6);
        //                        TempCondition tempCondEnum = (TempCondition)tempCond;

        //                        string document = "";

        //                        try
        //                        {
        //                            document = dr.GetString(7);
        //                        }
        //                        catch { }

        //                        string visitReason = "";

        //                        try
        //                        {
        //                            visitReason = dr.GetString(8);
        //                        }
        //                        catch { }

        //                        string strFecha = Convert.ToDateTime(dr.GetString(2)).ToString("dd/MM/yyyy HH:mm:ss");

        //                        string name = "-";

        //                        try
        //                        {
        //                            name = dr.GetString(1);
        //                        }
        //                        catch { }


        //                        Person person = new Person()
        //                        {
        //                            Id = Convert.ToString(dr.GetInt64(0)),
        //                            Name = name,
        //                            Date = Convert.ToDateTime(dr.GetString(2)),
        //                            Register = Utils.GetEnumDescription(regTypeEnum),
        //                            RegType = regType,
        //                            Temperature = dr.GetString(4),
        //                            EnvTemperature = dr.GetString(5),
        //                            Condition = Utils.GetEnumDescription(tempCondEnum),
        //                            CondType = tempCond,
        //                            Document = document,
        //                            VisitReason = visitReason,
        //                            StrDate = strFecha
        //                        };

        //                        list.Add(person);
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetRegisterList", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetRegisterList", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetRegisterList", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            connection.Close();
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return list;
        //}

        //public List<Person> GetRegisterList(DateTime dateFrom, DateTime dateTo)
        //{
        //    List<Person> list = GetRegisterList();

        //    if (list.Count > 0) 
        //    {
        //        list = list.Where(p => (p.Date >= dateFrom && p.Date <= dateTo)).ToList();
        //    }

        //    return list;
        //}

        //public Person GetLastRegister()
        //{
        //    Person person = null;

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {
        //            string today = DateTime.Today.ToString("dd-MM-yyyy");

        //            string query = string.Format("select PersonId, PersonName, RegDate, RegType, Temperature, EnvTemperature, CondType, RegId from Register where RegDate LIKE '%{0}%' order by RegId desc LIMIT 1", today);

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        Thread.CurrentThread.CurrentCulture = new CultureInfo("es-AR");

        //                        int regId = dr.GetInt32(7);
        //                        RegisterType regTypeEnum = (RegisterType)dr.GetInt32(3);
        //                        TempCondition tempCondEnum = (TempCondition)dr.GetInt32(6);

        //                        string strFecha = Convert.ToDateTime(dr.GetString(2)).ToString("dd/MM/yyyy HH:mm:ss");

        //                        string name = "-";

        //                        try
        //                        {
        //                            name = dr.GetString(1);
        //                        }
        //                        catch { }

        //                        person = new Person()
        //                        {
        //                            Id = Convert.ToString(dr.GetInt64(0)),
        //                            TagId = dr.GetInt64(0),
        //                            RegId = regId,
        //                            Name = name,
        //                            Date = Convert.ToDateTime(dr.GetString(2)),
        //                            Register = Utils.GetEnumDescription(regTypeEnum),
        //                            RegType = (int)regTypeEnum,
        //                            Temperature = dr.GetString(4),
        //                            EnvTemperature = dr.GetString(5),
        //                            Condition = Utils.GetEnumDescription(tempCondEnum),
        //                            CondType = (int)tempCondEnum,
        //                            StrDate = strFecha
        //                        };
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetLastRegister", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetLastRegister", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetLastRegister", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            connection.Close();
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return person;
        //}

        //public RegTotal GetAnyDayStatistic(DateTime anyDay)
        //{     
        //    RegTotal result = new RegTotal();

        //    try
        //    {
        //        DateTime dateFrom = new DateTime(anyDay.Year, anyDay.Month, anyDay.Day);
        //        DateTime dateTo = new DateTime(anyDay.Year, anyDay.Month, anyDay.Day, 23, 59, 59);

        //        List<ConditionInfo> list = GetTempConditionList(dateFrom, dateTo);

        //        if (list.Count > 0)
        //        {
        //            result.Normal = list.Count(c => c.Condition == TempCondition.normal);
        //            result.Elevated = list.Count(c => c.Condition == TempCondition.elevated);
        //            result.Critical = list.Count(c => c.Condition == TempCondition.critical);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: GetAnyDayStatistic", e);
        //    }

        //    return result;
        //}

        //public List<RegTotal> GetAnyWeekStatistic(int weekOfyear)
        //{
        //    List<RegTotal> result = new List<RegTotal>();

        //    DateTime firstDayOfWeek = Utils.FirstDateOfWeek(DateTime.Today.Year, weekOfyear, CultureInfo.CurrentCulture); // Sunday

        //    for (int i = 0; i < 7; i++)
        //    {
        //        if (firstDayOfWeek.AddDays(i) > DateTime.Today)
        //        {
        //            result.Add(new RegTotal());
        //            continue;
        //        }

        //        RegTotal dateTot = GetAnyDayStatistic(firstDayOfWeek.AddDays(i));
        //        result.Add(dateTot);
        //    }

        //    return result;
        //}


        //public List<PersonInfo> GetUserList()
        //{
        //    List<PersonInfo> list = new List<PersonInfo>();

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {
        //            string query = "select Rfid, Name from Person";

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        PersonInfo person = new PersonInfo()
        //                        {
        //                            Id = dr.GetInt64(0),
        //                            Name = dr.GetString(1),
        //                        };

        //                        list.Add(person);
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetUserList", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetUserList", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetUserList", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            connection.Close();
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return list;
        //}

        //public bool AddUser(long rfId, string name)
        //{
        //    try
        //    {
        //        string query = string.Format("insert into Person (RfId, Name)" +
        //         " values ({0}, '{1}')", rfId, name);

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: AddUser", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: AddUser", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public bool UpdateUser(long rfId, string name)
        //{
        //    try
        //    {
        //        string query = string.Format("update Person set Name='{0}' where Rfid = {1}", name, rfId);

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: UpdateUser", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: UpdateUser", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public bool DeleteUser(long rfid)
        //{
        //    try
        //    {
        //        string query = string.Format("delete from Person where Rfid = {0}", rfid);

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: DeleteUser", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: DeleteUser", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public int? GetEmpId(Person person)
        //{
        //    int? EmpId = null;

        //    try
        //    {

        //        GetConnection();

        //        if (connection != null)
        //        {
        //            string query = string.Format("select EmpId from Person where Rfid={0}", person.TagId);

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        EmpId = dr.GetInt32(0);
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }

        //        return EmpId;

        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetEmpId", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return EmpId;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: GetEmpId", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return EmpId;
        //    }
        //}


        //public List<Device> GetDeviceList()
        //{
        //    List<Device> list = new List<Device>();

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {
        //            string query = "select SerialNum, Description, TempMin, TempMax from Device";

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        Device device = new Device()
        //                        {
        //                            SerialNumber = dr.GetInt32(0),
        //                            Description = dr.GetString(1),
        //                            TempMin = dr.GetString(2),
        //                            TempMax = dr.GetString(3)
        //                        };

        //                        list.Add(device);
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetDeviceList", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetDeviceList", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetDeviceList", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            connection.Close();
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return list;
        //}

        //public bool AddDevice(int serialNum, string desc, Threshold threshold)
        //{
        //    try
        //    {
        //        string query = string.Format("insert into Device (SerialNum, Description, TempMin, TempMax)" +
        //         " values ({0}, '{1}', '{2}', '{3}')",
        //         serialNum,
        //         desc,
        //         threshold.TempMin,
        //         threshold.TempMax);

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: AddDevice", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: AddDevice", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public bool UpdateDevice(int serialNum, string desc)
        //{
        //    try
        //    {
        //        string query = string.Format("update Device set Description='{0}' where SerialNum = {1}", desc, serialNum);

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: UpdateDevice", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: UpdateDevice", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public bool UpdateDevice(int serialNum, string tempMin, string tempMax) 
        //{
        //    try
        //    {
        //        string query = string.Format("update Device set TempMin='{0}', TempMax = '{1}' where SerialNum = {2}", tempMin, tempMax, serialNum);

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: UpdateDevice", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: UpdateDevice", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public bool DeleteDevice(int serialNum)
        //{
        //    try
        //    {
        //        string query = string.Format("delete from Device where SerialNum = {0}", serialNum);

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: DeleteDevice", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: DeleteDevice", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}


        //public int Count()
        //{
        //    int count = -1;

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {
        //            string query = "select count(*) from Buffer";

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        count = dr.GetInt32(0);
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: Buffer Count", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: Buffer Count", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: Buffer Count", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            connection.Close();
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return count;
        //}

        //public bool Clear() 
        //{
        //    try
        //    {
        //        string query = string.Format("delete from Buffer");

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: Clear buffer", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: Clear buffer", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }

        //}

        //public bool Reset()
        //{
        //    try
        //    {
        //        string query = string.Format("delete from sqlite_sequence where name='Buffer'");

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: Reset buffer", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: Reset buffer", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }

        //}

        //public bool Enqueue(BufferItem buffer)
        //{
        //    try
        //    {
        //        string query = string.Format("insert into Buffer (RequestType, Name, Address, State, Body, CRC, StateCrc, Frame )" +
        //         " values ({0},'{1}',{2},{3},'{4}','{5}',{6},'{7}')",
        //         (int)buffer.RequestType,
        //         buffer.Name,
        //         buffer.Address,
        //         (int)buffer.State,
        //         buffer.Body,
        //         buffer.CRC,
        //         (int)buffer.StateCrc,
        //         buffer.Frame
        //         );

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: AddUser", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: AddUser", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public BufferItem Dequeue()
        //{
        //    BufferItem buffer = null;

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {
                  
        //            string query = "select BufferId, RequestType, Name, Address, State, Body, CRC, StateCrc, Frame from Buffer order by BufferId asc LIMIT 1";

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {           
        //                        RequestType reqType = (RequestType)dr.GetInt32(1);
        //                        RespState respState = (RespState)dr.GetInt32(4);
        //                        CrcState crcState = (CrcState)dr.GetInt32(7);

        //                        buffer = new BufferItem()
        //                        {
        //                            BufferId = dr.GetInt32(0),
        //                            RequestType = reqType,
        //                            Name = dr.GetString(2),
        //                            Address = dr.GetInt32(3),
        //                            State = respState,
        //                            Body = dr.GetString(5),
        //                            CRC = dr.GetString(6),
        //                            StateCrc = crcState,
        //                            Frame = dr.GetString(8)
        //                        };
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: Buffer Dequeue", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: Buffer Dequeue", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: Buffer Dequeue", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            connection.Close();
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return buffer;
        //}

        //public bool Delete(int bufferId)
        //{
        //    try
        //    {
        //        string query = string.Format("delete from Buffer where BufferId = {0}", bufferId);

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: Buffer Delete", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error:  Buffer Delete", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public LocalStatus GetLocalStatus()
        //{
        //    LocalStatus status = null;

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {

        //            string query = "select LocServSt, LocDevPortSt from LocalStatus order by LocStId desc LIMIT 1";

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        LocServSt locServSt = (LocServSt)dr.GetInt32(0);
        //                        LocDevPortSt locDevPortSt = (LocDevPortSt)dr.GetInt32(1);
                        
        //                        status = new LocalStatus()
        //                        {
        //                             LocServStatus = locServSt,
        //                             LocDevPortStatus = locDevPortSt
        //                        };
        //                    }

        //                    dr.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetLocalStatus", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetLocalStatus", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetLocalStatus", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            try
        //            {
        //                connection.Close();
        //            }
        //            catch (Exception e)
        //            {
        //                log.Log.WriteIfExists("Error: GetLocalStatus Finally.", e);
                        
        //            }

                   
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return status;
        //}

        //public bool SetLocalStatus(LocalStatus status)
        //{
        //    try
        //    {
        //        int locSerSt = (int)status.LocServStatus;
        //        int locDevPortSt = (int)status.LocDevPortStatus;
                
        //        string query = string.Format("update LocalStatus set LocServSt={0}, LocDevPortSt={1}", locSerSt, locDevPortSt);

        //        return ExecuteQuery(query);
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: SetLocalStatus", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: SetLocalStatus", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //}

        //public bool ResetLocalStatus()
        //{
        //    try
        //    {

        //        string query = "delete from LocalStatus";

        //        bool deleted =  ExecuteQuery(query);

        //        if (deleted) 
        //        {
        //            query = string.Format("delete from sqlite_sequence where name='LocalStatus'");

        //            bool reseted = ExecuteQuery(query);

        //            if (reseted) 
        //            {
        //                query = "insert into LocalStatus (LocServSt, LocDevPortSt) values (0, 0)";

        //                return ExecuteQuery(query);
        //            }
        //        }

        //        return false;
        //    }
        //    catch (System.AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: ResetLocalStatus", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        // log
        //        log.Log.WriteIfExists("Error: ResetLocalStatus", e);

        //        connection.Close();
        //        //log.Log.WriteIfExists("Connection Close.");
        //        return false;
        //    }

        //}

        //public Person GetLastIncoming(long rfId)
        //{
        //    Person person = null;

        //    try
        //    {
        //        GetConnection();

        //        if (connection != null)
        //        {
        //            string query = string.Format("select PersonId, PersonName, RegDate, RegType, Temperature, EnvTemperature, CondType, RegId from Register where regType = {0} and PersonId = {1} order by RegId desc",
        //                (int) RegisterType.Incoming, rfId );

        //            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
        //            {
        //                List<Person> list = new List<Person>();

        //                using (SQLiteDataReader dr = cmd.ExecuteReader())
        //                {
        //                    while (dr.Read())
        //                    {
        //                        DateTime date = Convert.ToDateTime(dr.GetString(2));

        //                        bool todayIncoming = (DateTime.Today.ToString("ddMMyyyy")).Equals(date.ToString("ddMMyyyy"));

        //                        if (todayIncoming) 
        //                        {
        //                            Thread.CurrentThread.CurrentCulture = new CultureInfo("es-AR");

        //                            int regId = dr.GetInt32(7);
        //                            RegisterType regTypeEnum = (RegisterType)dr.GetInt32(3);
        //                            TempCondition tempCondEnum = (TempCondition)dr.GetInt32(6);

        //                            string strFecha = Convert.ToDateTime(dr.GetString(2)).ToString("dd/MM/yyyy HH:mm:ss");

        //                            string name = "-";

        //                            try
        //                            {
        //                                name = dr.GetString(1);
        //                            }
        //                            catch { }

        //                            Person p = new Person()
        //                            {
        //                                Id = Convert.ToString(dr.GetInt64(0)),
        //                                TagId = dr.GetInt64(0),
        //                                RegId = regId,
        //                                Name = name,
        //                                Date = date,
        //                                Register = Utils.GetEnumDescription(regTypeEnum),
        //                                RegType = (int)regTypeEnum,
        //                                Temperature = dr.GetString(4),
        //                                EnvTemperature = dr.GetString(5),
        //                                Condition = Utils.GetEnumDescription(tempCondEnum),
        //                                CondType = (int)tempCondEnum,
        //                                StrDate = strFecha
        //                            };

        //                            list.Add(p);
        //                        }     
        //                    }

        //                    dr.Close();
        //                }

        //                if (list.Count > 0) 
        //                {
        //                    person = list.FirstOrDefault(p => (p.Date == list.Min(m => m.Date)));
        //                }
                        
        //            }
        //        }
        //    }
        //    catch (AccessViolationException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetLastIncoming", e);
        //    }
        //    catch (SystemException e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetLastIncoming", e);
        //    }
        //    catch (Exception e)
        //    {
        //        //log
        //        log.Log.WriteIfExists("Error: GetLastIncoming", e);
        //    }
        //    finally
        //    {
        //        if (connection != null)
        //        {
        //            connection.Close();
        //            //log.Log.WriteIfExists("Connection Close.");
        //        }
        //    }

        //    return person;         
        //}

    //}
//}
