using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Scheduling;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using Vanjaro.Common.Data.Entities;
using Vanjaro.Common.Factories;

namespace Vanjaro.Common.Components
{
    public class Scheduler : SchedulerClient
    {
        private static readonly object VanjaroCommonLock = new object();
        private List<string> SchedulerLog = new List<string>();

        public Scheduler(ScheduleHistoryItem HistoryItem)
        : base()
        {
            ScheduleHistoryItem = HistoryItem;
        }

        public override void DoWork()
        {
            try
            {
                lock (VanjaroCommonLock)
                {
                    ProcessMailQueue();//Run Every 2 Minute            

                    if (SchedulerLog.Count > 0)
                    {
                        StringBuilder LogNote = new StringBuilder();
                        foreach (string str in SchedulerLog)
                        {
                            LogNote.Append(str + " | ");
                        }
                        ScheduleHistoryItem.AddLogNote("<br />&nbsp; - " + LogNote.ToString().TrimEnd(' ', '|'));
                    }
                    else
                    {
                        ScheduleHistoryItem.AddLogNote("<br />&nbsp; - " + "Idle - No Work Performed.");
                    }

                    ScheduleHistoryItem.Succeeded = true;
                }
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
                Errored(ref ex);
                ScheduleHistoryItem.AddLogNote("Not able to run successfully. Check your event log.");
                ScheduleHistoryItem.Succeeded = false;
            }
            finally
            {
                SchedulerLog = null;
            }
        }

        private void ProcessMailQueue()
        {
            foreach (int ModuleId in SettingFactory.GetDistinctModuleIds(AppFactory.Identifiers.admin_notifications_email.ToString()))
            {
                SmtpClient client = null;
                string NotificationEnabled = SettingFactory.GetValue(ModuleId, AppFactory.Identifiers.admin_notifications_email.ToString(), "Notification_Email");
                if (!string.IsNullOrEmpty(NotificationEnabled) && Convert.ToBoolean(NotificationEnabled))
                {

                    SmtpServer SmtpServer = NotificationFactory.GetSMTP(ModuleId, AppFactory.Identifiers.admin_notifications_email.ToString());
                    try
                    {
                        if (SmtpServer != null && !string.IsNullOrEmpty(SmtpServer.Server) && SmtpServer.Port > 0)
                        {
                            List<MailQueue> MailQueue = NotificationFactory.GetMailQueue(ModuleId);
                            if (MailQueue.Count > 0)
                            {
                                client = NotificationFactory.Connect(SmtpServer.Server, SmtpServer.Port, SmtpServer.Authentication, SmtpServer.Username, SmtpServer.Password, SmtpServer.SSL);

                                foreach (MailQueue mail in MailQueue)
                                {
                                    try
                                    {
                                        NotificationFactory.SendMail(client, mail);
                                        mail.Delete();
                                    }
                                    catch (Exception ex)
                                    {
                                        if (mail.RetryAttempt < 3)
                                        {
                                            mail.Status = "Retry";
                                            mail.RetryDateTime = DateTime.Now.AddMinutes(2);
                                            mail.RetryAttempt = mail.RetryAttempt + 1;
                                            mail.Update();
                                        }
                                        else
                                        {
                                            mail.Status = "Error";
                                            mail.Update();
                                            mail.Delete();
                                        }
                                        Exceptions.LogException(ex);
                                    }
                                }
                                SchedulerLog.Add("Mail queue operation run successfully");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Exceptions.LogException(ex);
                        SchedulerLog.Add("Mail queue operation Failed. !See event log for details");
                    }
                    finally
                    {
                        if (client != null)
                        {
                            client.Dispose();
                        }
                    }
                }
            }

        }



    }
}