using MMDAL;
using MMLib.Models.MYOB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Helpers
{
    public static class JobHelper
    {
        public static List<MyobJob> ConvertModel(List<MyobJobModel> selectedJobs, int apId)
        {
            List<MyobJob> jobs = new List<MyobJob>();
            try
            {                
                DateTime dateTime = DateTime.Now;
                foreach (var job in selectedJobs)
                {
                    jobs.Add(new MyobJob
                    {
                        JobID = job.JobID,
                        ParentJobID = job.ParentJobID ?? 0,
                        IsInactive = job.IsInactive ?? false,
                        JobName = job.JobName ?? "",
                        JobNumber = job.JobNumber ?? "",
                        IsHeader = job.IsHeader ?? false,
                        JobLevel = job.JobLevel ?? 0,
                        IsTrackingReimburseable = job.IsTrackingReimburseable ?? false,
                        JobDescription = job.JobDescription ?? "",
                        ContactName = job.ContactName ?? "",
                        Manager = job.Manager ?? "",
                        PercentCompleted = job.PercentCompleted ?? 0,
                        StartDate = job.StartDate ?? null,
                        FinishDate = job.FinishDate ?? null,
                        CustomerID = job.CustomerID ?? 0,
                        AccountProfileId = apId,
                        CreateTime = dateTime,
                    });
                }
            }
            catch(Exception)
            {
                
            }
            
            return jobs;
        }
    }

}
