using MMDAL;
using MMLib.Models.MYOB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Helpers
{
    public static class JobHelper
    {
        public static IEnumerable<MyobJob> ConvertModel(List<MyobJobModel> selectedJobs, int apId)
        {
            List<MyobJob> jobs = new List<MyobJob>();
            foreach (var job in selectedJobs)
            {
                jobs.Add(new MyobJob
                {
                    JobID = job.JobID,
                    ParentJobID = job.ParentJobID,
                    IsInactive = job.IsInactive,
                    JobName = job.JobName,
                    JobNumber = job.JobNumber,
                    IsHeader = job.IsHeader,
                    JobLevel = job.JobLevel,
                    IsTrackingReimburseable = job.IsTrackingReimburseable,
                    JobDescription = job.JobDescription,
                    ContactName = job.ContactName,
                    Manager = job.Manager,
                    PercentCompleted = job.PercentCompleted,
                    StartDate = job.StartDate,
                    FinishDate = job.FinishDate,
                    CustomerID = job.CustomerID,
                    AccountProfileId = apId,
                    CreateTime = DateTime.Now,
                    ModifyTime = DateTime.Now
                });
            }
            return jobs;
        }
    }

}
